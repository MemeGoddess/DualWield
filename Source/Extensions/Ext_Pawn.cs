using DualWield.Stances;
using DualWield.Storage;
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
            if(DualWield.Instance.GetExtendedDataStorage() is ExtendedDataStorage store)
            {
                return store.GetExtendedDataFor(instance).stancesOffhand;
            }
            return null;
        }
        public static void SetStancesOffHand(this Pawn instance, Pawn_StanceTracker stancesOffHand)
        {
            if (DualWield.Instance.GetExtendedDataStorage() is ExtendedDataStorage store)
            {
                store.GetExtendedDataFor(instance).stancesOffhand = stancesOffHand;
            }
        }
        public static void TryStartOffHandAttack(this Pawn __instance, LocalTargetInfo targ, ref bool __result)
        {
            if(__instance.equipment == null || !__instance.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
            {
                return;
            }
            var offhandStance = __instance.GetStancesOffHand();
            if (offhandStance.curStance is Stance_Warmup_DW || offhandStance.curStance is Stance_Cooldown)
            {
                return;
            }
            if (__instance.story != null && __instance.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
            {
                return;
            }
            if (__instance.jobs.curDriver.GetType().Name.Contains("Ability"))//Compatbility for Jecstools' abilities.
            {
                return;
            }
            bool allowManualCastWeapons = !__instance.IsColonist;
            Verb verb = __instance.TryGetOffhandAttackVerb(targ.Thing, true);
            
            if (verb != null)
            {
                bool success = verb.OffhandTryStartCastOn(targ);
                __result = __result || (verb != null && success);
            }
        }
        public static Verb TryGetOffhandAttackVerb(this Pawn instance, Thing target, bool allowManualCastWeapons = false)
        {
            Pawn_EquipmentTracker equipment = instance.equipment;
            ThingWithComps offHandEquip = null;
            CompEquippable compEquippable = null;
            if (equipment != null && equipment.TryGetOffHandEquipment(out ThingWithComps result) && result != equipment.Primary)
            {
                offHandEquip = result;//TODO: replace this temp code.
                compEquippable = offHandEquip.TryGetComp<CompEquippable>();
            }
            if (compEquippable != null && compEquippable.PrimaryVerb.Available() && (!compEquippable.PrimaryVerb.verbProps.onlyManualCast || (instance.CurJob != null && instance.CurJob.def != JobDefOf.Wait_Combat) || allowManualCastWeapons))
            {
                return compEquippable.PrimaryVerb;
            }
            else
            {
                return instance.TryGetMeleeVerbOffHand(target);
            }
        }
        public static bool HasMissingArmOrHand(this Pawn instance)
        {
            bool hasMissingHand = false;
            foreach (Hediff_MissingPart missingPart in instance.health.hediffSet.GetMissingPartsCommonAncestors())
            {
                if (missingPart.Part.def == BodyPartDefOf.Hand || missingPart.Part.def == BodyPartDefOf.Arm)
                {
                    hasMissingHand = true;
                }
            }
            return hasMissingHand;
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

    }
}
