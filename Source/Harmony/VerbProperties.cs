using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(VerbProperties), "AdjustedCooldown")]
    [HarmonyPatch(new Type[] { typeof(Tool), typeof(Pawn), typeof(Thing) })]
    class VerbProperties_AdjustedCooldown
    {
        static void Postfix(VerbProperties __instance, Thing equipment, Pawn attacker, ref float __result)
        {
            if (attacker?.equipment == null || __instance.category == VerbCategory.BeatFire) return;

            if (!attacker.equipment.TryGetOffHandEquipment(out _))
                return;

            var skillLevel = attacker.skills == null
                ? 8
                : (__instance.IsMeleeAttack
                    ? attacker.skills.GetSkill(SkillDefOf.Melee)
                    : attacker.skills.GetSkill(SkillDefOf.Shooting)).levelInt;

            var staticPenalty = (equipment is ThingWithComps twc && twc.IsOffHand()
                ? DualWield.Settings.StaticCooldownPOffHand
                : DualWield.Settings.StaticCooldownPMainHand) / 100f;
            var dynamicPenalty = (DualWield.Settings.DynamicCooldownP / 100f) * (20 - skillLevel);

            __result *= 1.0f + staticPenalty + dynamicPenalty;
        }
    }

    [HarmonyPatch(typeof(VerbProperties), "AdjustedAccuracy")]
    class VerbProperties_AdjustedAccuracy
    {
        static void Postfix(VerbProperties __instance, Thing equipment, ref float __result)
        {
            if (!(equipment is { ParentHolder: Pawn_EquipmentTracker peqt })) return;

            var pawn = peqt.pawn;

            if (!pawn.equipment.TryGetOffHandEquipment(out _))
                return;

            var skillLevel = pawn.skills == null
                ? 8
                : (__instance.IsMeleeAttack
                    ? pawn.skills.GetSkill(SkillDefOf.Melee)
                    : pawn.skills.GetSkill(SkillDefOf.Shooting)).levelInt;

            var staticPenalty = (equipment is ThingWithComps twc && twc.IsOffHand()
                ? DualWield.Settings.StaticAccPOffHand
                : DualWield.Settings.StaticAccPMainHand) / 100f;
            var dynamicPenalty = (DualWield.Settings.DynamicAccP / 100f) * (20 - skillLevel);

            __result *= 1.0f - staticPenalty - dynamicPenalty;
        }
    }
}
