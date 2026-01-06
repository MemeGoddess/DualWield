using DualWield.Stances;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "GetUpdatedAvailableVerbsList")]
    class Pawn_MeleeVerbs_GetUpdatedAvailableVerbsList
    {
        static void Postfix(ref List<VerbEntry> __result)
        {
            //remove all offhand verbs so they're not used by for mainhand melee attacks.
            Pawn pawn = null;
             List<VerbEntry> shouldRemove = new List<VerbEntry>();
            foreach (VerbEntry ve in __result)
            {
                pawn ??= ve.verb.CasterPawn;
                if (ve.verb.EquipmentSource != null && ve.verb.EquipmentSource.IsOffHand())
                {
                    shouldRemove.Add(ve);
                }
            }

            if (pawn == null)
                return;

            foreach (VerbEntry ve in shouldRemove)
            {
                __result.Remove(ve);
            }

            var leftovers = __result.Select(x => x.verb).ToList();
            var highestSelWeight = leftovers.Max(x => VerbUtility.InitialVerbWeight(x, pawn));
            __result = leftovers.Select(x => new VerbEntry(x, pawn, leftovers, highestSelWeight)).ToList();
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs),"TryMeleeAttack")]
    class Pawn_MeleeVerbs_TryMeleeAttack
    {
        static bool Prefix(Pawn_MeleeVerbs __instance, Thing target, Verb verbToUse, bool surpriseAttack, ref bool __result, ref Pawn ___pawn)
        {
            var stance = ___pawn.GetStancesOffHand();
            if (stance == null || stance.curStance is Stance_Warmup_DW || stance.curStance is Stance_Cooldown)
                  return true;
            if (___pawn.equipment == null || !___pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip))
                 return true;

            if (___pawn.InMentalState)
                return true;

            var verb = __instance.Pawn.TryGetMeleeVerbOffHand(target);
            if(verb != null)
            {
                var success = verb.OffhandTryStartCastOn(target);
                __result = __result || success;
            }

            return !___pawn.stances.FullBodyBusy;
        }
    }
}
