using HarmonyLib;
using Verse;

namespace DualWield.CECompat.Harmony
{
    [HarmonyPatch(typeof(Ext_ThingDef), nameof(Ext_ThingDef.CanBeOffHand))]
    public static class Ext_ThingDef_CanBeOffHand
    {
        public static bool Prefix(ThingDef td, ref bool __result)
        {
            if (!DualWield.Settings.UseCombatExentedConfig)
            {
                return true;
            }
            __result = td?.weaponTags?.Contains("CE_OneHandedWeapon") ?? false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Ext_ThingDef), nameof(Ext_ThingDef.IsTwoHand))]
    public static class Ext_ThingDef_IsTwoHand
    {
        public static bool Prefix(ThingDef td, ref bool __result)
        {
            if (!DualWield.Settings.UseCombatExentedConfig)
            {
                return true;
            }
            __result = !(td?.weaponTags?.Contains("CE_OneHandedWeapon") ?? false);
            return false;
        }
    }
}