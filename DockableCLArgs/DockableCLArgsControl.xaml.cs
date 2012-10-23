using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using EnvDTE;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace MattC.DockableCLArgs
{
    /// <summary>
    /// Interaction logic for DockableCLArgsControl.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public partial class DockableCLArgsControl : UserControl
    {
        #region Member Variables

        private IDEUtils.ProjLang lang = IDEUtils.ProjLang.UNKNOWN;

        private bool runChangedHandler = true;

        private DebuggerEvents debugEvents;
        private SolutionEvents solutionEvents;

        private NamedArgsDictionary savedArgs;
        public NamedArgsDictionary SavedArgs
        {
            get { return savedArgs; }
        }

        #endregion Member Variables

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public DockableCLArgsControl()
        {
            InitializeComponent();

            savedArgs = new NamedArgsDictionary();

            debugEvents = IDEUtils.DTE.Events.DebuggerEvents;
            debugEvents.OnEnterRunMode += DebuggerEvents_OnEnterRunMode;

            solutionEvents = IDEUtils.DTE.Events.SolutionEvents;
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

            SetTextBoxProperties();

            runChangedHandler = false;
            CmdArgs.Text = IDEUtils.GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = false;

            try
            {
                Init();
            }
            catch (Exception)
            {
                throw;// Don't care if it fails here
            }
        }

        private void Init()
        {
            lang = IDEUtils.LANG;

            CmdArgs.FontFamily = new FontFamily(Properties.Settings.Default.Font.FontFamily.Name);
            CmdArgs.FontSize = Properties.Settings.Default.Font.SizeInPoints;

            if (lang != IDEUtils.ProjLang.UNKNOWN)
            {
                runChangedHandler = false;
                CmdArgs.Text = IDEUtils.GetCommandArgs();
                runChangedHandler = true;

                CmdArgs.IsEnabled = true;

                History.Init();

                savedArgs.Path = Path.Combine(IDEUtils.GetStartupProjectDirectory(), "DockableCLArgsSavedArgs.user");
                savedArgs.Load();
            }
            else
            {
                runChangedHandler = false;
                CmdArgs.Text = DockableCLArgs.Resources.UnsupportedProjectType;
                runChangedHandler = true;
            }

            SetTextBoxProperties();
        }

        #region TextBox Events

        private void OnTextChanged(object sender, EventArgs e)
        {
            if (runChangedHandler)
            {
                IDEUtils.SetCommandArgs(CmdArgs.Text);
            }
        }

        private void OnFocus(object sender, RoutedEventArgs e)
        {
            SetTextBoxProperties();

            runChangedHandler = false;
            CmdArgs.Text = IDEUtils.GetCommandArgs();
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
            SetTextBoxProperties();
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
            lang = IDEUtils.ProjLang.UNKNOWN;

            runChangedHandler = false;
            CmdArgs.Text = IDEUtils.GetCommandArgs();
            runChangedHandler = true;

            CmdArgs.IsEnabled = false;
        }

        private void DebuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            History.AddToHistory(CmdArgs.Text);
        }

        #endregion IDE Events

        #region Context Menu

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private void BuildContextMenu()
        {
            CmdArgsCtxMenu_Cut.IsEnabled = true;
            CmdArgsCtxMenu_Copy.IsEnabled = true;
            CmdArgsCtxMenu_Paste.IsEnabled = true;

            CmdArgsCtxMenu_SavedMenu.IsEnabled = false;
            CmdArgsCtxMenu_SavedMenu.Header = "Saved";
            CmdArgsCtxMenu_HistoryMenu.IsEnabled = false;
            CmdArgsCtxMenu_HistoryMenu.Header = "History";

            if (String.IsNullOrEmpty(CmdArgs.SelectedText))
            {
                CmdArgsCtxMenu_Cut.IsEnabled = false;
                CmdArgsCtxMenu_Copy.IsEnabled = false;
            }
            if (!Clipboard.ContainsText())
            {
                CmdArgsCtxMenu_Paste.IsEnabled = false;
            }

            if (savedArgs.Count > 0)
            {
                CmdArgsCtxMenu_SavedMenu.IsEnabled = true;

                CmdArgsCtxMenu_SavedMenu.Items.Clear();
                foreach (KeyValuePair<string, string> kvp in savedArgs)
                {
                    MenuItem savedMI = new MenuItem();

                    DockPanel savedMIPanel = new DockPanel();
                    savedMIPanel.LastChildFill = false;
                    savedMIPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = kvp.Key;
                    savedMIPanel.Children.Add(textBlock);

                    Button deleteButton = new Button();
                    deleteButton.Content = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri("Resources/Delete.png", UriKind.Relative))
                    };
                    deleteButton.Margin = new Thickness(20, 0, 0, 0);
                    deleteButton.Click += deleteButton_OnClick;
                    savedMIPanel.Children.Add(deleteButton);

                    savedMI.Header = savedMIPanel;
                    savedMI.Name = "SavedEntry";
                    savedMI.IsCheckable = true;
                    if (kvp.Value == IDEUtils.GetCommandArgs())
                        savedMI.IsChecked = true;
                    savedMI.Click += savedMI_OnClick;

                    TextBlock toolTipTextBlock = new TextBlock();
                    toolTipTextBlock.Text = kvp.Value;
                    toolTipTextBlock.TextWrapping = TextWrapping.Wrap;
                    savedMI.ToolTip = toolTipTextBlock;
                    ToolTipService.SetShowDuration(savedMI, 3600000);

                    CmdArgsCtxMenu_SavedMenu.Items.Add(savedMI);
                }

                CmdArgsCtxMenu_SavedMenu.Header = string.Format(CultureInfo.CurrentCulture, "Saved ({0})", CmdArgsCtxMenu_SavedMenu.Items.Count);

                CmdArgsCtxMenu_SavedMenu.UpdateLayout();
            }

            if (History.GetHistory.Count > 0)
            {
                CmdArgsCtxMenu_HistoryMenu.IsEnabled = true;

                CmdArgsCtxMenu_HistoryMenu.Items.Clear();
                foreach (string historyEntry in History.GetHistory)
                {
                    MenuItem historyMI = new MenuItem();
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = historyEntry;
                    historyMI.Header = textBlock;
                    historyMI.Name = "HistoryEntry";
                    historyMI.IsCheckable = true;
                    if (historyEntry == IDEUtils.GetCommandArgs())
                        historyMI.IsChecked = true;
                    historyMI.Click += historyMI_OnClick;

                    TextBlock toolTipTextBlock = new TextBlock();
                    toolTipTextBlock.Text = historyEntry;
                    toolTipTextBlock.TextWrapping = TextWrapping.Wrap;
                    historyMI.ToolTip = toolTipTextBlock;
                    ToolTipService.SetShowDuration(historyMI, 3600000);

                    CmdArgsCtxMenu_HistoryMenu.Items.Add(historyMI);
                }

                CmdArgsCtxMenu_HistoryMenu.Header = string.Format(CultureInfo.CurrentCulture, "History ({0}/{1})", CmdArgsCtxMenu_HistoryMenu.Items.Count, History.GetHistory.Size);

                CmdArgsCtxMenu_HistoryMenu.UpdateLayout();
            }
        }

        private void OnCmdArgsCtxMenu_Opened(object sender, RoutedEventArgs e)
        {
            BuildContextMenu();
        }

        private void OnCmdArgsCtxMenu_Cut(object sender, RoutedEventArgs e)
        {
            CmdArgs.Cut();
            IDEUtils.SetCommandArgs(CmdArgs.Text);
        }

        private void OnCmdArgsCtxMenu_Copy(object sender, RoutedEventArgs e)
        {
            CmdArgs.Copy();
        }

        private void OnCmdArgsCtxMenu_Paste(object sender, RoutedEventArgs e)
        {
            CmdArgs.Paste();
            IDEUtils.SetCommandArgs(CmdArgs.Text);
        }

        private void OnCmdArgsCtxMenu_Save(object sender, RoutedEventArgs e)
        {
            ShowSaveDialogue();
        }

        private void savedMI_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CmdArgsCtxMenu_SavedMenu.IsEnabled)
                return;

            MenuItem mi = sender as MenuItem;
            string args = ((TextBlock)mi.ToolTip).Text;
            IDEUtils.SetCommandArgs(args);

            foreach (MenuItem savedMI in CmdArgsCtxMenu_SavedMenu.Items)
            {
                if (((TextBlock)savedMI.ToolTip).Text == args)
                    savedMI.IsChecked = true;
                else
                    savedMI.IsChecked = false;
            }

            runChangedHandler = false;
            CmdArgs.Text = IDEUtils.GetCommandArgs();
            runChangedHandler = true;
        }

        private void deleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            string arg = (((DockPanel)((Button)sender).Parent).Children.OfType<TextBlock>().First().Text);
            savedArgs.Remove(arg);
            savedArgs.Save();

            BuildContextMenu();
        }

        private void historyMI_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            string args = ((TextBlock)mi.Header).Text;
            IDEUtils.SetCommandArgs(args);

            foreach (MenuItem historyMI in CmdArgsCtxMenu_HistoryMenu.Items)
            {
                if (((TextBlock)historyMI.Header).Text == args)
                    historyMI.IsChecked = true;
                else
                    historyMI.IsChecked = false;
            }

            runChangedHandler = false;
            CmdArgs.Text = IDEUtils.GetCommandArgs();
            runChangedHandler = true;
        }

        private void OnCmdArgsCtxMenu_Options(object sender, RoutedEventArgs e)
        {
            OptionsDialogue options = new OptionsDialogue();
            options.ShowDialog();
        }

        #endregion Context Menu

        #region Save Dialogue

        private void SaveInputComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(this, new RoutedEventArgs());
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (savedArgs.ContainsKey(SaveInputComboBox.Text))
            {
                if (savedArgs[SaveInputComboBox.Text] != CmdArgs.Text)
                {
                    MessageBoxResult result = MessageBox.Show(DockableCLArgs.Resources.NamedArgsExistMessage, DockableCLArgs.Resources.NamedArgsExistTitle, MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) return;
                }
                savedArgs[SaveInputComboBox.Text] = CmdArgs.Text;
            }
            else
            {
                savedArgs.Add(SaveInputComboBox.Text, CmdArgs.Text);
            }
            savedArgs.Save();

            HideSaveDialogue();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            HideSaveDialogue();
        }

        private void ShowSaveDialogue()
        {
            SaveInputBox.Visibility = System.Windows.Visibility.Visible;
            SaveInputComboBox.Focus();

            SaveInputComboBox.Items.Clear();
            foreach (KeyValuePair<string, string> kvp in savedArgs)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = kvp.Key;
                SaveInputComboBox.Items.Add(cbi);
            }
        }

        private void HideSaveDialogue()
        {
            SaveInputBox.Visibility = System.Windows.Visibility.Collapsed;
            SaveInputComboBox.Text = string.Empty;
            CmdArgs.Focus();
        }

        #endregion Save Dialogue

        #region Base Utility Functions

        private void SetTextBoxProperties()
        {
            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(IDEUtils.DTE);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");
            CmdArgs.Background = new SolidColorBrush(back);

            if (ColUtils.IsLightTheme(back))
                CmdArgs.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Resources.CmdArgs-light.xshd");
            else
                CmdArgs.SyntaxHighlighting = ResourceLoader.LoadHighlightingDefinition("Resources.CmdArgs-dark.xshd");

            Color digitColor;
            Color optionColor;
            Color subOptionColor;
            Color argumentColor;
            if (ColUtils.IsLightTheme(back))
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

            CmdArgs.FontSize = Properties.Settings.Default.Font.SizeInPoints*(96.0/72.0);
            CmdArgs.FontFamily = new FontFamily(Properties.Settings.Default.Font.Name);
        }

        #endregion Base Utility Functions
    }
}