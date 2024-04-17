using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedDiscordUIControls.Controls.SubItems
{
    public class DropDownItem
    {
        public string Title { get; private set; }
        public string Description { get; private set; }

        public DropDownItem(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
}
