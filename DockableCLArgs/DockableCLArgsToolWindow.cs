using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace ImaginationTechnologies.DockableCLArgs
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("1fabd1b6-9281-49cd-82c2-fa8a46ce7f06")]
    public sealed class DockableCLArgsToolWindow : ToolWindowPane//, IVsPersistSolutionOpts
    {
        public DockableCLArgsControl MyControl { get; private set; }

        private const string STR_DockableCLArgs_History = "DockableCLArgs_History";

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public DockableCLArgsToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            base.Content = new DockableCLArgsControl();
        }

        /*
        public int LoadUserOptions(IVsSolutionPersistence persistence, uint grfLoadOpts)
        {
            this.MyControl.History.Clear();

            try
            {
                persistence.LoadPackageUserOpts(this, STR_DockableCLArgs_History);
            }
            finally
            {
                Marshal.ReleaseComObject(persistence);
            }

            return VSConstants.S_OK;
        }

        public int ReadUserOptions(IStream optionsStream, string pszKey)
        {
            try
            {
                using (StreamEater wrapper = new StreamEater(optionsStream))
                {
                    switch (pszKey)
                    {
                        case STR_DockableCLArgs_History:
                            LoadOptions(wrapper);
                            break;
                        default:
                            break;
                    }
                }
                return VSConstants.S_OK;
            }
            finally
            {
                Marshal.ReleaseComObject(optionsStream);
            }
        }

        public int SaveUserOptions(IVsSolutionPersistence persistence)
        {
            try
            {
                if (this.MyControl.History.Count > 0)
                    persistence.SavePackageUserOpts(this, STR_DockableCLArgs_History);
            }
            finally
            {
                Marshal.ReleaseComObject(persistence);
            }

            return VSConstants.S_OK;
        }

        public int WriteUserOptions(IStream optionsStream, string pszKey)
        {
            try
            {
                using (StreamEater wrapper = new StreamEater(optionsStream))
                {
                    switch (pszKey)
                    {
                        case STR_DockableCLArgs_History:
                            WriteOptions(wrapper);
                            break;
                        default:
                            break;
                    }
                }

                return VSConstants.S_OK;
            }
            finally
            {
                Marshal.ReleaseComObject(optionsStream);
            }
        }

        private void WriteOptions(Stream storageStream)
        {
            if (this.MyControl.History.Count == 0)
                return;

            using (BinaryWriter writer = new BinaryWriter(storageStream))
            {
                writer.Write(this.MyControl.History.Count);
                foreach (string historyItem in this.MyControl.History)
                {
                    writer.Write(historyItem);
                }
            }
        }

        private void LoadOptions(Stream storageStream)
        {
            using (BinaryReader reader = new BinaryReader(storageStream))
            {
                int historyCount = reader.ReadInt32();
                for (int i = 0; i < historyCount; ++i)
                {
                    string historyItem = reader.ReadString();
                    this.MyControl.History.Enqueue(historyItem);
                }
            }
        }
        */
    }
}
