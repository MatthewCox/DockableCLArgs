using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
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
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using MattC.DockableCLArgs.Properties;

namespace MattC.DockableCLArgs
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

        private enum LANG
        {
            CS,
            CPP,
            UNKNOWN
        }

        private static LANG lang;

        private FixedSizeQueue<string> history = new FixedSizeQueue<string>(10);
        public FixedSizeQueue<string> History
        {
            get { return history; }
        }

        public DockableCLArgsControl()
        {
            InitializeComponent();

            lang = LANG.UNKNOWN;

            dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            debugEvents = dte.Events.DebuggerEvents;
            debugEvents.OnEnterRunMode += DebuggerEvents_OnEnterRunMode;

            solutionEvents = dte.Events.SolutionEvents;
            solutionEvents.Opened += SolutionEvents_OnOpened;
            solutionEvents.AfterClosing += SolutionEvents_OnAfterClosing;

            Properties.Settings.Default.PropertyChanged += OnSettingChanged;

            foreach (var commandBinding in CmdArgs.TextArea.CommandBindings.Cast<CommandBinding>())
            {
                if (commandBinding.Command == ApplicationCommands.Paste)
                {
                    commandBinding.PreviewCanExecute += new CanExecuteRoutedEventHandler(pasteCommandBinding_PreviewCanExecute);
                    break;
                }
            }

            SetPlainTextColours();

            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = false;
        }

        #region TextBox Events

        private void OnTextChanged(object sender, EventArgs e)
        {
            if (runChangedHandler)
            {
                SetCommandArgs(CmdArgs.Text);
            }
        }

        private void OnFocus(object sender, RoutedEventArgs e)
        {
            SetPlainTextColours();

            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent newlines from being entered
            if (e.Key == Key.Return)
            {
                e.Handled = true;
            }
        }

        // Replace newlines in pasted text data
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Clipboard.SetText(System.String)")]
        private void pasteCommandBinding_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var dataObject = Clipboard.GetDataObject();
            var text = (string)dataObject.GetData(DataFormats.UnicodeText);
            text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);

            if (text.Contains(Environment.NewLine))
            {
                e.CanExecute = false;
                e.Handled = true;
                text = text.Replace(Environment.NewLine, " ");
                Clipboard.SetText(text);
                CmdArgs.Paste();
            }
        }

        #endregion TextBox Events

        #region Addin Events

        private void OnSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SetPlainTextColours();
        }

        #endregion Addin Events

        #region IDE Events

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "ICSharpCode.AvalonEdit.TextEditor.set_Text(System.String)")]
        private void SolutionEvents_OnOpened()
        {
            lang = LANG.UNKNOWN;

            IVsHierarchy startupProjHierarchy = GetStartupProjectHierarchy();
            EnvDTE.Properties props = GetDtePropertiesFromHierarchy(startupProjHierarchy);
            string commandArgs = GetProperty(props, "CommandArguments") as string ?? string.Empty;
            if (String.IsNullOrEmpty(commandArgs))
            {
                commandArgs = GetProperty(props, "StartArguments") as string ?? string.Empty;
                if (!String.IsNullOrEmpty(commandArgs))
                    lang = LANG.CS;
            }
            else
                lang = LANG.CPP;

            if (lang != LANG.UNKNOWN)
            {
                runChangedHandler = false;
                CmdArgs.Text = GetCommandArgs();
                runChangedHandler = true;

                CmdArgs.IsEnabled = true;
            }
            else
            {
                runChangedHandler = false;
                CmdArgs.Text = "Project type unsupported (C++, C#, and VB are supported (VB untested))";
                runChangedHandler = true;
            }

            SetPlainTextColours();
        }

        private void SolutionEvents_OnAfterClosing()
        {
            lang = LANG.UNKNOWN;

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

            if (String.IsNullOrEmpty(CmdArgs.SelectedText))
            {
                CmdArgsCtxMenu_Cut.IsEnabled = false;
                CmdArgsCtxMenu_Copy.IsEnabled = false;
            }
            if (!Clipboard.ContainsText())
            {
                CmdArgsCtxMenu_Paste.IsEnabled = false;
            }

            if (history.Count > 0)
            {
                CmdArgsCtxMenu_HistoryMenu.IsEnabled = true;

                CmdArgsCtxMenu_HistoryMenu.Items.Clear();
                foreach (string historyEntry in history)
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

                CmdArgsCtxMenu_HistoryMenu.Header = string.Format(CultureInfo.CurrentCulture, "History ({0}/{1})", CmdArgsCtxMenu_HistoryMenu.Items.Count, history.Size);

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

        private static string GetCommandArgs()
        {
            IVsHierarchy startupProjHierarchy = GetStartupProjectHierarchy();
            EnvDTE.Properties props = GetDtePropertiesFromHierarchy(startupProjHierarchy);
            if (props == null)
                return "Disabled; no solution loaded";

            string commandArgs = string.Empty;
            switch (lang)
            {
                case LANG.CPP:
                    commandArgs = GetProperty(props, "CommandArguments") as string ?? string.Empty;
                    break;
                case LANG.CS:
                    commandArgs = GetProperty(props, "StartArguments") as string ?? string.Empty;
                    break;
            }
                
            return commandArgs;
        }

        private static void SetCommandArgs(string value)
        {
            IVsHierarchy startupProjHierarchy = GetStartupProjectHierarchy();
            EnvDTE.Properties props = GetDtePropertiesFromHierarchy(startupProjHierarchy);
            if (props == null)
                return;
            switch (lang)
            {
                case LANG.CPP:
                    SetProperty(props, "CommandArguments", value);
                    break;
                case LANG.CS:
                    SetProperty(props, "StartArguments", value);
                    break;
            }
        }

        private void AddToHistory(string value)
        {
            if (!String.IsNullOrEmpty(value.Trim()) && !history.Contains(value))
                history.Enqueue(value);
        }

        #endregion Core Functionality

        #region Base Utility Functions

        private static IVsHierarchy GetStartupProjectHierarchy()
        {
            IVsSolutionBuildManager build = DockableCLArgsPackage.GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            IVsHierarchy hierarchy;

            if (ErrorHandler.Failed(build.get_StartupProject(out hierarchy)))
                return null;

            return hierarchy;
        }

        private static EnvDTE.Properties GetDtePropertiesFromHierarchy(IVsHierarchy hierarchy)
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

        private static object GetProperty(EnvDTE.Properties properties, string name)
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
            catch (InvalidCastException)
            {
                return null;
            }
        }

        private static bool SetProperty(EnvDTE.Properties properties, string name, object value)
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
            catch (InvalidCastException)
            {
                return false;
            }
        }

        private static FontsAndColorsItems GetTextEditorFontAndColorsItems(DTE2 dte)
        {
            EnvDTE.Properties props = dte.get_Properties("FontsAndColors", "TextEditor");
            return props.Item("FontsAndColorsItems").Object as FontsAndColorsItems;
        }

        private static System.Windows.Media.Color GetBackgroundColourOf(FontsAndColorsItems faci, string item)
        {
            Int32 oleColor = System.Convert.ToInt32(faci.Item(item).Background);
            System.Drawing.Color sdColor = System.Drawing.ColorTranslator.FromOle(oleColor);
            System.Windows.Media.Color backColor = System.Windows.Media.Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
            return backColor;
        }

        private static System.Windows.Media.Color GetForegroundColourOf(FontsAndColorsItems faci, string item)
        {
            Int32 oleColor = System.Convert.ToInt32(faci.Item(item).Foreground);
            System.Drawing.Color sdColor = System.Drawing.ColorTranslator.FromOle(oleColor);
            System.Windows.Media.Color foreColor = System.Windows.Media.Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
            return foreColor;
        }

        //private static bool GetBoldednessOf(FontsAndColorsItems faci, string item)
        //{
        //    return faci.Item(item).Bold;
        //}

        private void SetPlainTextColours()
        {
            FontsAndColorsItems faci = GetTextEditorFontAndColorsItems(dte);
            Color back = GetBackgroundColourOf(faci, "Plain Text");
            Color fore = GetForegroundColourOf(faci, "Plain Text");
            CmdArgs.Background = new SolidColorBrush(back);
            CmdArgs.Foreground = new SolidColorBrush(fore);
            
            if (IsLightTheme(back))
                CmdArgs.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Resources.CmdArgs-light.xshd");
            else
                CmdArgs.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Resources.CmdArgs-dark.xshd");

            Color digitColor;
            Color optionColor;
            Color subOptionColor;
            if (IsLightTheme(back))
            {
                digitColor = ConvertToMediaColor(Properties.Settings.Default.DigitColorLight);
                optionColor = ConvertToMediaColor(Properties.Settings.Default.OptionColorLight);
                subOptionColor = ConvertToMediaColor(Properties.Settings.Default.SubOptionColorLight);
            }
            else
            {
                digitColor = ConvertToMediaColor(Properties.Settings.Default.DigitColorDark);
                optionColor = ConvertToMediaColor(Properties.Settings.Default.OptionColorDark);
                subOptionColor = ConvertToMediaColor(Properties.Settings.Default.SubOptionColorDark);
            }
            foreach (HighlightingColor hColor in CmdArgs.SyntaxHighlighting.NamedHighlightingColors)
            {
                switch (hColor.Name)
                {
                    case "Digits":
                        hColor.Foreground = new SimpleHighlightingBrush(digitColor);
                        break;
                    case "Option":
                        hColor.Foreground = new SimpleHighlightingBrush(optionColor);
                        break;
                    case "SubOption":
                        hColor.Foreground = new SimpleHighlightingBrush(subOptionColor);
                        break;
                }
            }
        }

        private static bool IsLightTheme(System.Windows.Media.Color plainTextBackgroundColour)
        {
            Color ptbc = plainTextBackgroundColour;
            return ptbc.R + ptbc.G + ptbc.B > (128 * 3);
        }

        private static System.Windows.Media.Color ConvertToMediaColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        #endregion Base Utility Functions
    }
}