﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.VisualStudio.R.Package.Commands;

namespace Microsoft.VisualStudio.R.Package.Repl.Debugger {
    internal sealed class StepIntoCommand : DebuggerWrappedCommand {
        public StepIntoCommand(IRInteractiveWorkflowVisual interactiveWorkflow)
            : base(interactiveWorkflow, RPackageCommandId.icmdStepInto, 
                   VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.StepInto,
                   DebuggerCommandVisibility.Stopped) {
        }
    }
}
