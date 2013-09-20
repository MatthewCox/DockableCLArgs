using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;

using ColorPickerControls.Chips;
using ColorPickerControls.Pickers;

using MattC.DockableCLArgs.Properties;

namespace MattC.DockableCLArgs
{
    /// <summary>
    /// Interaction logic for OptionsDialogue.xaml
    /// </summary>
    public partial class OptionsDialogue : DialogWindow
    {
        Color prevOptionColour, prevSubOptionColour, prevArgumentColour, prevDigitColour, prevStringColour;

        string prevFontName;
        float prevFontSize;

        public OptionsDialogue()
        {
            InitializeComponent();

            FontSizeSlider.ValueChanged += OnFontSizeSlider_ValueChanged;

            HistorySize.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, GeneralUtils.DisablePasteEventHandler));

            OpenOptionsDialogue();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private void OpenOptionsDialogue()
        {
            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(IDEUtils.DTE);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");

            ColorChip colChip_Options = new ColorChip();
            colChip_Options.Name = "Colours_Options";
            colChip_Options.Color = ColUtils.ConvertToMediaColor(ColUtils.IsLightTheme(back) ? Settings.Default.OptionColorLight : Settings.Default.OptionColorDark);
            prevOptionColour = colChip_Options.Color;
            Label colChipLabel_Options = new Label();
            colChipLabel_Options.Content = "Options";
            colChipLabel_Options.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Options, Dock.Left);
            DockPanel.SetDock(colChipLabel_Options, Dock.Left);
            ColoursDock_Options.Children.Add(colChip_Options);
            ColoursDock_Options.Children.Add(colChipLabel_Options);

            ColorChip colChip_SubOptions = new ColorChip();
            colChip_SubOptions.Name = "Colours_SubOptions";
            colChip_SubOptions.Color = ColUtils.ConvertToMediaColor(ColUtils.IsLightTheme(back) ? Settings.Default.SubOptionColorLight : Settings.Default.SubOptionColorDark);
            prevSubOptionColour = colChip_SubOptions.Color;
            Label colChipLabel_SubOptions = new Label();
            colChipLabel_SubOptions.Content = "Sub Options";
            colChipLabel_SubOptions.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_SubOptions, Dock.Left);
            DockPanel.SetDock(colChipLabel_SubOptions, Dock.Left);
            ColoursDock_SubOptions.Children.Add(colChip_SubOptions);
            ColoursDock_SubOptions.Children.Add(colChipLabel_SubOptions);

            ColorChip colChip_Arguments = new ColorChip();
            colChip_Arguments.Name = "Colours_Arguments";
            colChip_Arguments.Color = ColUtils.ConvertToMediaColor(ColUtils.IsLightTheme(back) ? Settings.Default.ArgumentColorLight : Settings.Default.ArgumentColorDark);
            prevArgumentColour = colChip_Arguments.Color;
            Label colChipLabel_Arguments = new Label();
            colChipLabel_Arguments.Content = "Arguments";
            colChipLabel_Arguments.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Arguments, Dock.Left);
            DockPanel.SetDock(colChipLabel_Arguments, Dock.Left);
            ColoursDock_Arguments.Children.Add(colChip_Arguments);
            ColoursDock_Arguments.Children.Add(colChipLabel_Arguments);

            ColorChip colChip_Digits = new ColorChip();
            colChip_Digits.Name = "Colours_Digits";
            colChip_Digits.Color = ColUtils.ConvertToMediaColor(ColUtils.IsLightTheme(back) ? Settings.Default.DigitColorLight : Settings.Default.DigitColorDark);
            prevDigitColour = colChip_Digits.Color;
            Label colChipLabel_Digits = new Label();
            colChipLabel_Digits.Content = "Digits";
            colChipLabel_Digits.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Digits, Dock.Left);
            DockPanel.SetDock(colChipLabel_Digits, Dock.Left);
            ColoursDock_Digits.Children.Add(colChip_Digits);
            ColoursDock_Digits.Children.Add(colChipLabel_Digits);

            ColorChip colChip_Strings = new ColorChip();
            colChip_Strings.Name = "Colours_Strings";
            colChip_Strings.Color = ColUtils.ConvertToMediaColor(ColUtils.IsLightTheme(back) ? Settings.Default.StringColorLight : Settings.Default.StringColorDark);
            prevStringColour = colChip_Strings.Color;
            Label colChipLabel_Strings = new Label();
            colChipLabel_Strings.Content = "Strings";
            colChipLabel_Strings.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            DockPanel.SetDock(colChip_Strings, Dock.Left);
            DockPanel.SetDock(colChipLabel_Strings, Dock.Left);
            ColoursDock_Strings.Children.Add(colChip_Strings);
            ColoursDock_Strings.Children.Add(colChipLabel_Strings);

            ColorPickerStandard colPicker = new ColorPickerStandard();
            colPicker.InitialColor = colChip_Options.Color;
            colPicker.SelectedColor = colChip_Options.Color;
            colPicker.SelectedColorChanged += colPicker_SelectedColorChanged;
            colPicker.Foreground = new SolidColorBrush(ColUtils.GetForegroundColourOf(faci, "Plain Text"));
            ColourPanel.Children.Add(colPicker);

            Colours_Options.IsChecked = true;

            HistorySize.Text = Settings.Default.HistorySize.ToString(CultureInfo.CurrentCulture);

            prevFontName = Settings.Default.Font.Name;
            prevFontSize = Settings.Default.Font.SizeInPoints;

            FontCombo.ItemsSource = Fonts.SystemFontFamilies.OrderBy(s => s.ToString());
            FontCombo.SelectedItem = Fonts.SystemFontFamilies.OrderBy(s => s.ToString()).First(s => s.Source == Settings.Default.Font.Name);

            FontSizeSlider.Value = Settings.Default.Font.SizeInPoints;
        }

        private void Clear()
        {
            ColourPanel.Children.Clear();

            List<DockPanel> docks = new List<DockPanel>()
            {
                ColoursDock_Options, ColoursDock_SubOptions, ColoursDock_Arguments, ColoursDock_Digits, ColoursDock_Strings
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
                Colours_Options, Colours_SubOptions, Colours_Arguments, Colours_Digits, Colours_Strings
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
                Colours_Options, Colours_SubOptions, Colours_Arguments, Colours_Digits, Colours_Strings
            };

            RadioButton rad = radios.First(c => c.IsChecked == true);
            ((DockPanel)rad.Content).Children.OfType<ColorChip>().First().Color = e.Value;

            System.Drawing.Color col = ColUtils.ConvertToDrawingColor(e.Value);

            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(IDEUtils.DTE);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");
            if (ColUtils.IsLightTheme(back))
            {
                if (rad == Colours_Options) Settings.Default.OptionColorLight = col;
                else if (rad == Colours_SubOptions) Settings.Default.SubOptionColorLight = col;
                else if (rad == Colours_Arguments) Settings.Default.ArgumentColorLight = col;
                else if (rad == Colours_Digits) Settings.Default.DigitColorLight = col;
                else if (rad == Colours_Strings) Settings.Default.StringColorLight = col;
            }
            else
            {
                if (rad == Colours_Options) Settings.Default.OptionColorDark = col;
                else if (rad == Colours_SubOptions) Settings.Default.SubOptionColorDark = col;
                else if (rad == Colours_Arguments) Settings.Default.ArgumentColorDark = col;
                else if (rad == Colours_Digits) Settings.Default.DigitColorDark = col;
                else if (rad == Colours_Strings) Settings.Default.StringColorDark = col;
            }
        }

        private void ResetOptions()
        {
            FontsAndColorsItems faci = ColUtils.GetTextEditorFontAndColorsItems(IDEUtils.DTE);
            Color back = ColUtils.GetBackgroundColourOf(faci, "Plain Text");
            if (ColUtils.IsLightTheme(back))
            {
                Settings.Default.OptionColorLight = ColUtils.ConvertToDrawingColor(prevOptionColour);
                Settings.Default.SubOptionColorLight = ColUtils.ConvertToDrawingColor(prevSubOptionColour);
                Settings.Default.ArgumentColorLight = ColUtils.ConvertToDrawingColor(prevArgumentColour);
                Settings.Default.DigitColorLight = ColUtils.ConvertToDrawingColor(prevDigitColour);
                Settings.Default.StringColorLight = ColUtils.ConvertToDrawingColor(prevStringColour);
            }
            else
            {
                Settings.Default.OptionColorDark = ColUtils.ConvertToDrawingColor(prevOptionColour);
                Settings.Default.SubOptionColorDark = ColUtils.ConvertToDrawingColor(prevSubOptionColour);
                Settings.Default.ArgumentColorDark = ColUtils.ConvertToDrawingColor(prevArgumentColour);
                Settings.Default.DigitColorDark = ColUtils.ConvertToDrawingColor(prevDigitColour);
                Settings.Default.StringColorDark = ColUtils.ConvertToDrawingColor(prevStringColour);
            }

            Settings.Default.Font = new System.Drawing.Font(prevFontName, prevFontSize);
        }

        private void OnNumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
            if (uint.TryParse(HistorySize.Text, out newHistorySize))
            {
                History.GetHistory.Resize(newHistorySize);
                Settings.Default.HistorySize = newHistorySize;
            }

            Clear();
            this.Close();
        }

        private void OnCancel_Click(object sender, RoutedEventArgs e)
        {
            ResetOptions();
            Clear();
            this.Close();
        }

        private void OnFontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Drawing.Font oldFont = Settings.Default.Font;
            try
            {
                Settings.Default.Font = new System.Drawing.Font(((FontFamily)FontCombo.SelectedItem).Source, Settings.Default.Font.SizeInPoints);
            }
            catch (System.ArgumentException /*e*/)
            {
                FontCombo.SelectedItem = e.RemovedItems[0];
            }
        }

        private void OnFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.Default.Font = new System.Drawing.Font(Settings.Default.Font.Name, (float)e.NewValue);
        }
    }
}
