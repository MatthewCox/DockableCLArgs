using System.IO;
using System.Xml.Serialization;

namespace MattC.DockableCLArgs
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class AutoSavingFixedSizeQueue<T> : FixedSizeQueue<T>
    {
        public string Path { get; set; }

        public AutoSavingFixedSizeQueue(uint size) : base(size) { Path = null; }

        private bool overrideSave = false;

        public override void Enqueue(T obj)
        {
            base.Enqueue(obj);

            if (!overrideSave)
                Save();
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(Path))
                return;

            T[] queue = this.ToArray();
            XmlSerializer serializer = new XmlSerializer(typeof(T[]));
            using (TextWriter writer = new StreamWriter(Path))
            {
                serializer.Serialize(writer, queue);
            }
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(Path) || !File.Exists(Path))
                return;

            XmlSerializer deserializer = new XmlSerializer(typeof(T[]));
            T[] queueArray;
            using (TextReader reader = new StreamReader(Path))
            {
                queueArray = (T[])deserializer.Deserialize(reader);
            }
            overrideSave = true;
            foreach (T item in queueArray)
            {
                Enqueue(item);
            }
            overrideSave = false;
        }
    }
}
