﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Languages.Editor.Tasks;
using Microsoft.R.Debugger;
using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.R.Package.Shell;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.R.Package.DataInspect {
    internal class VariableChangedArgs : EventArgs {
        public EvaluationWrapper NewVariable { get; set; }
    }

    internal class VariableProvider: IDisposable {
        #region members and ctor

        private IRSession _rSession;
        private DebugSession _debugSession;

        public VariableProvider() {
            var sessionProvider = VsAppShell.Current.ExportProvider.GetExport<IRSessionProvider>().Value;
            sessionProvider.CurrentChanged += RSessionProvider_CurrentChanged;

            IdleTimeAction.Create(() => {
                SetRSession(sessionProvider.Current).SilenceException<Exception>().DoNotWait();
            }, 10, typeof(VariableProvider));
        }

        #endregion

        #region Public

        private static VariableProvider _instance;
        /// <summary>
        /// Singleton
        /// </summary>
        public static VariableProvider Current {
            get {
                if(_instance == null) {
                    _instance = new VariableProvider();
                }
                return _instance;
            }
        }

        public event EventHandler<VariableChangedArgs> VariableChanged;

        public EvaluationWrapper LastEvaluation { get; private set; }

        public async Task<IGridData<string>> GetGridDataAsync(string expression, GridRange gridRange) {
            await TaskUtilities.SwitchToBackgroundThread();

            var rSession = _rSession;

            string rows = RangeToRString(gridRange.Rows);
            string columns = RangeToRString(gridRange.Columns);

            using (var elapsed = new Elapsed("Data:Evaluate:")) {
                using (var evaluator = await rSession.BeginEvaluationAsync(false)) {
                    var result = await evaluator.EvaluateAsync($"rtvs:::grid.dput(rtvs:::grid.data({expression}, {rows}, {columns}))", REvaluationKind.Normal);

                    if (result.ParseStatus != RParseStatus.OK || result.Error != null) {
                        throw new InvalidOperationException($"Grid data evaluation failed:{result}");
                    }

                    var data = GridParser.Parse(result.StringResult);
                    data.Range = gridRange;

                    if (data.ValidHeaderNames
                        && (data.ColumnNames.Count != gridRange.Columns.Count
                            || data.RowNames.Count != gridRange.Rows.Count)) {
                        throw new InvalidOperationException("Header names lengths are different from data's length");
                    }

                    return data;
                }
            }
        }

        public void Dispose() {
            var sessionProvider = VsAppShell.Current.ExportProvider.GetExport<IRSessionProvider>().Value;
            if (sessionProvider != null) {
                sessionProvider.CurrentChanged -= RSessionProvider_CurrentChanged;
            }

            // Only used in tests to make sure each instance 
            // of the variable explorer uses fresh variable provider
            _instance = null;
        }
        #endregion

        #region RSession related event handler

        private void RSession_Mutated(object sender, EventArgs e) {
            RefreshVariableCollection().SilenceException<Exception>().DoNotWait();
        }

        /// <summary>
        /// IRSessionProvider.CurrentSessionChanged handler. When current session changes, this is called
        /// </summary>
        private void RSessionProvider_CurrentChanged(object sender, EventArgs e) {
            var sessionProvider = sender as IRSessionProvider;
            Debug.Assert(sessionProvider != null);

            if (sessionProvider != null) {
                SetRSession(sessionProvider.Current).SilenceException<Exception>().DoNotWait();
            }
        }

        #endregion

        private async Task InitializeData() {
            var debugSessionProvider = VsAppShell.Current.ExportProvider.GetExport<IDebugSessionProvider>().Value;

            _debugSession = await debugSessionProvider.GetDebugSessionAsync(_rSession);

            await RefreshVariableCollection();
        }

        private async Task SetRSession(IRSession session) {
            // cleans up old RSession
            if (_rSession != null) {
                _rSession.Mutated -= RSession_Mutated;
            }

            // set new RSession
            _rSession = session;
            if (_rSession != null) {
                _rSession.Mutated += RSession_Mutated;
                await InitializeData();
            }
        }

        private async Task RefreshVariableCollection() {
            if (_debugSession == null) {
                return;
            }

            var stackFrames = await _debugSession.GetStackFramesAsync();

            var globalStackFrame = stackFrames.FirstOrDefault(s => s.IsGlobal);
            if (globalStackFrame != null) {
                DebugEvaluationResult evaluation = await globalStackFrame.EvaluateAsync("environment()", "Global Environment");

                LastEvaluation = new EvaluationWrapper(-1, evaluation, false);  // root level doesn't truncate children and return every variables

                if (VariableChanged != null) {
                    VariableChanged(
                        this,
                    new VariableChangedArgs() { NewVariable = LastEvaluation });
                }
            }
        }

        private static string RangeToRString(Range range) {
            return $"{range.Start + 1}:{range.Start + range.Count}";
        }
    }
}
