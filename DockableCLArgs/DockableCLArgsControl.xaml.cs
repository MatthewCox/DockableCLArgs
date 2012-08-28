using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using ColorPickerControls.Chips;
using ColorPickerControls.Pickers;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

using MattC.DockableCLArgs.Properties;

namespace MattC.DockableCLArgs
{
    /// <summary>
    /// Interaction logic for DockableCLArgsControl.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
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

        private FixedSizeQueue<string> history;
        public FixedSizeQueue<string> History
        {
            get { return history; }
        }

        private Color prevOptionColour;
        private Color prevSubOptionColour;
        private Color prevArgumentColour;
        private Color prevDigitColour;

        public DockableCLArgsControl()
        {
            InitializeComponent();

            history = new FixedSizeQueue<string>(Settings.Default.HistorySize);

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

            OptionsHistorySize.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, DisablePasteEventHandler));

            SetPlainTextColours();

            runChangedHandler = false;
            CmdArgs.Text = GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = false;

            try
            {
                Init();
            }
            catch (Exception)
            {
                // Don't care if it fails
            }
        }

        private void DisablePasteEventHandler(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Init()
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
                CmdArgs.Text = DockableCLArgs.Resources.UnsupportedProjectType;
                runChangedHandler = true;
            }

            SetPlainTextColours();
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
            Init();
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

        private void OnCmdArgsCtxMenu_Options(object sender, RoutedEventArgs e)
        {
            if (OptionsPane.Visibility == System.Windows.Visibility.Collapsed)
                OpenOptionsPane();
            else
            {
                ResetColours();
                CloseOptionsPane();
            }
        }

        #endregion Context Menu

        #region Options Pane

        private void OpenOptionsPane()
        {
            CmdArgsCtxMenu_Options.IsChecked = true;

            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(dte);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");

            ColorChip colChip_Options = new ColorChip();
            colChip_Options.Name = "OptionsColours_Options";
            colChip_Options.Color = ColUtils.ConvertToMediaColor(IsLightTheme(back) ? Settings.Default.OptionColorLight : Settings.Default.OptionColorDark);
            prevOptionColour = colChip_Options.Color;
            Label colChipLabel_Options = new Label();
            colChipLabel_Options.Content = "Options";
            colChipLabel_Options.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Options, Dock.Left);
            DockPanel.SetDock(colChipLabel_Options, Dock.Left);
            OptionsColoursDock_Options.Children.Add(colChip_Options);
            OptionsColoursDock_Options.Children.Add(colChipLabel_Options);

            ColorChip colChip_SubOptions = new ColorChip();
            colChip_SubOptions.Name = "OptionsColours_SubOptions";
            colChip_SubOptions.Color = ColUtils.ConvertToMediaColor(IsLightTheme(back) ? Settings.Default.SubOptionColorLight : Settings.Default.SubOptionColorDark);
            prevSubOptionColour = colChip_SubOptions.Color;
            Label colChipLabel_SubOptions = new Label();
            colChipLabel_SubOptions.Content = "Sub Options";
            colChipLabel_SubOptions.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_SubOptions, Dock.Left);
            DockPanel.SetDock(colChipLabel_SubOptions, Dock.Left);
            OptionsColoursDock_SubOptions.Children.Add(colChip_SubOptions);
            OptionsColoursDock_SubOptions.Children.Add(colChipLabel_SubOptions);

            ColorChip colChip_Arguments = new ColorChip();
            colChip_Arguments.Name = "OptionsColours_Arguments";
            colChip_Arguments.Color = ColUtils.ConvertToMediaColor(IsLightTheme(back) ? Settings.Default.ArgumentColorLight : Settings.Default.ArgumentColorDark);
            prevArgumentColour = colChip_Arguments.Color;
            Label colChipLabel_Arguments = new Label();
            colChipLabel_Arguments.Content = "Arguments";
            colChipLabel_Arguments.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Arguments, Dock.Left);
            DockPanel.SetDock(colChipLabel_Arguments, Dock.Left);
            OptionsColoursDock_Arguments.Children.Add(colChip_Arguments);
            OptionsColoursDock_Arguments.Children.Add(colChipLabel_Arguments);

            ColorChip colChip_Digits = new ColorChip();
            colChip_Digits.Name = "OptionsColours_Digits";
            colChip_Digits.Color = ColUtils.ConvertToMediaColor(IsLightTheme(back) ? Settings.Default.DigitColorLight : Settings.Default.DigitColorDark);
            prevDigitColour = colChip_Digits.Color;
            Label colChipLabel_Digits = new Label();
            colChipLabel_Digits.Content = "Digits";
            colChipLabel_Digits.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Digits, Dock.Left);
            DockPanel.SetDock(colChipLabel_Digits, Dock.Left);
            OptionsColoursDock_Digits.Children.Add(colChip_Digits);
            OptionsColoursDock_Digits.Children.Add(colChipLabel_Digits);

            ColorPickerStandard colPicker = new ColorPickerStandard();
            colPicker.InitialColor = colChip_Options.Color;
            colPicker.SelectedColor = colChip_Options.Color;
            colPicker.SelectedColorChanged += colPicker_SelectedColorChanged;
            colPicker.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            ColourPanel.Children.Add(colPicker);

            OptionsColours_Options.IsChecked = true;

            OptionsHistorySize.Text = Settings.Default.HistorySize.ToString(CultureInfo.CurrentCulture);

            OptionsPane.Visibility = System.Windows.Visibility.Visible;
        }

        private void CloseOptionsPane()
        {
            CmdArgsCtxMenu_Options.IsChecked = false;

            OptionsPane.Visibility = System.Windows.Visibility.Collapsed;

            ColourPanel.Children.Clear();

            List<DockPanel> docks = new List<DockPanel>()
            {
                OptionsColoursDock_Options, OptionsColoursDock_SubOptions, OptionsColoursDock_Arguments, OptionsColoursDock_Digits
            };

            foreach (DockPanel dock in docks)
            {
                dock.Children.Clear();
            }
        }

        private void OnColourRadio_Checked(object sender, RoutedEventArgs e)
        {
            List<RadioButton> radios = new List<RadioButton>()
            {
                OptionsColours_Options, OptionsColours_SubOptions, OptionsColours_Arguments, OptionsColours_Digits
            };

            foreach (RadioButton rad in radios)
            {
                if (rad.IsChecked == true)
                {
                    ColourPanel.Children.OfType<ColorPickerStandard>().First().InitialColor = ((DockPanel)rad.Content).Children.OfType<ColorChip>().First().Color;
                    ColourPanel.Children.OfType<ColorPickerStandard>().First().SelectedColor = ((DockPanel)rad.Content).Children.OfType<ColorChip>().First().Color;
                }
            }
        }

        private void colPicker_SelectedColorChanged(object sender, ColorPicker.EventArgs<Color> e)
        {
            List<RadioButton> radios = new List<RadioButton>()
            {
                OptionsColours_Options, OptionsColours_SubOptions, OptionsColours_Arguments, OptionsColours_Digits
            };

            RadioButton rad = radios.First(c => c.IsChecked == true);
            ((DockPanel)rad.Content).Children.OfType<ColorChip>().First().Color = e.Value;

            System.Drawing.Color col = ColUtils.ConvertToDrawingColor(e.Value);

            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(dte);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");
            if (IsLightTheme(back))
            {
                if (rad == OptionsColours_Options) Settings.Default.OptionColorLight = col;
                else if (rad == OptionsColours_SubOptions) Settings.Default.SubOptionColorLight = col;
                else if (rad == OptionsColours_Arguments) Settings.Default.ArgumentColorLight = col;
                else if (rad == OptionsColours_Digits) Settings.Default.DigitColorLight = col;
            }
            else
            {
                if (rad == OptionsColours_Options) Settings.Default.OptionColorDark = col;
                else if (rad == OptionsColours_SubOptions) Settings.Default.SubOptionColorDark = col;
                else if (rad == OptionsColours_Arguments) Settings.Default.ArgumentColorDark = col;
                else if (rad == OptionsColours_Digits) Settings.Default.DigitColorDark = col;
            }
        }

        private void ResetColours()
        {
            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(dte);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");
            if (IsLightTheme(back))
            {
                Settings.Default.OptionColorLight = ColUtils.ConvertToDrawingColor(prevOptionColour);
                Settings.Default.SubOptionColorLight = ColUtils.ConvertToDrawingColor(prevSubOptionColour);
                Settings.Default.ArgumentColorLight = ColUtils.ConvertToDrawingColor(prevArgumentColour);
                Settings.Default.DigitColorLight = ColUtils.ConvertToDrawingColor(prevDigitColour);
            }
            else
            {
                Settings.Default.OptionColorDark = ColUtils.ConvertToDrawingColor(prevOptionColour);
                Settings.Default.SubOptionColorDark = ColUtils.ConvertToDrawingColor(prevSubOptionColour);
                Settings.Default.ArgumentColorDark = ColUtils.ConvertToDrawingColor(prevArgumentColour);
                Settings.Default.DigitColorDark = ColUtils.ConvertToDrawingColor(prevDigitColour);
            }
        }

        private void OnHistorySize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void OnApply_Click(object sender, RoutedEventArgs e)
        {
            uint newHistorySize = Settings.Default.HistorySize;
            if (uint.TryParse(OptionsHistorySize.Text, out newHistorySize))
            {
                history.Resize(newHistorySize);
                Settings.Default.HistorySize = newHistorySize;
            }

            CloseOptionsPane();
        }

        private void OnCancel_Click(object sender, RoutedEventArgs e)
        {
            ResetColours();
            CloseOptionsPane();
        }

        #endregion Options Pane

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "plainText")]
        public static bool IsLightTheme(System.Windows.Media.Color plainTextBackgroundColour)
        {
            System.Windows.Media.Color ptbc = plainTextBackgroundColour;
            return ptbc.R + ptbc.G + ptbc.B > (128 * 3);
        }

        private void SetPlainTextColours()
        {
            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(dte);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");
            CmdArgs.Background = new SolidColorBrush(back);

            if (IsLightTheme(back))
                CmdArgs.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Resources.CmdArgs-light.xshd");
            else
                CmdArgs.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Resources.CmdArgs-dark.xshd");

            Color digitColor;
            Color optionColor;
            Color subOptionColor;
            Color argumentColor;
            if (IsLightTheme(back))
            {
                digitColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.DigitColorLight);
                optionColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.OptionColorLight);
                subOptionColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.SubOptionColorLight);
                argumentColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.ArgumentColorLight);
            }
            else
            {
                digitColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.DigitColorDark);
                optionColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.OptionColorDark);
                subOptionColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.SubOptionColorDark);
                argumentColor = ColUtils.ConvertToMediaColor(Properties.Settings.Default.ArgumentColorDark);
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
            CmdArgs.Foreground = new SolidColorBrush(argumentColor);
        }

        #endregion Base Utility Functions
    }
}