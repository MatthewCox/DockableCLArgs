using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColorPicker.ExtensionMethods;

namespace ColorPicker.ColorModels.CMY 
{
    class Magenta : ColorComponent
    {
        public static CMYModel sModel = new CMYModel();

        public override int MinValue
        {
            get {return 0; }
        }

        public override int MaxValue
        {
            get {return 100; }
        }

        public override double Value(System.Windows.Media.Color color)
        {
            return sModel.MComponent(color);
        }

        public override string Name
        {
            get { return "CMY_magenta"; }
        }
    }
}
