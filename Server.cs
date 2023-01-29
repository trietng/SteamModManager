using SteamModManager.SteamWebAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SteamModManager
{
    public class Server
    {
        public static int Port { get; } = 27060;
        private static readonly HttpListener listener = new();
        public HashSet<string> ItemsToAdd { get; private set; } = new();
        public HashSet<string> ItemsToRemove { get; private set; } = new();
        public void Start()
        {
            Console.WriteLine("Listening...");
            Console.WriteLine("Press Enter to execute the requests");
            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
        }
        public void Stop()
        {
            listener.Stop();
            Console.WriteLine("Executing requests...");
            Console.WriteLine("Removing items...");
            Control.Remove(ItemsToRemove.ToArray());
            Console.WriteLine("Adding items...");
            Control.Add(ItemsToAdd.ToArray());
        }
        private static JsonNode ReadAsJson(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return JsonNode.Parse(reader.ReadToEnd())!;
            }
        }
        private bool Contains(string itemId)
        {
            if (!ItemsToRemove.Contains(itemId))
            {
                if (ItemsToAdd.Contains(itemId)) return true;
                if (Database.Contains(itemId)) return true;
                return false;
            }
            return true;
        }
        private bool Add(string itemId)
        {
            ItemsToRemove.Remove(itemId);
            if (!Database.Contains(itemId))
            {
                ItemsToAdd.Add(itemId);
                return true;
            }
            return false;
        }
        private bool Remove(string itemId) 
        {
            if (!ItemsToAdd.Remove(itemId))
            {
                ItemsToRemove.Add(itemId);
                return true;
            }
            return false;
        }
        private void Response(HttpListenerRequest request, HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            response.AddHeader("Access-Control-Allow-Origin", "https://steamcommunity.com");
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/plain";
            string responseString = string.Empty;
            if (request.HttpMethod != "OPTIONS")
            {
                var json = ReadAsJson(request);
                if (request.HttpMethod == "POST")
                {
                    var actionType = json["action"]!.GetValue<string>();
                    var itemId = json["id"]!.GetValue<string>();
                    switch (actionType)
                    {
                        case "query":
                            responseString = Contains(itemId).ToString().ToLower();
                            Console.WriteLine($"Item {itemId} existence status: {responseString}");
                            break;
                        case "add":
                            if (Add(itemId))
                            {
                                responseString = $"Item {itemId} added";
                                Console.WriteLine(responseString);
                            }
                            break;
                        case "remove":
                            if (Remove(itemId))
                            {
                                responseString = $"Item {itemId} removed";
                                Console.WriteLine(responseString);
                            }
                            break;
                        default: break;
                    }
                }
            }
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        private void ListenerCallback(IAsyncResult result)
        {
            try
            {
                if (listener.IsListening)
                {
                    var context = listener.EndGetContext(result);
                    var request = context.Request;
                    Response(request, context);
                    listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
                }
            }
            catch (HttpListenerException)
            {
                // intentionally left blank
            }
        }
    }
}
