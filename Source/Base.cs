using DualWield.Settings;
using DualWield.Storage;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DualWield
{
    public class Base : Mod
    {
        public static Base Instance { get; private set; }
        public static DWSettings Settings { get; private set; }
        ExtendedDataStorage _extendedDataStorage;


        public Base(ModContentPack config) : base(config)
        {
            Instance = this;
            Settings = GetSettings<DWSettings>();
        }

        public override string SettingsCategory()
        {
            return "Dual Wield - Continued";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public ExtendedDataStorage GetExtendedDataStorage()
        {
            return _extendedDataStorage ?? Find.World.GetComponent<ExtendedDataStorage>();
        }

        public void RefreshExtendedDataStorage()
        {
            _extendedDataStorage = Find.World.GetComponent<ExtendedDataStorage>();
        }
    }

    [HarmonyPatch(typeof(World))]
    [HarmonyPatch(nameof(World.FinalizeInit))]
    [HarmonyPatch(new System.Type[] { typeof(bool) })]
    internal static class ExtendedDataStorageLoader
    {
        [HarmonyPostfix]
        private static void LoadComp(World __instance, bool loadFrom) => Base.Instance.RefreshExtendedDataStorage();
    }
}
