using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.IO;
using System.Xml.Serialization;
using Extext.Ini;

namespace SteamModManager
{
    public static class Control
    {
        private static readonly string pathConfig = "config.ini";
        private static class Configuration
        {
            public static class Path
            {
                public static string sql = "items.db";
                public static string destination = string.Empty;
                public static string steamcmd = "C:/steamcmd/steamcmd.exe";
            }
            public static class Network
            {
                public static UInt32 port = 1234;
                public static bool httpListenerStatus = false;
            }
            public static void Set(IniDocument document)
            {
                Path.sql = document["path"]["sql"].Value;
                Path.destination = document["path"]["destination"].Value;
                Path.steamcmd = document["path"]["steamcmd"].Value;
                Network.port = Convert.ToUInt32(document["network"]["port"].Value);
                Network.httpListenerStatus = Convert.ToBoolean(document["network"]["http_listener_status"].Value);
            }
        }
        

        public static class Script
        {
            public static void Generate()
            {
                
            }
        }
        public static void LoadConfiguration()
        {
            string textInput = "";
            try
            {
                textInput = File.ReadAllText(pathConfig);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"File \"{pathConfig}\" not found. Using default config.");
                return;
            }
            IniDocument document = IniSerializer.Deserialize(textInput);
            try
            {
                var ensureValidity = document["steam_mod_manager"];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"File \"{pathConfig}\" is not valid. Using default config.");
                return;
            }
            Configuration.Set(document);
        }
        public static void Add(string[] steamWorkshopIds)
        {
            Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetPublishedFileDetails(steamWorkshopIds);
            task.Wait();
            var steamWorkshopItems = SteamWorkshopItem.Parse(task.Result);
            
        }
        public static void Remove(string[] steamWorkshopIds) 
        {
            
        }
    }
}
