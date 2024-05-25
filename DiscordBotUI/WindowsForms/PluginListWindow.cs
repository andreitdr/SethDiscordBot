using DiscordBotCore.Loaders;
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
            labelInstalling.Visible = false;

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
                dataGridView1.Rows[rowIndex].Cells["Install"] = new DataGridViewButtonCell()
                {
                    Value = isInstalled ? "Remove" : "Install",
                    Style = { BackColor = isInstalled ? System.Drawing.Color.LightGray : default }
                };
            }

            dataGridView1.ReadOnly = true;

            dataGridView1.CellContentClick += async (sender, e) =>
            {
                var senderGrid = (DataGridView)sender;

                if (e.ColumnIndex == 5 && e.RowIndex >= 0)
                {
                    var pluginName = (string)senderGrid.Rows[e.RowIndex].Cells["Name"].Value;
                    var isInstalled = (string)senderGrid.Rows[e.RowIndex].Cells["Install"].Value == "Remove";

                    
                    if (isInstalled)
                    {
                        await DiscordBotCore.Application.CurrentApplication.PluginManager.MarkPluginToUninstall(pluginName);
                        dataGridView1.Rows[e.RowIndex].Cells["Install"] = new DataGridViewButtonCell()
                        {
                            Value = "Install",
                            Style = { BackColor = default }
                        };
                    }
                    else
                    {
                        labelInstalling.Visible = true;
                        labelInstalling.Text = "Installing " + pluginName;

                        var result = await DiscordBotCore.Application.CurrentApplication.PluginManager.GetPluginDataByName(pluginName);

                        await DiscordBotCore.Application.CurrentApplication.PluginManager.InstallPlugin(result!, null);

                        labelInstalling.Visible = false;

                        dataGridView1.Rows[e.RowIndex].Cells["Install"] = new DataGridViewButtonCell()
                        {
                            Value = "Remove",
                            Style = { BackColor = System.Drawing.Color.LightGray }
                        };
                    }
                }

                dataGridView1.Refresh();
                dataGridView1.Update();
            };
        }
    }
}
