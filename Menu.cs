namespace SteamModManager
{
    public static class Menu
    {
        public static readonly string header = "SteamModManager> ";
        private static void Execute(string? command)
        {
            if (command == null)
            {
                return;
            }
            if (command.Length == 0)
            {
                return;
            }
            string[] args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length > 1)
            {
                string[] items = new ArraySegment<string>(args, 1, args.Length - 1).ToArray();
                switch (args[0])
                {
                    case "add":
                        Control.Add(items);
                        break;
                    case "delete":
                        Control.Remove(items);
                        break;
                    case "remove":
                        Control.Remove(items);
                        break;
                    case "update":
                        if (args[1] == "force")
                        {
                            if (args.Length == 2)
                            {
                                Control.Update(true);
                            }
                            else
                            {
                                Control.Update(items, true);
                            }
                        }
                        else
                        {
                            Control.Update(items);
                        }
                        break;
                    case "info":
                        Control.Info(items);
                        break;
                    case "search":
                        Control.Info(items);
                        break;
                    case "find":
                        Control.Info(items);
                        break;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
            else
            {
                switch(args[0])
                {
                    case "update":
                        Control.Update();
                        break;
                    case "backup":
                        Control.Backup();
                        break;
                    case "clear":
                        Control.Clear();
                        break;
                    case "info":
                        Control.Info();
                        break;
                    case "list":
                        Control.Info();
                        break;
                    case "listen":
                        Control.Listen(true);
                        break;
                    case "config":
                        Control.ShowConfig();
                        break;
                    case "reconfig":
                        Control.LoadConfiguration(true);
                        break;
                    case "help":
                        Control.Help();
                        break;
                    case "integrity":
                        Control.IntegrityCheck(true);
                        break;
                    case "quit":
                        break;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }
        public static void Default() 
        {
            string? input;
            do
            {
                Console.Write(header);
                input = Console.ReadLine();
                Execute(input);
            } while (input != "quit");
        }
        public static void Simple(string[] args)
        {
            if (args.Length > 1)
            {
                string[] items = new ArraySegment<string>(args, 1, args.Length - 1).ToArray();
                switch (args[0])
                {
                    case "-a": // add items
                        Control.Add(items);
                        break;
                    case "-i": // info query
                        Control.Info(items);
                        break;
                    case "-r": // remove items
                        Control.Remove(items);
                        break;
                    case "-u": // update items
                        Control.Update(items);
                        break;
                    case "-uf": // update items, forced
                        Control.Update(items, true);
                        break;
                    default:
                        Console.WriteLine("Invalid arguments.");
                        break;
                }
            }
            else
            {
                switch (args[0])
                {
                    case "-b": // backup
                        Control.Backup();
                        break;
                    case "-c": // clear directory
                        Control.Clear();
                        break;
                    case "-i": // information query
                        Control.Info();
                        break;
                    case "-ic": // integrity check
                        Control.IntegrityCheck(true);
                        break;
                    case "-l": // open http listener
                        Control.Listen(true);
                        break;
                    case "-u": // update all items
                        Control.Update();
                        break;
                    case "-uf":
                        Control.Update(true);
                        break;
                    default:
                        Console.WriteLine("Invalid arguments.");
                        break;
                }
            }
        }
    }
}
