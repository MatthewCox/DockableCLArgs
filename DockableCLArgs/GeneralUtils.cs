using System.Windows.Input;

namespace MattC.DockableCLArgs
{
    class GeneralUtils
    {
        private GeneralUtils() {}

        public static void DisablePasteEventHandler(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
