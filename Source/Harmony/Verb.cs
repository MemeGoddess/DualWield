using DualWield.Stances;
using DualWield.Storage;
using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(Verb), "TryStartCastOn", new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public class Verb_TryStartCastOn {
        static bool Prefix(Verb __instance, LocalTargetInfo castTarg, ref bool __result)
        {
            if(__instance.caster is Pawn casterPawn)
            {
                //Check if it's an enemy that's attacked, and not a fire or an arguing husband
                if ((!casterPawn.InMentalState && !(castTarg.Thing is Fire)))
                {
                    casterPawn.TryStartOffHandAttack(castTarg, ref __result);
                }

                if (__instance.CasterPawn.stances.curStance == null)
                {
                    Log.Warning($"Caught {casterPawn.LabelShort} having a null stance while attacking");
                    __instance.CasterPawn.stances.SetStance(new Stance_Mobile());
                }

                return !__instance.CasterPawn.stances.FullBodyBusy;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance))]
    public static class Pawn_StanceTracker_SetStance
    {
        static bool Prefix(Pawn_StanceTracker __instance, ref Stance newStance)
        {
            if (__instance?.pawn == null || !(newStance is Stance_Cooldown cooldown))
            {
                return true;
            }

            if (ShouldRouteToOffHand(__instance, cooldown))
            {
                var offHandStanceTracker = __instance.pawn.GetStancesOffHand();
                if (offHandStanceTracker != null && !ReferenceEquals(offHandStanceTracker, __instance))
                {
                    offHandStanceTracker.SetStance(cooldown);
                    return false;
                }
            }

            if (ShouldWrapMainHandCooldown(__instance, cooldown))
            {
                if(cooldown == null)
                    Log.Error("Cooldown null");
                if(cooldown.ticksLeft == null)
                    Log.Error("ticksLeft was null");
                if (cooldown.focusTarg == null)
                    Log.Error("focusTarg was null");
                if(cooldown.verb == null)
                    Log.Error("verb was null");
                newStance = new Stance_Cooldown_DW(cooldown.ticksLeft, cooldown.focusTarg, cooldown.verb);
            }

            return true;
        }

        private static bool ShouldRouteToOffHand(Pawn_StanceTracker stanceTracker, Stance_Cooldown stance)
        {
            if (stanceTracker == null || stance?.verb?.EquipmentSource == null)
            {
                return false;
            }

            if (!(DualWield.Instance.GetExtendedDataStorage() is { } store) ||
                !store.TryGetExtendedDataFor(stance.verb.EquipmentSource, out var twcdata) ||
                !twcdata.isOffHand)
            {
                return false;
            }

            return stance.verb.EquipmentSource.TryGetComp<CompEquippable>() != null &&
                   ReferenceEquals(stanceTracker, stanceTracker.pawn.stances);
        }

        private static bool ShouldWrapMainHandCooldown(Pawn_StanceTracker stanceTracker, Stance_Cooldown stance)
        {
            return ReferenceEquals(stanceTracker, stanceTracker.pawn.stances) &&
                   !(stance is Stance_Cooldown_DW) &&
                   stance.GetType().Name != "Stance_RunAndGun_Cooldown" &&
                   stance.verb != null;
        }
    }
}
