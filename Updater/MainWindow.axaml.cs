using Avalonia.Controls;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

using System.Threading.Tasks;

namespace Updater
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Activated += (sender, e) => FormActive();
        }

        public async void FormActive()
        {
            if (Program.Command != "/update")
                return;
            await Task.Delay(3000);
            WebClient c = new WebClient();
            Directory.CreateDirectory("./Updater/Downloads");
            await c.DownloadFileTaskAsync(Program.Link, "./Updater/Downloads/Update.zip");
            await Task.Run(() => ZipFile.ExtractToDirectory("./Updater/Downloads/Update.zip", Program.Location, true));
            Process.Start(Program.AppToOpen);
            File.Delete("./Updater/Downloads/Update.zip");
            Environment.Exit(0);
        }
    }
}
