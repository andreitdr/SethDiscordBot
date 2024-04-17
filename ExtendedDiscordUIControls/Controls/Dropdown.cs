using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExtendedDiscordUIControls.Controls.SubItems;

namespace ExtendedDiscordUIControls.Controls
{
    public class Dropdown
    {
        public List<DropDownItem> Items { get; private set; }

        public Dropdown(List<DropDownItem> items) {  
            Items = items; 
        }



    }
}
