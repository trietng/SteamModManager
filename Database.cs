using Microsoft.Data.Sqlite;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SteamModManager
{
    public static class Database
    {
        private static readonly SqliteConnection connection = new();
        public static string SteamAppID { get; private set; } = string.Empty;
        public static void Close()
        {
            connection.Close();
        }
        public static void Create(string dataSource, string steamAppID)
        {
            SteamAppID = steamAppID;
            connection.ConnectionString = $"Data Source={dataSource};";
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            $@"
                CREATE TABLE IF NOT EXISTS steam_app_{SteamAppID} (
                    publishedfileid INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    type TEXT NOT NULL,
                    app_version TEXT NOT NULL,
                    time_updated INTEGER
                );
            ";
            command.ExecuteNonQuery();
        }
        public static void Insert(SteamWorkshopItem[] items) {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            $@"
                INSERT OR IGNORE INTO steam_app_{SteamAppID} (publishedfileid, title, type, app_version, time_updated)
                VALUES 
            ";
            for (int i = 0; i < items.Length - 1; ++i)
            {
                command.CommandText += (items[i].ToSqlValue() + ",");
            }
            command.CommandText += (items.Last().ToSqlValue() + ";");
            command.ExecuteNonQuery();
        }
        public static List<string> Select()
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT publishedfileid FROM steam_app_{SteamAppID};";
            List<string> list = new();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(reader.GetString(0));
                }
            }
            return list;
        }
        public static List<string> SelectForInfo()
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM steam_app_{SteamAppID};";
            List<string> list = new()
            {
                ConsoleFormat.headerInfo
            };
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string name = reader.GetString(1);
                    string date = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(4)).ToString("yyyy/MM/dd");
                    if (name.Length > 40)
                    {
                        name = name[..40];
                    }
                    list.Add(
                        $"|{reader.GetInt64(0),-10}|{name, -40}|{reader.GetString(2), -11}|{reader.GetString(3), -3}|{date,-10}|"
                    );
                }
            }
            list.Add(ConsoleFormat.horizontalBarInfo);
            return list;
        }
        public static List<string> SelectForInfo(string[] itemIDs)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            $@"
                SELECT * FROM steam_app_{SteamAppID}
                WHERE publishedfileid in ( 
            ";
            for (int i = 0; i < itemIDs.Length - 1; ++i)
            {
                command.CommandText += (itemIDs[i] + ',');
            }
            command.CommandText += (itemIDs.Last() + ");");
            List<string> list = new()
            {
                ConsoleFormat.headerInfo
            };
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string name = reader.GetString(1);
                    if (name.Length > 40)
                    {
                        name = name[..40];
                    }
                    list.Add(
                        $"|{reader.GetString(0),-10}|{name,-40}|{reader.GetString(2),-11}|{reader.GetString(3),-3}|{reader.GetString(4),-10}|"
                    );
                }
            }
            list.Add(ConsoleFormat.horizontalBarInfo);
            return list;
        }
        public static int Delete(string[] itemIDs)
        {
            if (itemIDs.Length == 0)
            {
                return 0;
            }
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            $@"
                DELETE FROM steam_app_{SteamAppID}
                WHERE publishedfileid in ( 
            ";
            for (int i = 0; i < itemIDs.Length - 1; ++i)
            {
                command.CommandText += (itemIDs[i] + ',');
            }
            command.CommandText += (itemIDs.Last() + ");");
            return command.ExecuteNonQuery();
        }
        public static List<Tuple<Int64, Int64>> SelectForUpdate()
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT publishedfileid, time_updated FROM steam_app_{SteamAppID};";
            List<Tuple<Int64, Int64>> list = new();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(
                        new (Convert.ToInt64(reader.GetString(0)), Convert.ToInt64(reader.GetString(1)))
                    );
                }
            }
            return list;
        }
        public static List<Tuple<Int64, Int64>> SelectForUpdate(string[] itemIDs)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            @$"
                SELECT publishedfileid, time_updated
                FROM steam_app_{SteamAppID}
                WHERE publishedfileid in (
            ";
            for (int i = 0; i < itemIDs.Length - 1; ++i)
            {
                command.CommandText += (itemIDs[i] + ',');
            }
            command.CommandText += (itemIDs.Last() + ");");
            List<Tuple<Int64, Int64>> list = new();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(
                        new(Convert.ToInt64(reader.GetString(0)), Convert.ToInt64(reader.GetString(1)))
                    );
                }
            }
            return list;
        }
        public static void Replace(List<Tuple<SteamWorkshopItem, Int64>> items)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            $@"
                REPLACE INTO steam_app_{SteamAppID} (publishedfileid, title, type, app_version, time_updated)
                VALUES 
            ";
            for (int i = 0; i < items.Count - 1; ++i)
            {
                command.CommandText += (items[i].Item1.ToSqlValue() + ",");
            }
            command.CommandText += (items.Last().Item1.ToSqlValue() + ";");
            command.ExecuteNonQuery();
        }
        public static void Replace(SteamWorkshopItem[] items)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            $@"
                REPLACE INTO steam_app_{SteamAppID} (publishedfileid, title, type, app_version, time_updated)
                VALUES 
            ";
            for (int i = 0; i < items.Length - 1; ++i)
            {
                command.CommandText += (items[i].ToSqlValue() + ",");
            }
            command.CommandText += (items.Last().ToSqlValue() + ";");
            command.ExecuteNonQuery();
        }

        public static bool Contains(string item)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT publishedfileid FROM steam_app_{SteamAppID} WHERE publishedfileid = {item};";
            bool result = false;
            using (var reader = command.ExecuteReader())
            {
                while (!result && reader.Read())
                {
                    result = true;
                }
            }
            return result;
        }
    }
}
