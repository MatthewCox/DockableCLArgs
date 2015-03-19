using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using System.Text.RegularExpressions;

namespace ColorPicker
{
    /// <summary>
    /// Interaction logic for HexDisplay.xaml
    /// </summary>
    public partial class HexDisplay : UserControl
    {

        public enum EAlphaByteVisibility
        {
            visible,
            hidden, 
            auto //show if Alpha byte not ff
        }
        public static Type ClassType
        {
            get { return typeof(HexDisplay); }
        }
        public event EventHandler<EventArgs<Color>> ColorChanged;

        #region Color

        public static DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), ClassType,
             new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

        public static bool InternalSet = false;
        
        [ Category("ColorPicker")]
        public Color Color
        {
            get
            {
                return (Color)GetValue(ColorProperty);
            }
            set
            {
                SetValue(ColorProperty, value);
            }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ( InternalSet )
            {
               return;
            }
            
            var c = (Color)e.NewValue;
            var rd = (HexDisplay)d;
            rd.OnColorChanged(c);
        }

        private void OnColorChanged(Color c)
        {
            string colorText = "";

            if (IsNumberSignIncludedInText)
            {
                colorText="#";
            }
           switch (AlphaByteVisibility )
           {
               case EAlphaByteVisibility.visible:
                    colorText += c.ToString().Substring(1);
                   break;
               case EAlphaByteVisibility.hidden :
                    colorText += c.ToString().Substring(3);
                   break;
               case EAlphaByteVisibility.auto :
                   break;
           }


            txtHex.Text = colorText;
            if (ColorChanged != null)
            {
                ColorChanged(this, new EventArgs<Color>(c));
            }
        }

        #endregion

        #region Text

        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), ClassType, new PropertyMetadata("", new PropertyChangedCallback(OnTextChanged)));

        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var display = (HexDisplay) d;
            var oldtext = (string) e.OldValue;
            var newText = (String) e.NewValue;

        }

        #endregion

        #region IsNumberSignIncludedInText

        public static DependencyProperty IsNumberSignIncludedInTextProperty = DependencyProperty.Register("IsNumberSignIncludedInText", typeof(bool), ClassType, 
            new PropertyMetadata(false, OnIsNumberSignIncludedInTextChanged));

         [Category("ColorPicker")]
        public bool IsNumberSignIncludedInText
        {
            get
            {
                return (bool)GetValue(IsNumberSignIncludedInTextProperty);
            }
            set
            {
                SetValue(IsNumberSignIncludedInTextProperty, value);
            }
        }

        private static void OnIsNumberSignIncludedInTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        #endregion

        #region AlphaByteVisibility

        public static DependencyProperty AlphaByteVisibilityProperty = DependencyProperty.Register("AlphaByteVisibility", typeof(EAlphaByteVisibility), ClassType,
            new PropertyMetadata(EAlphaByteVisibility.hidden, OnAlphaByteVisibilityChanged));
         [Category("ColorPicker")]
        public EAlphaByteVisibility AlphaByteVisibility
        {
            get
            {
                return (EAlphaByteVisibility)GetValue(AlphaByteVisibilityProperty);
            }
            set
            {
                SetValue(AlphaByteVisibilityProperty, value);
            }
        }

        private static void OnAlphaByteVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        #endregion

         private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if ( ! IsValidHexColor( txtHex.Text ) )
                return;
 
            try
            {
                // if string can be converted into appropriate color and there is no exception, accept the Color
                Color newColor = (Color)ColorConverter.ConvertFromString("#00" + txtHex.Text);
                
                InternalSet = true;
                Color = newColor;
                InternalSet = false;
            } catch( NotSupportedException ex )
            {
                // In case of exception do nothing
            }
        }
 
        // Filter invalid colors as much as possible
        private bool IsValidHexColor( string hexText )
        {
            if ( hexText.Length != 6 )
                return false;
         
            // Now, check to see if the format is right
            Regex rgx = new Regex(@"[a-fA-F0-9]+");
 
            for ( int i = 0; i < hexText.Length; ++i )
            {
                if ( ! rgx.IsMatch( hexText, i ) )
                    return false;
            }
 
            return true;
        }

        public HexDisplay()
        {
            InitializeComponent();
        }
    }
}
