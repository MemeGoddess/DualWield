using DualWield.Stances;
using DualWield.Storage;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace DualWield
{
    public static class Ext_Pawn
    {
        public static Pawn_StanceTracker GetStancesOffHand(this Pawn instance)
        {
            if(DualWield.Instance.GetExtendedDataStorage() is { } store)
                return store.GetExtendedDataFor(instance).stancesOffhand;

            return null;
        }
        public static void SetStancesOffHand(this Pawn instance, Pawn_StanceTracker stancesOffHand)
        {
            if (DualWield.Instance.GetExtendedDataStorage() is { } store)
                store.GetExtendedDataFor(instance).stancesOffhand = stancesOffHand;
        }
        public static void TryStartOffHandAttack(this Pawn __instance, LocalTargetInfo targ, ref bool __result)
        {
            if(__instance.equipment == null || !__instance.equipment.TryGetOffHandEquipment(out _))
                return;

            var offhandStance = __instance.GetStancesOffHand();
            if (offhandStance.curStance is Stance_Warmup_DW || offhandStance.curStance is Stance_Cooldown)
                return;

            if (__instance.story != null && __instance.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
                return;

            if (__instance.jobs.curDriver.GetType().Name.Contains("Ability")) //Compatbility for Jecstools' abilities.
                return;

            // Is this meant to be ignored? - MG
            var allowManualCastWeapons = !__instance.IsColonist;
            var verb = __instance.TryGetOffhandAttackVerb(targ.Thing, true);

            if (verb == null) return;

            var success = verb.OffhandTryStartCastOn(targ);
            __result = __result || success;
        }
        public static Verb TryGetOffhandAttackVerb(this Pawn instance, Thing target, bool allowManualCastWeapons = false)
        {
            var equipment = instance.equipment;

            if (equipment == null || !equipment.TryGetOffHandEquipment(out var offHandEquip))
                return instance.TryGetMeleeVerbOffHand(target);

            var comp = offHandEquip.TryGetComp<CompEquippable>();

            if (comp == null)
                return instance.TryGetMeleeVerbOffHand(target);

            if (comp.PrimaryVerb.Available() &&
                (!comp.PrimaryVerb.verbProps.onlyManualCast ||
                 (instance.CurJob != null && instance.CurJob.def != JobDefOf.Wait_Combat) ||
                 allowManualCastWeapons))
                return comp.PrimaryVerb;

            return instance.TryGetMeleeVerbOffHand(target);
        }

        public static bool HasMissingArmOrHand(this Pawn instance)
        {
            return instance.health.hediffSet.GetMissingPartsCommonAncestors().Any(x =>
                x.Part.def == BodyPartDefOf.Hand || x.Part.def == BodyPartDefOf.Arm); ;
        }
        public static Verb TryGetMeleeVerbOffHand(this Pawn instance, Thing target)
        {
            var usableVerbs = new List<VerbEntry>();
            if (instance.equipment == null ||
                !instance.equipment.TryGetOffHandEquipment(out var offHandEquip))
                return null;

            var comp = offHandEquip.GetComp<CompEquippable>();

            var allVerbs = comp?.AllVerbs?.Where(x => x.IsMeleeAttack).ToList();
            if (allVerbs == null)
                return null;

            usableVerbs.AddRange(allVerbs
                    .Where(x => x.IsStillUsableBy(instance))
                    .Select(x => new VerbEntry(x, instance, allVerbs, allVerbs.Count)));

            return usableVerbs.TryRandomElementByWeight(ve => ve.GetSelectionWeight(target), out var result) 
                ? result.verb 
                : null;
        }

        public static bool RunAndGunEnabled(this Pawn pawn)
        {
            if (!(pawn?.AllComps.FirstOrDefault(x => x.GetType().Name == "CompRunAndGun") is { } comp)) 
                return false;

            var traverse = Traverse.Create(comp);
            return 
                traverse.Field("isEnabled").GetValue<bool>() || 
                traverse.Property("isEnabled").GetValue<bool>();

        }

    }
}
