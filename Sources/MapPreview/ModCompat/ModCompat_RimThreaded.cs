using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MapPreview.Patches;

public static class ModCompat_RimThreaded
{
    public static bool IsPresent { get; private set; }

    private static MethodInfo InitializeAllThreadStatics;
    
    public static void Apply()
    {
        try
        {
            var rtType = GenTypes.GetTypeInAnyAssembly("RimThreaded.RimThreaded");
            if (rtType != null)
            {
                Log.Message(ModInstance.LogPrefix + "Applying compatibility patches for RimThreaded.");

                InitializeAllThreadStatics = AccessTools.Method(rtType, "InitializeAllThreadStatics");
                if (InitializeAllThreadStatics == null) throw new Exception("InitializeAllThreadStatics not found");

                ExactMapPreviewGenerator.OnPreviewThreadInit += InitThread;
                IsPresent = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(ModInstance.LogPrefix + "Failed to apply compatibility patches for RimThreaded!");
            Debug.LogException(e);
        }
    }

    private static void InitThread()
    {
        InitializeAllThreadStatics.Invoke(null, Array.Empty<object>());
    }
}