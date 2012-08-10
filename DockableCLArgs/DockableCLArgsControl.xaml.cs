using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using EnvDTE;
using EnvDTE80;
using EnvDTE90;
using EnvDTE100;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ImaginationTechnologies.DockableCLArgs
{
    /// <summary>
    /// Interaction logic for DockableCLArgsControl.xaml
    /// </summary>
    public partial class DockableCLArgsControl : UserControl
    {
        private bool runChangedHandler = true;

        private DTE2 dte;
        private DebuggerEvents debugEvents;
        private SolutionEvents solutionEvents;

        public FixedSizeQueue<string> History = new FixedSizeQueue<string>(10);

        public DockableCLArgsControl()
        {
            InitializeComponent();

            dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            debugEvents = dte.Events.DebuggerEvents;
            debugEvents.OnEnterRunMode += DebuggerEvents_OnEnterRunMode;

            solutionEvents = dte.Events.SolutionEvents;
            solutionEvents.Opened += SolutionEvents_OnOpened;
            solutionEvents.AfterClosing += SolutionEvents_OnAfterClosing;

            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = false;
        }

        #region TextBox Events

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (runChangedHandler)
            {
                SetCommandArgs(CmdArgs.Text);
            }
        }

        private void OnFocus(object sender, RoutedEventArgs e)
        {
            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;
        }

        #endregion TextBox Events

        #region IDE Events

        private void SolutionEvents_OnOpened()
        {
            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = true;
        }

        private void SolutionEvents_OnAfterClosing()
        {
            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = false;
        }

        private void DebuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            AddToHistory(CmdArgs.Text);
        }

        #endregion IDE Events

        #region Context Menu

        private void OnCmdArgsCtxMenu_Cut(object sender, RoutedEventArgs e)
        {
            CmdArgs.Cut();
            SetCommandArgs(CmdArgs.Text);
        }

        private void OnCmdArgsCtxMenu_Copy(object sender, RoutedEventArgs e)
        {
            CmdArgs.Copy();
        }

        private void OnCmdArgsCtxMenu_Paste(object sender, RoutedEventArgs e)
        {
            CmdArgs.Paste();
            SetCommandArgs(CmdArgs.Text);
        }

        private void OnCmdArgsCtxMenu_Opened(object sender, RoutedEventArgs e)
        {
            CmdArgsCtxMenu_Cut.IsEnabled = true;
            CmdArgsCtxMenu_Copy.IsEnabled = true;
            CmdArgsCtxMenu_Paste.IsEnabled = true;

            CmdArgsCtxMenu_HistoryMenu.IsEnabled = false;

            if (CmdArgs.SelectedText == "")
            {
                CmdArgsCtxMenu_Cut.IsEnabled = false;
                CmdArgsCtxMenu_Copy.IsEnabled = false;
            }
            if (!Clipboard.ContainsText())
            {
                CmdArgsCtxMenu_Paste.IsEnabled = false;
            }

            if (History.Count > 0)
            {
                CmdArgsCtxMenu_HistoryMenu.IsEnabled = true;

                CmdArgsCtxMenu_HistoryMenu.Items.Clear();
                foreach (string historyEntry in History)
                {
                    MenuItem historyMI = new MenuItem();
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = historyEntry;
                    historyMI.Header = textBlock;
                    historyMI.Name = "HistoryEntry";
                    historyMI.IsCheckable = true;
                    if (historyEntry == GetCommandArgs())
                        historyMI.IsChecked = true;
                    historyMI.Click += historyMI_OnClick;

                    TextBlock toolTipTextBlock = new TextBlock();
                    toolTipTextBlock.Text = historyEntry;
                    toolTipTextBlock.TextWrapping = TextWrapping.Wrap;
                    historyMI.ToolTip = toolTipTextBlock;
                    ToolTipService.SetShowDuration(historyMI, 3600000);

                    CmdArgsCtxMenu_HistoryMenu.Items.Add(historyMI);
                }

                CmdArgsCtxMenu_HistoryMenu.Header = string.Format("History ({0}/{1})", CmdArgsCtxMenu_HistoryMenu.Items.Count, History.Size);

                CmdArgsCtxMenu_HistoryMenu.UpdateLayout();
            }
        }

        private void historyMI_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            SetCommandArgs(((TextBlock)mi.Header).Text);

            foreach (MenuItem historyMI in CmdArgsCtxMenu_HistoryMenu.Items)
            {
                if (((TextBlock)historyMI.Header).Text == ((TextBlock)mi.Header).Text)
                    historyMI.IsChecked = true;
                else
                    historyMI.IsChecked = false;
            }

            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;
        }

        #endregion Context Menu

        #region Core Functionality

        private string GetCommandArgs()
        {
            IVsHierarchy startupProjHierarchy = GetStartupProjectHierarchy();
            EnvDTE.Properties props = GetDtePropertiesFromHierarchy(startupProjHierarchy);
            if (props == null)
                return "Disabled; no solution loaded";
            return GetProperty(props, "CommandArguments") as string ?? string.Empty;
        }

        private void SetCommandArgs(string value)
        {
            IVsHierarchy startupProjHierarchy = GetStartupProjectHierarchy();
            EnvDTE.Properties props = GetDtePropertiesFromHierarchy(startupProjHierarchy);
            if (props == null)
                return;
            SetProperty(props, "CommandArguments", value);
        }

        private void AddToHistory(string value)
        {
            if (value.Trim() != string.Empty && !History.Contains(value))
                History.Enqueue(value);
        }

        #endregion Core Functionality

        #region Base Utility Functions

        private IVsHierarchy GetStartupProjectHierarchy()
        {
            IVsSolutionBuildManager build = DockableCLArgsPackage.GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            IVsHierarchy hierarchy;

            if (ErrorHandler.Failed(build.get_StartupProject(out hierarchy)))
                return null;

            return hierarchy;
        }

        private EnvDTE.Properties GetDtePropertiesFromHierarchy(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
                return null;

            object projectobj;
            if (ErrorHandler.Failed(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectobj)))
                return null;

            EnvDTE.Project project = projectobj as EnvDTE.Project;
            if (project == null)
                return null;

            EnvDTE.ConfigurationManager configManager = project.ConfigurationManager;
            if (configManager == null)
                return null;

            EnvDTE.Configuration activeConfig = configManager.ActiveConfiguration;
            if (activeConfig == null)
                return null;

            return activeConfig.Properties;
        }

        private object GetProperty(EnvDTE.Properties properties, string name)
        {
            if (properties == null || string.IsNullOrEmpty(name))
                return null;

            try
            {
                EnvDTE.Property property = properties.Cast<EnvDTE.Property>().FirstOrDefault(p => p.Name == name);
                if (property == null)
                    return null;

                return property.Value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool SetProperty(EnvDTE.Properties properties, string name, object value)
        {
            if (properties == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                EnvDTE.Property property = properties.Cast<EnvDTE.Property>().FirstOrDefault(p => p.Name == name);
                if (property == null)
                    return false;

                property.Value = value;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion Base Utility Functions
    }
}