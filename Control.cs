using System.Text.Json.Nodes;
using Extext.Ini;
using System.IO.Compression;
using Extext.Compression;

namespace SteamModManager
{
    public static class ConsoleFormat
    {
        public static readonly string horizontalBarInfo =
        $"+{new string('-', 10)}+{new string('-', 40)}+{new string('-', 11)}+{new string('-', 3)}+{new string('-', 10)}+";
        public static readonly string horizontalBarUpdate =
        $"+{new string('-', 10)}+{new string('-', 38)}+{new string('-', 16)}+{new string('-', 16)}+";
        public static readonly string headerInfo =
        $"{horizontalBarInfo}\n|{"ID",-10}|{"Title",-40}|{"Type",-11}|{"AV",-3}|{"Updated on",-10}|\n{horizontalBarInfo}";
        public static readonly string headerUpdate =
        $"{horizontalBarUpdate}\n|{"ID",-10}|{"Title",-38}|{"Current", -16}|{"New",-16}|\n{horizontalBarUpdate}";
    }
    public static class Control
    {
        internal class Configuration
        {
            public static readonly string version = "1.0";
            public string Database { get; set; }
            public string InstallDirectory { get; set; }
            public string SteamCMD { get; set; }
            public string SteamAppID { get; set; }
            public bool HttpListenerStatus { get; set; }
            public bool AutoUpdate { get; set; }
            public bool IntegrityCheck { get; set; }
            public bool Login { get; set; }
            public Configuration()
            {
                Database = "database";
                InstallDirectory = string.Empty;
                SteamCMD = "steamcmd/steamcmd.exe";
                HttpListenerStatus = false;
                SteamAppID = string.Empty;
                AutoUpdate = false;
                IntegrityCheck = false;
                Login = false;
            }
            public void Set(IniDocument document)
            {
                Database = document["path"]["sql"].Value;
                InstallDirectory = document["path"]["install_dir"].Value;
                SteamCMD = document["path"]["steamcmd"].Value;
                HttpListenerStatus = Convert.ToBoolean(document["network"]["http_listener_status"].Value);
                SteamAppID = document["game"]["steam_app_id"].Value;
                AutoUpdate = Convert.ToBoolean(document["game"]["auto_update"].Value);
                IntegrityCheck = Convert.ToBoolean(document["game"]["integrity_check"].Value);
                Login = Convert.ToBoolean(document["game"]["login"].Value);
            }
            public void Export(string pathConfig = "config.ini")
            {
                File.WriteAllText(pathConfig, ToString());
            }
            public override string ToString()
            {
                IniDocument document = new()
                {
                    new IniSection("steam_mod_manager")
                    {
                        new IniProperty("config_version", version)
                    },
                    new IniSection("path")
                    {
                        new IniProperty("sql", Database),
                        new IniProperty("install_dir", InstallDirectory),
                        new IniProperty("steamcmd", SteamCMD)
                    },
                    new IniSection("network")
                    {
                        new IniProperty("http_listener_status", HttpListenerStatus.ToString().ToLower())
                    },
                    new IniSection("game")
                    {
                        new IniProperty("steam_app_id", SteamAppID),
                        new IniProperty("auto_update", AutoUpdate.ToString().ToLower()),
                        new IniProperty("integrity_check", IntegrityCheck.ToString().ToLower()),
                        new IniProperty("login", Login.ToString().ToLower())
                    }
                };
                return IniSerializer.Serialize(document);
            }
        }
        public static string PathConfig { get; } = "config.ini";
        private static readonly Configuration configuration = new();
        private static void CreateDefaultConfiguration()
        {
            Console.Write("Enter target Steam App ID: ");
            string? input = Console.ReadLine();
            if (input == null)
            {
                Environment.Exit(0);
            }
            configuration.SteamCMD = input;
            SteamCMD.SteamAppID = input;
            Console.Clear();
            configuration.Export();
        }
        public static void LoadConfiguration(bool reload = false)
        {
            string textInput;
            try
            {
                textInput = File.ReadAllText(PathConfig);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"File \"{PathConfig}\" not found. Using default config.");
                CreateDefaultConfiguration();
                return;
            }
            IniDocument document = IniSerializer.Deserialize(textInput);
            try
            {
                var ensureValidity = document["steam_mod_manager"];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"File \"{PathConfig}\" is not valid. Using default config.");
                CreateDefaultConfiguration();
                return;
            }
            configuration.Set(document);
            SteamCMD.PathExcutable = configuration.SteamCMD;
            if ((configuration.InstallDirectory == string.Empty) ||
                (configuration.InstallDirectory == ".") ||
                new string[] { "./", "\\." }.Any(configuration.InstallDirectory.Contains))
            {
                configuration.InstallDirectory = "./";
                SteamCMD.PathInstallDirectory = Directory.GetCurrentDirectory();
            }
            else if (
                (configuration.InstallDirectory == "..") ||
                new string[] { "../", "\\.." }.Any(configuration.InstallDirectory.Contains))
            {
                configuration.InstallDirectory = "../";
                SteamCMD.PathInstallDirectory = Directory.GetParent(Directory.GetCurrentDirectory())!.ToString();
            }
            else
            {
                SteamCMD.PathInstallDirectory = configuration.InstallDirectory;
            }
            SteamCMD.SteamAppID = configuration.SteamAppID;
            SteamCMD.LoginStatus = configuration.Login;
            if (reload == true)
            {
                Database.Close();
            }
            Database.Create(configuration.Database, configuration.SteamAppID);
        }
        public static void AutoUpdate()
        {
            if (configuration.AutoUpdate)
            {
                Console.WriteLine("Auto-update process started...");
                Update();
            }
        }
        public static void IntegrityCheck(bool forced = false)
        {
            if ((configuration.IntegrityCheck) || (forced))
            {
                Console.WriteLine("Checking file integrity... ");
                var items = Database.Select();
                List<string> list = new();
                Console.Write("Checking if all database entries exist... ");
                foreach (var item in items)
                {
                    string pathItem = $"{configuration.InstallDirectory}/{item}";
                    if (!Directory.Exists(pathItem))
                    {
                        list.Add(item);
                    }
                }
                if (list.Count > 0)
                {
                    Console.WriteLine("FAILED");
                    Console.WriteLine($"{list.Count} items missing.");
                    Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetPublishedFileDetails(list.ToArray());
                    task.Wait();
                    SteamWorkshopItem[] steamWorkshopItems;
                    try
                    {
                        steamWorkshopItems = SteamWorkshopItem.Parse(task.Result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }
                    Console.WriteLine(ConsoleFormat.headerUpdate);
                    foreach (var item in steamWorkshopItems)
                    {
                        string title = item.Title;
                        if (title.Length > 38)
                        {
                            title = title[..38];
                        }
                        string dateNew = DateTimeOffset.FromUnixTimeSeconds((Int64)item.TimeUpdated!).ToString("yyyy/MM/dd HH:mm");
                        Console.WriteLine($"|{item.PublishedFileId,-10}|{title,-38}|{"null",-16}|{dateNew,-16}|");
                    }
                    Console.WriteLine(ConsoleFormat.horizontalBarUpdate);
                    Console.Write("Re-download the missing files? (y/n): ");
                    string? answer = Console.ReadLine();
                    if (answer is null)
                    {
                        Console.WriteLine("Input error");
                        Environment.Exit(0);
                    }
                    answer = answer.Trim();
                    if ((answer == "yes") || (answer == "y"))
                    {
                        SteamCMD.Download(steamWorkshopItems);
                        Database.Replace(steamWorkshopItems);
                    }
                }
                else
                {
                    Console.WriteLine("OK");
                }
                Console.Write("Checking if there is any unrecorded mod... ");
                list.Clear();
                var unrecordedItems = Directory.GetDirectories(configuration.InstallDirectory).Except(items).Select(_ => Path.GetFileName(_)).ToList();
                foreach (var item in unrecordedItems)
                {
                    if (Int64.TryParse(Path.GetFileName(item), out _))
                    {
                        if (File.Exists(Path.Combine(configuration.InstallDirectory, item, "About", "PublishedFileId.txt")))
                        {
                            Console.WriteLine($"Item {item} detected");
                            list.Add(item);
                        }
                    }
                }
                if (list.Count > 0)
                {
                    Console.WriteLine($"{list.Count} items unrecorded.");
                    Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetPublishedFileDetails(list.ToArray());
                    task.Wait();
                    SteamWorkshopItem[] steamWorkshopItems;
                    try
                    {
                        steamWorkshopItems = SteamWorkshopItem.Parse(task.Result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }
                    Database.Insert(steamWorkshopItems);
                }
                else
                {
                    Console.WriteLine("OK");
                }
            }
        }
        public static void Add(string[] steamWorkshopItemIds)
        {
            Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetPublishedFileDetails(steamWorkshopItemIds);
            task.Wait();
            SteamWorkshopItem[] steamWorkshopItems;
            try
            {
                steamWorkshopItems = SteamWorkshopItem.Parse(task.Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            SteamCMD.Download(steamWorkshopItems);
            Database.Insert(steamWorkshopItems);
        }
        public static void Remove(string[] steamWorkshopItemIds) 
        {
            foreach (var item in steamWorkshopItemIds)
            {
                string pathItem = $"{configuration.InstallDirectory}/{item}";
                if (Directory.Exists(pathItem))
                {
                    Directory.Delete(pathItem, true);
                    Console.WriteLine($"Item {item} removed");
                }
            }
            var deletedRows = Database.Delete(steamWorkshopItemIds);
            Console.WriteLine($"Number of input ids: {steamWorkshopItemIds.Length}");
            Console.WriteLine($"Number of removed ids: {deletedRows}"); 
            if (steamWorkshopItemIds.Length > deletedRows)
            {
                Console.WriteLine("There are some ids that doesn't exist in the database.");
            }
        }
        public static void Help()
        {
            Console.WriteLine(
                "help\t\tprint help" +
                "\nadd [item]\tadd and download item(s)" +
                "\nremove [item]\tremove item(s)" +
                "\nclear\t\tclear screen" +
                "\nupdate\t\tupdate all items" +
                "\nupdate [item]\tupdate specific item(s)" +
                "\ninfo\t\tlist all available items in memory" +
                "\ninfo [item]\tsearch specific items" +
                "\nbackup\t\tcreate zip archive of all available items" +
                "\nlisten\t\topen HTTP listener on port 27060" +
                "\nconfig\t\tshow configuration" +
                "\nreconfig\treload configuration" +
                "\nlist\t\talias of info" +
                "\ndelete [item]\talias of remove [item]" +
                "\nsearch [item]\talias of info [item]" +
                "\nfind [item]\talias of info [item]" +
                "\nquit\t\tquit program"
            );
        }
        public static void Backup()
        {
            var list = Database.Select();
            string now = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            using (FileStream fileStream = File.Open($"{now}.zip", FileMode.Create))
            {
                using (ZipArchive zipArchive = new(fileStream, ZipArchiveMode.Create))
                {
                    Console.WriteLine("Compressing...");
                    foreach (var item in list)
                    {
                        zipArchive.CreateEntryFromDirectory(item, item, CompressionLevel.Optimal);
                        Console.WriteLine($"Item {item} added to archive.");
                    }
                    Database.Backup(zipArchive);
                    Console.WriteLine($"Database \"{configuration.Database}\" added to archive");
                    zipArchive.CreateEntryFromFile(PathConfig, PathConfig);
                    Console.WriteLine("Config file \"config.ini\" added to archive");
                }
            }
        }
        public static void Clear()
        {
            Console.Clear();
        }
        public static void Info()
        {
            var list = Database.SelectForInfo();
            foreach (var item in list) 
            { 
                Console.WriteLine(item);
            }
        }
        public static void Info(string[] itemIDs)
        {
            var list = Database.SelectForInfo(itemIDs);
            foreach (var item in list)
            {
                Console.WriteLine(item);
            }
        }
        public static void Update(bool forced = false)
        {
            var list = Database.SelectForUpdate();
            Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetPublishedFileDetails(list.Select(_ => _.Item1).ToArray());
            task.Wait();
            SteamWorkshopItem[] steamWorkshopItems;
            try
            {
                steamWorkshopItems = SteamWorkshopItem.Parse(task.Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            var listUpdate = new List<Tuple<SteamWorkshopItem, Int64>>();
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Item2 < steamWorkshopItems[i].TimeUpdated)
                {
                    listUpdate.Add(new (steamWorkshopItems[i], list[i].Item2));
                }
            }
            if (listUpdate.Count == 0)
            {
                Console.WriteLine("No update found.");
            }
            else
            {
                Console.WriteLine($"Found {listUpdate.Count} updates.");
                Console.WriteLine(ConsoleFormat.headerUpdate);
                foreach (var item in listUpdate)
                {
                    string title = item.Item1.Title;
                    if (title.Length > 38)
                    {
                        title = title[..38];
                    }
                    string dateCurrent = DateTimeOffset.FromUnixTimeSeconds(item.Item2).ToString("yyyy/MM/dd HH:mm");
                    string dateNew = DateTimeOffset.FromUnixTimeSeconds((Int64)item.Item1.TimeUpdated!).ToString("yyyy/MM/dd HH:mm");
                    Console.WriteLine($"|{item.Item1.PublishedFileId,-10}|{title,-38}|{dateCurrent, -16}|{dateNew,-16}|");
                }
                Console.WriteLine(ConsoleFormat.horizontalBarUpdate);
                if (!forced)
                {
                    Console.Write("Proceed with the update? (y/n): ");
                    string? answer = Console.ReadLine()!.Trim();
                    if ((answer == "yes") || (answer == "y"))
                    {
                        goto CommitUpdate;
                    }
                    else
                    {
                        return;
                    }
                }
                CommitUpdate:
                SteamCMD.Download(steamWorkshopItems);
                Database.Replace(steamWorkshopItems);
            }
        }
        public static void Update(string[] itemIDs, bool forced = false)
        {
            var list = Database.SelectForUpdate(itemIDs);
            Task<JsonNode>? task = SteamWebAPI.ISteamRemoteStorage.GetPublishedFileDetails(list.Select(_ => _.Item1).ToArray());
            task.Wait();
            SteamWorkshopItem[] steamWorkshopItems;
            try
            {
                steamWorkshopItems = SteamWorkshopItem.Parse(task.Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            var listUpdate = new List<Tuple<SteamWorkshopItem, Int64>>();
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Item2 < steamWorkshopItems[i].TimeUpdated)
                {
                    listUpdate.Add(new(steamWorkshopItems[i], list[i].Item2));
                }
            }
            if (listUpdate.Count == 0)
            {
                Console.WriteLine("No update found.");
            }
            else
            {
                Console.WriteLine($"Found {listUpdate.Count} updates.");
                Console.WriteLine(ConsoleFormat.headerUpdate);
                foreach (var item in listUpdate)
                {
                    string title = item.Item1.Title;
                    if (title.Length > 38)
                    {
                        title = title[..38];
                    }
                    string dateCurrent = DateTimeOffset.FromUnixTimeSeconds(item.Item2).ToString("yyyy/MM/dd HH:mm");
                    string dateNew = DateTimeOffset.FromUnixTimeSeconds((Int64)item.Item1.TimeUpdated!).ToString("yyyy/MM/dd HH:mm");
                    Console.WriteLine($"|{item.Item1.PublishedFileId,-10}|{title,-38}|{dateCurrent,-16}|{dateNew,-16}|");
                }
                Console.WriteLine(ConsoleFormat.horizontalBarUpdate);
                if (!forced)
                {
                    Console.Write("Proceed with the update? (y/n): ");
                    string? answer = Console.ReadLine()!.Trim();
                    if ((answer == "yes") || (answer == "y"))
                    {
                        goto CommitUpdate;
                    }
                    else
                    {
                        return;
                    }
                }
                CommitUpdate:
                SteamCMD.Download(steamWorkshopItems);
                Database.Replace(steamWorkshopItems);
            }
        }
        public static void Listen(bool forced = false)
        {
            if ((configuration.HttpListenerStatus) || (forced))
            {
                bool escapeKeyPressed = false;
                var server = new Server();
                server.Start();
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Enter)
                        {
                            break;
                        }
                        else if (key == ConsoleKey.Escape)
                        {
                            escapeKeyPressed = true;
                            break;
                        }
                    }
                }
                server.Stop(escapeKeyPressed);
            }
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static void ShowConfig()
        {
            Console.WriteLine(configuration.ToString());
        }
    }
}
