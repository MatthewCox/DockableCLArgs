using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xaml;

namespace MattC.DockableCLArgs
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly"),
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors"),
    Serializable]
    public class NamedArgsDictionary : SortedDictionary<string, string>
    {
        public string Path { get; set; }

        public void Save()
        {
            if (string.IsNullOrEmpty(Path))
                return;

            StringBuilder builder = new StringBuilder();
            using (StringWriter writer = new StringWriter(builder, CultureInfo.InvariantCulture))
            {
                XamlServices.Save(writer, this);
            }
            using (TextWriter writer = new StreamWriter(Path))
            {
                writer.Write(builder.ToString());
            }
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(Path) || !File.Exists(Path))
                return;

            using (TextReader reader = new StreamReader(Path))
            {
                string data = reader.ReadToEnd();
                using (StringReader stringReader = new StringReader(data))
                {
                    NamedArgsDictionary dict = (NamedArgsDictionary)XamlServices.Load(stringReader);

                    this.Clear();
                    foreach (KeyValuePair<string, string> kvp in dict)
                    {
                        this.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }
    }
}
