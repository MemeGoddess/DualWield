using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(StatPart_GearStatOffset), nameof(StatPart_GearStatOffset.TransformValue))]
    public static class StatPart_OffhandGearStatOffset
    {
        private static FieldInfo apparelStatField = AccessTools.Field(typeof(StatPart_GearStatOffset), "apparelStat");
        private static FieldInfo subtractField = AccessTools.Field(typeof(StatPart_GearStatOffset), "subtract");
        private static FieldInfo includeWeaponField = AccessTools.Field(typeof(StatPart_GearStatOffset), "includeWeapon");
        public static void Postfix(StatPart_GearStatOffset __instance, StatRequest req, ref float val)
        {
            if (!req.HasThing || !(req.Thing is Pawn thing))
                return;

            var subtract = subtractField.GetValue(__instance) is true;
            var includeWeapon = includeWeaponField.GetValue(__instance) is true;
            var apparelStat = apparelStatField.GetValue(__instance) as StatDef;

            if (!includeWeapon || thing.equipment == null || thing.equipment.Primary == null)
                return;

            if (!thing.equipment.TryGetOffHandEquipment(out var offhand))
                return;

            var num1 = offhand.GetStatValue(apparelStat) + StatWorker.StatOffsetFromGear(thing.equipment.Primary, apparelStat);
            if (subtract)
                val -= num1;
            else
                val += num1;
        }
    }
}
