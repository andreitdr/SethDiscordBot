using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordBotUI_Windows.WindowsForms
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();

            Load += (_, _) => MainWindowLoad();
        }

        private void MainWindowLoad()
        {
            pluginListToolStripMenuItem.Click += (_, _) => new PluginListWindow().Show();
        }
    }
}
