using DualWield.Settings;
using DualWield.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return _extendedDataStorage ?? (_extendedDataStorage = Find.World.GetComponent<ExtendedDataStorage>());
        }
        
    }
}
