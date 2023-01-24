using System.Text.Json;
using System.Text.Json.Nodes;

namespace SteamModManager
{
    public class SteamWorkshopItem
    {
        public UInt64? PublishedFileId { get; set; } = null;
        public string Title { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public UInt64? TimeUpdated { get; set; } = null;
        public SteamWorkshopItem() { }
        public string ToSqlValue()
        {
            return $"({PublishedFileId}, '{Title}', '{JsonSerializer.Serialize(Tags)}', {TimeUpdated})";
        }
    }
    public static class Script
    {
        public static SteamWorkshopItem[] Parse(JsonNode response)
        {
            UInt32 resultCount = response["resultcount"]!.GetValue<UInt32>();
            var results = new SteamWorkshopItem[resultCount];
            for (Int32 i = 0; i < resultCount; i++)
            {
                JsonNode publishedFileDetails = response["publishedfiledetails"]![i]!;
                results[i] = new();
                results[i].PublishedFileId = Convert.ToUInt64(publishedFileDetails["publishedfileid"]!.GetValue<string>());
                results[i].Title = publishedFileDetails["title"]!.GetValue<string>();
                var tags = publishedFileDetails["tags"]!.AsArray();
                foreach (var tag in tags) {
                    results[i].Tags.Add(tag!["tag"]!.GetValue<string>());
                }
                results[i].TimeUpdated = publishedFileDetails["time_updated"]!.GetValue<UInt64>();
            }
            return results;
        }
    }
}
