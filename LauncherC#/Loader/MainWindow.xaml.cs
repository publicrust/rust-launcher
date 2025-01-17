using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using DiscordRPC;
using DiscordRPC.Logging;
using Newtonsoft.Json;
using Loader;
using System.Windows.Input;
using System.Windows.Forms;
using Button = DiscordRPC.Button;




namespace Loader
{
    public partial class MainWindow : Window
    {


        private static DiscordRpcClient discordClient;
        private static readonly HttpClient client = new HttpClient();
        private string gameFolderPath;

        public MainWindow()
        {
            InitializeComponent();
            SelectGameFolder();
            InitializeDiscordRPC();
            FetchServerData();
            CheckForUpdatesAsync();
        }

        private void SelectGameFolder()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the folder containing the game.";
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    gameFolderPath = folderDialog.SelectedPath;
                }
                else
                {
                    System.Windows.MessageBox.Show("Game folder selection is required to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }



        private void DragWin(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWebsite("https://discord.gg/example");
        }

        private void VKButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWebsite("https://vk.com/example");
        }

        private void ShopButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWebsite("example");
        }

        private void OpenWebsite(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                System.Windows.MessageBox.Show("Failed To download update.", "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void CheckForUpdatesAsync()
        {
            UpdateStatus2.Text = "Checking For Updates";
            await Updater.CheckAndUpdate(this, gameFolderPath);
        }


        public void UpdateProgress(int progress)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateBar.Value = progress;
            });
        }


        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus2.Text = message;
            });
        }

        public async void FetchServerData()
        {
            try
            {
                //using gamemonitoring api for online
                string apiUrl = "https://api.gamemonitoring.ru/servers/serverid";
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();


                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                var server = apiResponse.response;
                //modified discord api for online players in RPC
                UpdateDiscordPresence("In Game", $"Online : {server.numplayers}/{server.maxplayers}");
                PlayersText.Text = $"Online : {server.numplayers}/{server.maxplayers}";

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error fetching server data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class ApiResponse
        {
            public ServerInfo response { get; set; }
        }

        public class ServerInfo
        {
            public string name { get; set; }

            public int numplayers { get; set; }
            public int maxplayers { get; set; }
        }



        private static void InitializeDiscordRPC()
        {

            discordClient = new DiscordRpcClient("ClientID");
            discordClient.Logger = new ConsoleLogger() { Level = LogLevel.Warning };


            discordClient.Initialize();
            UpdateDiscordPresence("Loading Launcher", "Starting Rust");
        }

        private static void UpdateDiscordPresence(string state, string details)
        {
            discordClient.SetPresence(new RichPresence()
            {
                Details = details,
                State = state,



                Assets = new Assets()
                {
                    LargeImageKey = "png",
                    LargeImageText = "text",
                    SmallImageKey = "png",
                    SmallImageText = "text"

                }
            });
            discordClient.UpdateButtons(new Button[]
           {
                new Button { Label = "Vk", Url = "https://vk.com/example" },
                new Button { Label = "Discord", Url = "https://discord.gg/example" }
           });

        }

        private static void DisposeDiscordRPC()
        {
            if (discordClient != null)
            {
                discordClient.ClearPresence();
                discordClient.Dispose();
            }
        }

        public async void PlayGameButton_Click(object sender, RoutedEventArgs e)
        {


            try
            {
                string exePath = Path.Combine(gameFolderPath, "RustClient.exe");


                Process gameProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    }
                };
                gameProcess.Start();

                this.Hide();


                await Task.Run(() => gameProcess.WaitForExit());


                this.Show();


            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error launching game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }





        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            DisposeDiscordRPC();
        }
    }
}
