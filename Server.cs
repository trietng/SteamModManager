using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

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
            Console.WriteLine($"Listening on port {Port}...");
            Console.WriteLine("Press Esc to quit the listening process.");
            Console.WriteLine("Press Enter to execute the requests.");
            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
        }
        public void Stop(bool cancel = false)
        {
            listener.Stop();
            if (!cancel)
            {
                var totalRequests = ItemsToAdd.Count + ItemsToRemove.Count;
                if (totalRequests == 0)
                {
                    return;
                }
                Console.WriteLine("Executing requests...");
                if (ItemsToRemove.Count > 0)
                {
                    Console.WriteLine("Removing items...");
                    Control.Remove(ItemsToRemove.ToArray());
                }
                else
                {
                    Console.WriteLine("No remove request found.");
                }
                if (ItemsToAdd.Count > 0)
                {
                    Console.WriteLine("Adding items...");
                    Control.Add(ItemsToAdd.ToArray());
                }
                else
                {
                    Console.WriteLine("No add request found.");
                }
            }
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
            // Remove item in remove list, don't care if exists
            ItemsToRemove.Remove(itemId);
            // If database doesn't contain item
            if (!Database.Contains(itemId))
            {
                // Add item to add list
                ItemsToAdd.Add(itemId);
                return true;
            }
            // If database contains item already
            return false;
        }
        private void Add(SteamWorkshopCollection collection)
        {
            foreach (var item in collection)
            {
                ItemsToRemove.Remove(item);
            }
            var list = collection.Except(Database.Contains(collection)).ToList();
            foreach (var item in list)
            {
                ItemsToAdd.Add(item);
            }
        }
        private bool Remove(string itemId) 
        {
            // If item is not in add list
            if (!ItemsToAdd.Remove(itemId))
            {
                // If item is not in remove list
                if (ItemsToRemove.Add(itemId))
                {
                    return true;
                }
                // If item is already in remove list
                return false;
            }
            // If item is in add list
            return true;
        }
        private void Remove(SteamWorkshopCollection collection)
        {
            foreach (var item in collection)
            {
                if (!ItemsToAdd.Remove(item))
                {
                    ItemsToRemove.Add(item);
                }
            }
        }
        private void Response(HttpListenerRequest request, HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            response.AddHeader("Access-Control-Allow-Origin", "https://steamcommunity.com");
            response.AppendHeader("Access-Control-Allow-Headers", "Content-Type");
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
                    var itemTitle = json["title"]!.GetValue<string>();
                    var itemIsCollection = json["collection"]!.GetValue<bool>();
                    if (itemIsCollection)
                    {
                        if (actionType == "query")
                        {
                            Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetCollectionDetails(itemId);
                            StringBuilder builder = new();
                            builder.Append("[");
                            task.Wait();
                            var result = SteamWorkshopCollection.Parse(task.Result)[0];
                            List<string> list = Database.Contains(result);
                            if (list.Count > 0)
                            {
                                for (int i = 0; i < list.Count - 1; ++i)
                                {
                                    builder.Append($"\"{list[i]}\", ");
                                }
                                builder.Append($"\"{list[list.Count - 1]}\"]");
                                responseString = builder.ToString();
                            }
                        }
                        else if (actionType == "add")
                        {
                            Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetCollectionDetails(itemId);
                            StringBuilder builder = new();
                            builder.Append("[");
                            task.Wait();
                            var result = SteamWorkshopCollection.Parse(task.Result)[0];
                            Console.WriteLine($"Collection \"{itemTitle}\" added");
                            Console.Write($"Collection has {result.Count} items: ");
                            for (int i = 0; i < result.Count - 1; ++i)
                            {
                                Console.Write(result[i] + " ");
                                builder.Append($"\"{result[i]}\", ");
                            }
                            Console.Write(result.Last());
                            builder.Append($"\"{result.Last()}\"]");
                            Add(result);
                            responseString = builder.ToString();
                        }
                        else if (actionType == "remove")
                        {
                            Task<JsonNode> task = SteamWebAPI.ISteamRemoteStorage.GetCollectionDetails(itemId);
                            StringBuilder builder = new();
                            builder.Append("[");
                            task.Wait();
                            var result = SteamWorkshopCollection.Parse(task.Result)[0];
                            Console.WriteLine($"Collection \"{itemTitle}\" removed");
                            Console.Write($"Collection has {result.Count} items: ");
                            for (int i = 0; i < result.Count - 1; ++i)
                            {
                                Console.Write(result[i] + " ");
                                builder.Append($"\"{result[i]}\", ");
                            }
                            Console.Write(result.Last());
                            builder.Append($"\"{result.Last()}\"]");
                            Remove(result);
                            responseString = builder.ToString();
                        }
                    }
                    else
                    {
                        switch (actionType)
                        {
                            case "query":
                                responseString = Contains(itemId).ToString().ToLower();
                                // Console.WriteLine($"Item \"{itemTitle}\" existence status: {responseString}");
                                break;
                            case "add":
                                if (Add(itemId))
                                {
                                    responseString = $"Item \"{itemTitle}\" added";
                                    Console.WriteLine(responseString);
                                }
                                break;
                            case "remove":
                                if (Remove(itemId))
                                {
                                    responseString = $"Item \"{itemTitle}\" removed";
                                    Console.WriteLine(responseString);
                                }
                                break;
                            default: break;
                        }
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
