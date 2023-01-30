using System.IO.Compression;
using System.Text;

namespace SteamModManager
{
    public static class SteamCMD
    {
        private static readonly HttpClient downloader = new();
        private static readonly string urlDownload = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        private static readonly string pathExecutableDefault = "C:/steamcmd/steamcmd.exe";
        private static readonly string pathExcutableZip = ".temp/steamcmd.zip";
        public static bool LoginStatus { get; set; } = false;
        public static string PathExcutable { get; set; } = pathExecutableDefault;
        public static string PathInstallDirectory { get; set; } = string.Empty;
        public static string SteamAppID { get; set; } = string.Empty;
        private static async Task DownloadSteamCMD()
        {
            var responseStream = await downloader.GetStreamAsync(urlDownload);
            Directory.CreateDirectory(pathExcutableZip[..5]);
            var fileStream = new FileStream(pathExcutableZip, FileMode.Create);
            responseStream.CopyTo(fileStream);
            fileStream.Close();
            ZipFile.ExtractToDirectory(pathExcutableZip, PathExcutable[..12]);
            Directory.Delete(pathExcutableZip[..5], true);
        }
        public static void EnsureWindowsExecutableExistence()
        {
            while (!File.Exists(PathExcutable))
            {
                DownloadSteamCMD().Wait();
            }
        }
        public static void Download(SteamWorkshopItem[] items)
        {
            StringBuilder builder = new();
            builder.Append($" +force_install_dir \"{PathInstallDirectory}\"");
            if (!LoginStatus)
            {
                builder.Append(" +login anonymous");
            }
            else
            {
                string? username, password;
                Console.WriteLine("Login is required.");
                Console.Write("Username: ");
                username = Console.ReadLine();
                Console.Write("Password: ");
                password = Console.ReadLine();
                Console.Clear();
                builder.Append($" +login {username} {password}");
            }
            foreach (var item in items)
            {
                builder.Append($" +workshop_download_item {SteamAppID} {item.PublishedFileId}");
            }
            builder.Append(" +quit");
            System.Diagnostics.Process.Start(PathExcutable, builder.ToString()).WaitForExit();
            Console.WriteLine();
            string pathDownload = $"{PathInstallDirectory}/steamapps/workshop/content/{SteamAppID}/";
            foreach (var item in items)
            {
                string pathItem = $"{PathInstallDirectory}/{item.PublishedFileId}";
                if (Directory.Exists(pathItem))
                {
                    Directory.Delete(pathItem, true);
                }
                Directory.Move(pathDownload + item.PublishedFileId, pathItem);
            }
            Directory.Delete($"{PathInstallDirectory}/steamapps", true);
        }
    }
}
