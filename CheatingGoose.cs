using Harmony;

using System;
using System.IO;
using System.Reflection;

using UnityEngine;

using VortexHarmonyInstaller;
using VortexHarmonyInstaller.ModTypes;
using VortexUnity;

// Define a settings object for us to store data.
//  The benefit in doing this is the fact that it allows
//  us to re-load these settings through each playthrough.
public class CheatModSettings: VortexModSettings
{
    private bool m_bCheatEnabled = false;
    public bool AreCheatsEnabled {
        get { return m_bCheatEnabled; }
        set { m_bCheatEnabled = value; }
    }

    public CheatModSettings()
    {
    }

    public override void Save(IExposedMod mod)
    {
        // This mod is of Vortex mod type; cast and save it.
        VortexMod vortexMod = mod as VortexMod;
        Save(this, vortexMod);
    }
}

public class CheatingGoose
{
    #region VariableDefs
    private static string DataPath { get; set; }

    private static VortexMod m_CheatingGooseMod = null;
    internal static VortexMod CheatingGooseMod { get { return m_CheatingGooseMod; } }

    private static CheatModSettings m_CheatSettings = null;
    internal static CheatModSettings CheatSettings { get { return m_CheatSettings; } }
    #endregion

    // Find/Resolve assembly files - in this case we expect all required assembly files
    //  to be located in the game's datapath "../Untitled_Data/Managed/".
    private static Assembly AssemblyResolver(object sender, ResolveEventArgs args)
    {
        string asmName = args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
        FileInfo[] libFiles = new DirectoryInfo(DataPath)
            .GetFiles("*.dll", SearchOption.TopDirectoryOnly);
        foreach (FileInfo dll in libFiles)
        {
            if (dll.Name == asmName)
            {
                return Assembly.LoadFile(dll.FullName);
            }
        }

        return null;
    }

    // The mod's entry point defined within our manifest file.
    public static void RunPatch(VortexMod modInfo)
    {
        try
        {
            // Set the DataPath value (we use this to resolve missing assembly references)
            DataPath = Path.Combine(Application.dataPath, "Managed");
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver;

            // We store the VortexMod instance for easy access throughout our code.
            m_CheatingGooseMod = modInfo;

            // The entry function is a good spot to assign delegates for VortexInstaller
            //  to invoke. In this case we want to display the mod's options within the
            //  in-game GUI.
            modInfo.OnGUI = OnGUI;

            RefreshModSettings();
            var harmony = HarmonyInstance.Create("com.blacktreegaming.goose.cheat.mod");
            harmony.PatchAll();
        }
        catch (Exception e)
        {
            modInfo.LogError("Patch failed", e);
        }
    }

    private static void RefreshModSettings()
    {
        // Attempt to load in existing setting files
        m_CheatSettings = VortexModSettings.Load<CheatModSettings>(CheatingGooseMod);
    }

    // To allow backwards compatibility for older versions of Unity
    //  and avoid being forced to create UI/Version specific AssetBundles
    //  for each mod Vortex mods use Unity's Immediate Mod GUI system.
    private static void OnGUI(VortexMod vortexMod)
    {
        if (CheatSettings == null)
            return;

        GUILayout.BeginVertical();
        GUILayout.Space(5);
        GUIStyle modBox = new GUIStyle(VortexUI.StyleDefs[Enums.EGUIStyleID.H1]);
        GUILayout.BeginHorizontal(modBox);

        // Vortex exposes several style definitions for mod authors to use when
        //  setting up their layout; but obviously it is possible to customize the
        //  the settings layout as needed.
        GUIStyle toggleStyle = new GUIStyle(VortexUI.StyleDefs[Enums.EGUIStyleID.TOGGLE]);
        toggleStyle.padding = new RectOffset(14, 14, 14, 7);

        GUILayout.Space(10);

        // We create the toggle and assign its value to the "AreCheatsEnabled" setting.
        CheatSettings.AreCheatsEnabled = GUILayout.Toggle(CheatSettings.AreCheatsEnabled, "", toggleStyle);

        GUIStyle label = new GUIStyle(VortexUI.StyleDefs[Enums.EGUIStyleID.H2]);
        label.padding = Util.RectOffset(0);
        label.margin = Util.RectOffset(0);
        label.padding = Util.RectOffset(5);
        label.fontSize = (int)(VortexUI.GlobalFontSize * 0.65f);

        GUILayout.Label("Enable Cheat Mode", label, GUILayout.ExpandWidth(false));

        GUILayout.EndHorizontal();
        if (GUILayout.Button("Save Settings", VortexUI.StyleDefs[Enums.EGUIStyleID.ACTION_BUTTON], GUILayout.ExpandWidth(false)))
        {
            // Each mod is different; some mods may not require the
            //  settings object to be saved to the drive - that's something
            //  for the mod author to decide. In this case we _do_ want the 
            //  cheat options to be persistent so we provide the user with a
            //  "Save Settings" button to click when they're happy with the
            //  changes they made.
            CheatSettings.Save(vortexMod);
        }
        GUILayout.EndVertical();
    }

    // Untitled Goose Game seems to have a "CheatManager"
    //  class defined - given that this _is_ a cheat mod,
    //  this is a good place for us to insert our patch.
    //
    // Harmony provides us with a set of annotations/attributes
    //  it uses to make our life easier when patching methods.
    //  We're providing the type and the name of the method we wish
    //  to patch which is the Update function in this case.
    //
    //  Keep in mind that Unity's update function 
    [HarmonyPatch(typeof(CheatManager))]
    [HarmonyPatch("Update")]
    class CheatManagerPatch
    {
        // We want our patch to execute before the original function;
        //  this is why we add the HarmonyPrefix attribute. 
        [HarmonyPrefix]
        static void Prefix()
        {
            // Conveniently, UGG has an "allowCheats" parameter tied to its global
            //  settings object; enabling this parameter gives the user access to the
            //  game's inbuilt cheat mechanics such as teleportation ('pgup/pgdown' keys) and
            //  enabling/disabling NPC AI ('K' key)
            bool bGameCheatStatus = GameSettings.currentSettings.allowCheats;
            bool bWantedCheatStatus = CheatSettings.AreCheatsEnabled;
            if (bGameCheatStatus != bWantedCheatStatus)
            {
                GameSettings.currentSettings.allowCheats = bWantedCheatStatus;
            }
        }
    }
}
