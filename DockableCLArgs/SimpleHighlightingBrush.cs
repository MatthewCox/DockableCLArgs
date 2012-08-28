using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace MattC.DockableCLArgs
{
    /// <summary>
    /// Highlighting brush implementation that takes a frozen brush.
    /// </summary>
    [Serializable]
    sealed class SimpleHighlightingBrush : HighlightingBrush, ISerializable
    {
        readonly SolidColorBrush brush;

        public SimpleHighlightingBrush(SolidColorBrush brush)
        {
            brush.Freeze();
            this.brush = brush;
        }

        public SimpleHighlightingBrush(Color color) : this(new SolidColorBrush(color)) { }

        public override Brush GetBrush(ITextRunConstructionContext context)
        {
            return brush;
        }

        public override string ToString()
        {
            return brush.ToString();
        }

        SimpleHighlightingBrush(SerializationInfo info, StreamingContext context)
        {
            this.brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(info.GetString("color")));
            brush.Freeze();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info", "Failed to add brush colour to serialization info store; 'info' is null");

            info.AddValue("color", brush.Color.ToString(CultureInfo.InvariantCulture));
        }
    }
}
