using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace CheatingGooseMod
{
  public static class ModInfo
  {
    public const string GUID = "com.blacktreegaming.cheatinggoose";
    public const string Name = "Cheating Goose";
    public const string Version = "1.0.1";
  }

  [BepInPlugin (ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
  [HarmonyPatch]
  public class CheatingGoose : BaseUnityPlugin
  {
    private void Awake()
    {
      new Harmony (ModInfo.GUID).PatchAll (typeof (CheatingGoose));
    }

    [HarmonyPostfix]
    [HarmonyPatch (typeof (CheatManager), "Start")]
    private static void EnableCheats()
    {
      GameSettings.currentSettings.allowCheats = true;
    }

  }
}
