using DiscordBotCore.Online;

namespace DiscordBotUI_Windows.WindowsForms
{
    public partial class PluginListWindow : Form
    {
        public PluginListWindow()
        {
            InitializeComponent();

            Load += async (_,_) => await PluginListWindowLoad();
        }

        private async Task PluginListWindowLoad()
        {
            var listOfPlugins = await DiscordBotCore.Application.CurrentApplication.PluginManager.GetPluginsList();
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Name", "Name");
            dataGridView1.Columns.Add("Description", "Description");
            dataGridView1.Columns.Add("Dependencies", "Has Dependencies");
            dataGridView1.Columns.Add("Version", "Version");
            dataGridView1.Columns.Add("IsInstalled", "IsInstalled");
            dataGridView1.Columns.Add("Install", "Install");

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            foreach(var plugin in listOfPlugins)
            {
                bool isInstalled = await DiscordBotCore.Application.CurrentApplication.PluginManager.IsPluginInstalled(plugin.Name);
                string isInstalledMessage = isInstalled ? "Installed" : "Not Installed";
                int rowIndex = dataGridView1.Rows.Add(plugin.Name, plugin.Description, plugin.HasDependencies, plugin.Version, isInstalledMessage);
                dataGridView1.Rows[rowIndex].Cells["Install"] = (new DataGridViewButtonCell()
                {
                    Value = isInstalled ? "Installed" : "Install",
                    Style = { BackColor = isInstalled ? System.Drawing.Color.LightGray : default }
                });
                if(isInstalled)
                {
                    dataGridView1.Rows[rowIndex].Cells["Install"].Style.BackColor = System.Drawing.Color.LightGray;
                    
                }
            }

            dataGridView1.Refresh();
            dataGridView1.Update();
            dataGridView1.ReadOnly = true;
        }
    }
}
