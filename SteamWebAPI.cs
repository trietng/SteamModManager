﻿using System.Text.Json;
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
                
                foreach (var tag in tags)
                {
                    results[i].Tags.Add(tag!["tag"]!.GetValue<string>());
                }
                results[i].TimeUpdated = publishedFileDetails["time_updated"]!.GetValue<UInt64>();
            }
            return results;
        }
    }
    namespace SteamWebAPI {
        public class Request
        {
            private static readonly HttpClient httpClient = new();
            private static readonly string urlAPI = "https://api.steampowered.com";
            private UInt32? version = null;
            public Request() { }
            public Request(string webInterface, string method, uint? version, HttpMethod httpMethod)
            {
                WebInterface = webInterface;
                Method = method;
                Version = version;
                HttpMethod = httpMethod;
            }
            public Request(string webInterface, string method, uint? version, HttpMethod httpMethod, Dictionary<string, string> parameters)
            : this(webInterface, method, version, httpMethod)
            {
                Parameters = parameters;
            }
            public string WebInterface { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public UInt32? Version
            {
                get 
                { 
                    return version;
                }
                set
                {
                    if (value < 1)
                    {
                        throw new ArgumentException($"v{value} is not a valid method version", "version");
                    }
                    else
                    {
                        version = value;
                    }
                }
            }
            public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
            public Dictionary<string, string> Parameters { get; set; } = new();
            public async Task<JsonNode> Send()  {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod,
                    RequestUri = new Uri($"{urlAPI}/{WebInterface}/{Method}/v{Version}"),
                    Content = new FormUrlEncodedContent(Parameters),
                };
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string text = await response.Content.ReadAsStringAsync();
                JsonNode document = JsonNode.Parse(text)!;
                return document["response"]!;
            }
        }
        public static class ISteamRemoteStorage
        {
            public static async Task<JsonNode> GetPublishedFileDetails(IEnumerable<UInt64> publishedfileids)
            {
                var request = new Request
                {
                    HttpMethod = HttpMethod.Post,
                    WebInterface = "ISteamRemoteStorage",
                    Method = "GetPublishedFileDetails",
                    Version = 1,
                };
                var itemcount = publishedfileids.Count();
                request.Parameters.Add(nameof(itemcount), itemcount.ToString());
                for (int i = 0; i < itemcount; ++itemcount)
                {
                    request.Parameters.Add($"publishedfileids[{i}]", publishedfileids.ElementAt(i).ToString());
                }
                return await request.Send();
            }
            public static async Task<JsonNode> GetPublishedFileDetails(IEnumerable<string> publishedfileids)
            {
                var request = new Request
                {
                    HttpMethod = HttpMethod.Post,
                    WebInterface = "ISteamRemoteStorage",
                    Method = "GetPublishedFileDetails",
                    Version = 1,
                };
                var itemcount = publishedfileids.Count();
                request.Parameters.Add(nameof(itemcount), itemcount.ToString());
                for (int i = 0; i < itemcount; ++itemcount)
                {
                    request.Parameters.Add($"publishedfileids[{i}]", publishedfileids.ElementAt(i));
                }
                return await request.Send();
            }
            public static async Task<JsonNode> GetPublishedFileDetails(UInt64 publishedfileid)
            {
                var request = new Request
                {
                    HttpMethod = HttpMethod.Post,
                    WebInterface = "ISteamRemoteStorage",
                    Method = "GetPublishedFileDetails",
                    Version = 1,
                };
                request.Parameters.Add("itemcount", "1");
                request.Parameters.Add("publishedfileids[0]", publishedfileid.ToString());
                return await request.Send();
            }
            public static async Task<JsonNode> GetPublishedFileDetails(string publishedfileid)
            {
                var request = new Request
                {
                    HttpMethod = HttpMethod.Post,
                    WebInterface = "ISteamRemoteStorage",
                    Method = "GetPublishedFileDetails",
                    Version = 1,
                };
                request.Parameters.Add("itemcount", "1");
                request.Parameters.Add("publishedfileids[0]", publishedfileid.ToString());
                return await request.Send();
            }
        }
    }
}