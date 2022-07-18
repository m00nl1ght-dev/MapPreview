using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MapPreview.ModCompat;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

[StaticConstructorOnStartup]
internal static class ModCompat_RimThreaded
{
    public static bool IsPresent { get; }

    private static readonly MethodInfo InitializeAllThreadStatics;
    
    static ModCompat_RimThreaded()
    {
        try
        {
            var rtType = GenTypes.GetTypeInAnyAssembly("RimThreaded.RimThreaded");
            if (rtType != null)
            {
                Log.Message(Main.LogPrefix + "Applying compatibility patches for RimThreaded.");

                InitializeAllThreadStatics = AccessTools.Method(rtType, "InitializeAllThreadStatics");
                if (InitializeAllThreadStatics == null) throw new Exception("InitializeAllThreadStatics not found");

                MapPreviewGenerator.OnPreviewThreadInit += InitThread;
                IsPresent = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(Main.LogPrefix + "Failed to apply compatibility patches for RimThreaded!");
            Debug.LogException(e);
        }
    }

    private static void InitThread()
    {
        InitializeAllThreadStatics.Invoke(null, Array.Empty<object>());
    }
}