using SteamModManager;
using Extext.Ini;

if (args.Length > 1)
{
    string[] items = new ArraySegment<string>(args, 2, args.Length - 3).ToArray();
    switch (args[1])
    {
        case "-a": // add items
            Control.Add(items);
            break;
        case "-b": // backup

            break;
        case "-c": // clear directory

            break;
        case "-h": // help

            break;
        case "-l": // list all mod

            break;
        case "-o": // open http listener

            break;
        case "-s": // search item

            break;
        case "-r": // remove items
            Control.Remove(items);
            break;
        case "-u": // update items, default = all 

            break;

        default:
            break;
    }
}
else
{
    Control.LoadConfiguration();
}