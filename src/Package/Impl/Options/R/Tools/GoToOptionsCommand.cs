﻿using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.R.Package.Options.R.Tools {
    public sealed class GoToOptionsCommand : MenuCommand {
        public GoToOptionsCommand() :
            base(OnCommand, new CommandID(RGuidList.RCmdSetGuid, RPackageCommandId.icmdGoToRToolsOptions)) { }

        public static void OnCommand(object sender, EventArgs args) {
            IVsShell shell = VsAppShell.Current.GetGlobalService<IVsShell>(typeof(SVsShell));
            IVsPackage package;

            if (VSConstants.S_OK == shell.LoadPackage(RGuidList.RPackageGuid, out package)) {
                ((Microsoft.VisualStudio.Shell.Package)package).ShowOptionPage(typeof(RToolsOptionsPage));
            }
        }
    }
}
