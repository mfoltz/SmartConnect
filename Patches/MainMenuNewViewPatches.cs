using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.Shared;
using ProjectM.UI;
using Stunlock.Network;
using Stunlock.Platform.PC;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmartConnect.Patches;

[HarmonyPatch]
internal static class MainMenuNewViewPatches
{
    static readonly bool Active = Plugin.Enabled;
    static readonly bool AutoJoin = Plugin.AutoJoin;
    //static readonly bool AutoHost = Plugin.AutoHost;
    static readonly int TimerSeconds = Plugin.TimerSeconds;
    static readonly bool ShowTimer = Plugin.ShowTimer;
    static readonly string IPAddress = Plugin.IPAddress;
    //static readonly string LocalSave = Plugin.LocalSave;

    static GameObject timerGameObject;
    static Image timerObjectImage;
    static bool consoleReady = false;
    static bool triggered = false;
    static DateTime timerStart = DateTime.MinValue;

    static readonly string ResourcePath = "SmartConnect.Resources.icon.png";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenuNewView), nameof(MainMenuNewView.SetConsoleReady))]
    static void ConsoleReadyPostfix()
    {
        if (!Active) return;
        if (triggered) return;

        consoleReady = true;
        timerStart = DateTime.UtcNow;
    }

    [HarmonyPatch(typeof(MainMenuNewView), nameof(MainMenuNewView.Update))]
    [HarmonyPrefix]
    static void UpdatePrefix()
    {
        if (!Active) return;
        if (!consoleReady) return;
        if (triggered) return;

        if (ShowTimer && timerGameObject == null)
        {
            // Find the canvas base
            MainMenuCanvasBase mainMenuCanvasBase = UnityEngine.Object.FindObjectOfType<MainMenuCanvasBase>();

            if (mainMenuCanvasBase != null)
            {
                // Create a new GameObject to hold the radial timer image
                GameObject timerObject = new("RadialTimer");
                GameObject.DontDestroyOnLoad(timerObject);

                // rectTransform, set parent
                RectTransform rectTransform = timerObject.AddComponent<RectTransform>();
                timerGameObject = timerObject;
                timerObject.transform.SetParent(mainMenuCanvasBase.MenuParent, false);

                // load texture from png
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(ResourcePath);

                // Make circular sprite for masking
                Texture2D spriteTexture = LoadBackgroundTextureFromStream(stream);
                Sprite circularSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0.5f, 0.5f));

                // Add an Image component to the timer object
                Image timerImage = timerObject.AddComponent<Image>();
                timerObjectImage = timerImage;
                timerImage.sprite = circularSprite;
                timerImage.type = Image.Type.Filled;
                timerImage.fillMethod = Image.FillMethod.Radial360;
                timerImage.fillClockwise = false;
                timerImage.fillOrigin = 2;
                timerImage.fillAmount = 0f;
                timerImage.color = new Color(1.0f, 0.3f, 0.3f, 1.0f);  // Brighter red with full opacity

                // Set the RectTransform to properly size and position the timer in the UI
                rectTransform.sizeDelta = new Vector2(spriteTexture.width / 2, spriteTexture.height / 2);
                rectTransform.anchoredPosition = new Vector2(0, 0);  // Set position relative to the canvas
                rectTransform.localScale = Vector3.one;

                // Set active
                timerObject.SetActive(true);
            }
        }
        else if (ShowTimer)
        {
            timerObjectImage.fillAmount = (float)(DateTime.UtcNow - timerStart).TotalSeconds / (float)TimerSeconds;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Plugin.LogInstance.LogInfo("Mouse left-click detected, stopping timer till next restart...");
            triggered = true;
            if (ShowTimer) timerObjectImage.color = Color.clear;
            return;
        }

        if (DateTime.UtcNow - timerStart > TimeSpan.FromSeconds(TimerSeconds))
        {
            if (timerGameObject != null) GameObject.Destroy(timerGameObject);
            MainMenuNewView mainMenu = UnityEngine.Object.FindObjectOfType<MainMenuNewView>();

            if (mainMenu != null)
            {
                triggered = true;

                if (!string.IsNullOrEmpty(IPAddress) && AutoJoin)
                {
                    string[] split = IPAddress.Split(':');
                    string address = split[0];
                    ushort port = ushort.Parse(split[1]);
                    ConnectAddress connectAddress = ConnectAddress.CreateSteamIPv4(address, port);

                    try
                    {
                        ServerHistoryEntry serverHistoryEntry = new()
                        {
                            ConnectAddress = connectAddress
                        };

                        mainMenu.JoinGame(serverHistoryEntry);
                    }
                    catch (Exception e)
                    {
                        Plugin.LogInstance.LogError("Failed to join server:" + e);
                    }
                }
                else
                {
                    mainMenu.ContinueButton_OnClick();
                }
                /*
                else if (!string.IsNullOrEmpty(LocalSave) && AutoHost)
                {
                    try
                    {
                        var persistentDataPathMatch = Regex.Match(LocalSave, @"(.*\\VRising)");
                        string persistentDataPath = persistentDataPathMatch.Value;
                        Plugin.LogInstance.LogInfo($"Found persistent data path: {persistentDataPath}");

                        var guidMatch = Regex.Match(LocalSave, @"[^\\]+$"); // This ensures the match excludes the backslash
                        string folderGuid = guidMatch.Value;
                        Plugin.LogInstance.LogInfo($"Found session id: {folderGuid}");

                        var saveFiles = Directory.GetFiles(LocalSave, "AutoSave_*.save.gz");
                        string highestAutoSave = saveFiles
                            .Select(file => new { FileName = file, Number = GetAutoSaveNumber(file) })
                            .OrderByDescending(f => f.Number)
                            .First().FileName;

                        string saveFileName = Path.GetFileName(highestAutoSave);
                        Plugin.LogInstance.LogInfo($"Found highest auto save: {saveFileName}");

                        SaveFileData saveFileData = LoadSessionDataFromFiles(LocalSave, persistentDataPath, folderGuid, saveFileName);
                        
                        if (saveFileData != null)
                        {
                            mainMenu.HostGame(saveFileData);
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.LogInstance.LogError("Failed to host local save:" + e);
                    }
                }
                */
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MainMenuNewView), nameof(MainMenuNewView.OnDestroy))]
    static void OnDestroyPrefix()
    {
        if (timerGameObject != null) GameObject.Destroy(timerGameObject);
    }
    static SaveFileData LoadSessionDataFromFiles(string saveFolderPath, string persistentDataPath, string folderGuid, string saveFileName)
    {
        try
        {
            // Paths to the SessionId.json and StartDate.json files
            string sessionIdFilePath = Path.Combine(saveFolderPath, "SessionId.json");
            string startDateFilePath = Path.Combine(saveFolderPath, "StartDate.json");

            // Step 1: Read the first line from SessionId.json and assign to static property
            if (File.Exists(sessionIdFilePath))
            {
                string sessionId = File.ReadLines(sessionIdFilePath).FirstOrDefault();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    SaveFileData.SessionIdFileName = sessionId;  // Set static property
                    Console.WriteLine($"SessionIdFileName set to: {SaveFileData.SessionIdFileName}");
                }
                else
                {
                    throw new Exception("SessionId.json is empty or could not be read.");
                }
            }
            else
            {
                throw new Exception($"SessionId.json not found at path: {sessionIdFilePath}");
            }

            // Step 2: Read the first line from StartDate.json and assign to static property
            if (File.Exists(startDateFilePath))
            {
                string startDate = File.ReadLines(startDateFilePath).FirstOrDefault();
                if (!string.IsNullOrEmpty(startDate))
                {
                    SaveFileData.StartDateFileName = startDate;  // Set static property
                    Console.WriteLine($"StartDateFileName set to: {SaveFileData.StartDateFileName}");
                }
                else
                {
                    throw new Exception("StartDate.json is empty or could not be read.");
                }
            }
            else
            {
                throw new Exception($"StartDate.json not found at path: {startDateFilePath}");
            }

            // Step 3: Create and populate a SaveFileData object
            SaveFileData saveFileData = new()
            {
                Path = persistentDataPath,
                SessionId = folderGuid,
                Name = saveFileName
            };

            return saveFileData;  // Return the populated SaveFileData object
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading session data: {e.Message}");
            return null;  // Return null if there's an error
        }
    }
    static int GetAutoSaveNumber(string fileName)
    {
        var match = Regex.Match(Path.GetFileName(fileName), @"AutoSave_(\d+)\.save\.gz");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return 0; // Default to 0 if no number is found
    }
    public static void FindGameObjects(Transform root, bool includeInactive = false)
    {
        // Stack to hold the transforms to be processed
        Stack<(Transform transform, int indentLevel)> transformStack = new();
        transformStack.Push((root, 0));

        // HashSet to keep track of visited transforms to avoid cyclic references
        HashSet<Transform> visited = [];

        Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
        List<Transform> transforms = [.. children];

        while (transformStack.Count > 0)
        {
            var (current, indentLevel) = transformStack.Pop();

            if (!visited.Add(current))
            {
                // If we have already visited this transform, skip it
                continue;
            }

            List<string> objectComponents = FindGameObjectComponents(current.gameObject);

            // Create an indentation string based on the indent level
            string indent = new('|', indentLevel);

            // Write the current GameObject's name and some basic info to the file
            Plugin.LogInstance.LogInfo($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

            // Add all children to the stack
            foreach (Transform child in transforms)
            {
                if (child.parent == current)
                {
                    transformStack.Push((child, indentLevel + 1));
                }
            }
        }
    }
    public static List<string> FindGameObjectComponents(GameObject parentObject)
    {
        List<string> components = [];

        int componentCount = parentObject.GetComponentCount();
        for (int i = 0; i < componentCount; i++)
        {
            components.Add($"{parentObject.GetComponentAtIndex(i).GetIl2CppType().FullName}({i})");
        }

        return components;
    }
    public static Texture2D LoadBackgroundTextureFromStream(Stream stream)
    {
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(buffer);

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        return texture;
    }
}

