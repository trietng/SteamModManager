using SteamModManager;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    SteamCMD.EnsureWindowsExecutableExistence();
}
Control.LoadConfiguration();
if (args.Length > 0)
{
    Menu.Simple(args);
}
else
{
    Control.IntegrityCheck();
    Control.AutoUpdate();
    Control.Listen();
    Menu.Default();
}