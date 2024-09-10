using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;

namespace SmartConnect;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;

    static readonly string ConfigFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

    static ConfigEntry<bool> enable;
    static ConfigEntry<int> timerSeconds;
    static ConfigEntry<bool> showTimer;
    static ConfigEntry<string> ipAddress;
    static ConfigEntry<bool> autoJoin;
    //static ConfigEntry<string> localSave;
    //static ConfigEntry<bool> autoHost;
    public static bool Enabled => enable.Value;
    public static int TimerSeconds => timerSeconds.Value;
    public static bool ShowTimer => showTimer.Value;
    public static string IPAddress => ipAddress.Value;
    public static bool AutoJoin => autoJoin.Value;

    //public static string LocalSave => localSave.Value;
    //public static bool AutoHost => autoHost.Value;
    public override void Load()
    {
        Instance = this;
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded! Note that this will do nothing on dedicated servers.");
    }
    static void InitConfig()
    {
        CreateFile(ConfigFile);

        enable = InitConfigEntry("General", "Enable", false, "Enable/Disable SmartConnect (auto continue).");
        timerSeconds = InitConfigEntry("General", "TimerSeconds", 5, "Timer in seconds before mod will continue, or join. Detected mouse left-clicks will deactivate the timer until next restart.");
        showTimer = InitConfigEntry("General", "ShowTimer", true, "Show radial timer on screen.");
        ipAddress = InitConfigEntry("General", "IPAddress", "", "Server address to join if auto join is enabled.");
        autoJoin = InitConfigEntry("General", "AutoJoin", false, "Enable to automatically join the entered server address instead of using 'Continue'.");
        //localSave = InitConfigEntry("General", "LocalSave", "", "Local save path to host from if auto host is enabled.");
        //autoHost = InitConfigEntry("General", "AutoHost", false, "Enable to automatically host the local save found at the entered path instead of using 'Continue'.");
    }
    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
                entry.Value = existingEntry.Value;
            }
        }
        return entry;
    }
    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();
        return true;
    }
    static void CreateFile(string file)
    {
        if (!File.Exists(file))
        {
            FileStream fileStream = File.Create(file);
            fileStream.Dispose();
        }
    }
}
