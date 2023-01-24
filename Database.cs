using Microsoft.Data.Sqlite;

namespace SteamModManager
{
    public static class Database
    {
        private static readonly SqliteConnection connection;
        static Database()
        {
            connection = new("Data Source=items.db;");
            connection.Open();
        }
        public static void Create()
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS items (
                    publishedfileid INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    tags TEXT NOT NULL,
                    time_updated INTEGER
                );
            ";
            command.ExecuteNonQuery();
        }
        public static void Update(SteamWorkshopItem[] items) {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT OR REPLACE INTO items(publishedfileid, title, tags, time_updated)
                VALUES 
            ";
            for (int i = 0; i < items.Length - 1; ++i)
            {
                command.CommandText += items[i].ToSqlValue() + ",";
            }
            command.CommandText += items.Last().ToSqlValue() + ";";
            command.ExecuteNonQuery();
        }
    }
}
