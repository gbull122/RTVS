﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.R.Components.Extensions;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Core.Formatting;
using Microsoft.R.DataInspection;
using Microsoft.R.Editor.Settings;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.VisualStudio.R.Package.Shell;

namespace Microsoft.VisualStudio.R.Package.DataInspect.Viewers {
    [Export(typeof(IObjectDetailsViewer))]
    internal sealed class CodeViewer : ViewerBase, IObjectDetailsViewer {
        private readonly static string[] _types = { "closure", "language" };
        private readonly IRInteractiveWorkflow _workflow;

        [ImportingConstructor]
        public CodeViewer(IRInteractiveWorkflowProvider workflowProvider, IDataObjectEvaluator evaluator) :
            base(evaluator) {
            _workflow = workflowProvider.GetOrCreate();
        }

        #region IObjectDetailsViewer
        public ViewerCapabilities Capabilities => ViewerCapabilities.Function;

        public bool CanView(IRValueInfo evaluation) {
            return _types.Contains(evaluation?.TypeName);
        }

        public async Task ViewAsync(string expression, string title) {
            var evaluation = await EvaluateAsync(expression, REvaluationResultProperties.ExpressionProperty, null);
            if (evaluation == null || string.IsNullOrEmpty(evaluation.Expression)) {
                return;
            }

            var functionName = evaluation.Expression;
            var session = _workflow.RSession;

            string functionCode = await GetFunctionCode(functionName);
            if (!string.IsNullOrEmpty(functionCode)) {

                string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".r");
                try {
                    if (File.Exists(tempFile)) {
                        File.Delete(tempFile);
                    }

                    using (var sw = new StreamWriter(tempFile)) {
                        sw.Write(functionCode);
                    }

                    await VsAppShell.Current.SwitchToMainThreadAsync();

                    FileViewer.ViewFile(tempFile, functionName);
                    try {
                        File.Delete(tempFile);
                    } catch (IOException) { } catch (AccessViolationException) { }

                } catch (IOException) { } catch (AccessViolationException) { }
            }
        }
        #endregion

        internal async Task<string> GetFunctionCode(string functionName) {
            var session = _workflow.RSession;
            string functionCode = null;
            try {
                functionCode = await session.GetFunctionCodeAsync(functionName);
            } catch (RException) { } catch (MessageTransportException) { }

            if (!string.IsNullOrEmpty(functionCode)) {
                var formatter = new RFormatter(REditorSettings.FormatOptions);
                functionCode = formatter.Format(functionCode);
            }
            return functionCode;
        }
    }
}
