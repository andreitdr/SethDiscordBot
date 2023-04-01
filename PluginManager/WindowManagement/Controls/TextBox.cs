using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager.WindowManagement.Controls
{
    public class TextBox
    {
        public Label Label { get; set; }
        public string Text { get; private set; }
        public Func<string, bool> IsValid { get; set; }

        public bool SetText(string text)
        {
            if(IsValid(text))
            {
                Text = text;
                return true;
            }

            return false;

        }
    }
}