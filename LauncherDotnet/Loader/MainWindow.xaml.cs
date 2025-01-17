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
using Loader.Properties;




namespace Loader
{
    public partial class MainWindow : Window
    {


        private static DiscordRpcClient discordClient;
        private static readonly HttpClient client = new HttpClient();
        private string gameFolderPath;
        private string downloadFolderPath;

        private const string SettingsFileName = "settings.json";
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            SelectGameFolderIfNeeded();
            InitializeDiscordRPC();
            FetchServerData();
            CheckForUpdatesAsync();
        }

        private void LoadSettings()
        {
            if (File.Exists("settings.json"))
            {
                var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json"));
                gameFolderPath = settings.GameFolderPath;
                downloadFolderPath = settings.DownloadFolderPath;
            }
            else
            {
                downloadFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameUpdates");
            }
        }

        private void SaveSettings()
        {
            var settings = new Settings
            {
                GameFolderPath = gameFolderPath,
                DownloadFolderPath = downloadFolderPath
            };
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        
        private void SelectGameFolderIfNeeded()
        {
            if (string.IsNullOrEmpty(gameFolderPath) || !Directory.Exists(gameFolderPath))
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select the folder containing the game.";
                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        gameFolderPath = folderDialog.SelectedPath;
                        downloadFolderPath= folderDialog.SelectedPath;
                        SaveSettings();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Game folder selection is required to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        System.Windows.Application.Current.Shutdown();
                    }
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
                System.Windows.MessageBox.Show("Failed To open link.", "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void CheckForUpdatesAsync()
        {
            try
            {
                UpdateStatus2.Text = "Checking for updates...";
                await Updater.CheckAndUpdate(this, gameFolderPath, downloadFolderPath);
            }
            catch (Exception ex)
            {
                UpdateStatus2.Text = $"Update failed: {ex.Message}";
            }
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

        public class Settings
        {
            public string GameFolderPath { get; set; }
            public string DownloadFolderPath { get; set; }
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
