using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Loader
{
    public class Updater
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task CheckAndUpdate(MainWindow mainWindow, string extractFolderPath)
        {
            try
            {
                string localVersion = ReadLocalVersion();
                string versionUrl = "https://raw.githubusercontent.com/smanty222/version.json/refs/heads/main/version.json";
                string response = await client.GetStringAsync(versionUrl);
                var versionInfo = JsonConvert.DeserializeObject<VersionInfo>(response);

                if (localVersion != versionInfo.version)
                {
                    mainWindow.UpdateStatus("Starting Updating");

                    foreach (var file in versionInfo.files)
                    {
                        await DownloadFile(file.url, file.path, mainWindow, extractFolderPath);
                    }

                    UpdateLocalVersion(versionInfo.version);
                    mainWindow.UpdateBar.Value = 100;
                    mainWindow.UpdateStatus("Updated!");
                }
                else
                {
                    mainWindow.UpdateBar.Value = 100;
                    mainWindow.UpdateStatus("Up to date.");
                }
            }
            catch (Exception ex)
            {
                mainWindow.UpdateStatus($"Failed to update : {ex.Message}");
            }
        }

        private static string ReadLocalVersion()
        {
            string versionFilePath = "version.txt";

            if (!File.Exists(versionFilePath))
            {
                File.WriteAllText(versionFilePath, "1.0.0");
            }

            return File.ReadAllText(versionFilePath).Trim();
        }

        private static void ExtractZipFile(string zipPath, string extractPath)
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            ZipFile.ExtractToDirectory(zipPath, extractPath);
            File.Delete(zipPath);
        }

        private static void ExtractRarFile(string rarPath, string extractPath)
        {
            using (var archive = ArchiveFactory.Open(rarPath))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    string filePath = Path.Combine(extractPath, entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    entry.WriteToFile(filePath, new ExtractionOptions() { Overwrite = true });
                }
            }

            File.Delete(rarPath);
        }

        private static void UpdateLocalVersion(string newVersion)
        {
            string versionFilePath = "version.txt";
            File.WriteAllText(versionFilePath, newVersion);
        }

        private static async Task DownloadFile(string fileUrl, string destinationPath, MainWindow mainWindow, string extractFolderPath)
        {
            try
            {
                mainWindow.UpdateStatus($"Downloading: {fileUrl}");

                byte[] data = await client.GetByteArrayAsync(fileUrl);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await fileStream.WriteAsync(data, 0, data.Length);
                }

                string extension = Path.GetExtension(destinationPath).ToLowerInvariant();
                if (extension == ".zip")
                {
                    ExtractZipFile(destinationPath, extractFolderPath);
                }
                else if (extension == ".rar")
                {
                    ExtractRarFile(destinationPath, extractFolderPath);
                }
            }
            catch (Exception ex)
            {
                mainWindow.UpdateStatus($"Error Updating: {ex.Message}");
            }
        }
    }

    public class VersionInfo
    {
        public string version { get; set; }
        public FileInfo[] files { get; set; }
    }

    public class FileInfo
    {
        public string url { get; set; }
        public string path { get; set; }
    }
}
