using System;
using EnvDTE;
using EnvDTE80;

namespace MattC.DockableCLArgs
{
    static class ColUtils
    {
        public static FontsAndColorsItems GetTextEditorFontAndColorsItems(DTE2 dte)
        {
            EnvDTE.Properties props = dte.get_Properties("FontsAndColors", "TextEditor");
            return props.Item("FontsAndColorsItems").Object as FontsAndColorsItems;
        }

        public static System.Windows.Media.Color GetBackgroundColourOf(FontsAndColorsItems faci, string item)
        {
            Int32 oleColor = System.Convert.ToInt32(faci.Item(item).Background);
            System.Drawing.Color sdColor = System.Drawing.ColorTranslator.FromOle(oleColor);
            System.Windows.Media.Color backColor = System.Windows.Media.Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
            return backColor;
        }

        public static System.Windows.Media.Color GetForegroundColourOf(FontsAndColorsItems faci, string item)
        {
            Int32 oleColor = System.Convert.ToInt32(faci.Item(item).Foreground);
            System.Drawing.Color sdColor = System.Drawing.ColorTranslator.FromOle(oleColor);
            System.Windows.Media.Color foreColor = System.Windows.Media.Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
            return foreColor;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool GetBoldednessOf(FontsAndColorsItems faci, string item)
        {
            return faci.Item(item).Bold;
        }

        public static System.Windows.Media.Color ConvertToMediaColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color ConvertToDrawingColor(System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "plainText")]
        public static bool IsLightTheme(System.Windows.Media.Color plainTextBackgroundColour)
        {
            System.Windows.Media.Color ptbc = plainTextBackgroundColour;
            return ptbc.R + ptbc.G + ptbc.B > (128 * 3);
        }
    }
}
