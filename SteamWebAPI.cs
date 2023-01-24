using System.Text.Json.Nodes;

namespace SteamModManager
{
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
            public static async Task<JsonNode> GetPublishedFileDetails(UInt32 itemcount, IEnumerable<UInt64> publishedfileids)
            {
                var request = new Request
                {
                    HttpMethod = HttpMethod.Post,
                    WebInterface = "ISteamRemoteStorage",
                    Method = "GetPublishedFileDetails",
                    Version = 1,
                };
                request.Parameters.Add(nameof(itemcount), itemcount.ToString());
                for (int i = 0; i < itemcount; ++itemcount)
                {
                    request.Parameters.Add($"publishedfileids[{i}]", publishedfileids.ElementAt(i).ToString());
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
        }
    }
}