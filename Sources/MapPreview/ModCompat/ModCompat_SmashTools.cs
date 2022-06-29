using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MapPreview.Patches;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

/// <summary>
/// Patch to conditionally disable ComponentCache from the SmashTools utility mod used by The Vehicle Framework.
/// That feature otherwise causes preview generation to fail.
/// </summary>
public static class ModCompat_SmashTools
{
    public static bool IsPresent { get; private set; }
    
    public static void Apply()
    {
        try
        {
            var ccType = GenTypes.GetTypeInAnyAssembly("SmashTools.ComponentCache");
            if (ccType != null)
            {
                Log.Message(ModInstance.LogPrefix + "Applying compatibility patches for SmashTools from The Vehicle Framework.");
                Harmony harmony = new("Map Preview SmashTools Compat");

                var mgMethod = AccessTools.Method(ccType, "MapGenerated");

                var self = typeof(ModCompat_SmashTools);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod methodPatchPrefix = new(self.GetMethod(nameof(ComponentCache_MapGenerated), bindingFlags));

                harmony.Patch(mgMethod, methodPatchPrefix);
                IsPresent = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(ModInstance.LogPrefix + "Failed to apply compatibility patches for SmashTools from The Vehicle Framework!");
            Debug.LogException(e);
        }
    }
    
    private static bool ComponentCache_MapGenerated()
    {
        return !Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}