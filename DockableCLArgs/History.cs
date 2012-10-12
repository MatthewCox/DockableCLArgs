using System;
using System.IO;
using MattC.DockableCLArgs.Properties;

namespace MattC.DockableCLArgs
{
    class History
    {
        private History() {}

        private static AutoSavingFixedSizeQueue<string> history;
        public static AutoSavingFixedSizeQueue<string> GetHistory
        {
            get { return history; }
        }

        public static void Init()
        {
            history = new AutoSavingFixedSizeQueue<string>(Settings.Default.HistorySize);

            history.Path = Path.Combine(IDEUtils.GetStartupProjectDirectory(), "DockableCLArgsHistory.user");
            history.Load();
        }

        public static void AddToHistory(string value)
        {
            if (!String.IsNullOrEmpty(value.Trim()) && !history.Contains(value))
            {
                history.Enqueue(value);
            }
        }
    }
}
