using DualWield.Stances;
using DualWield.Storage;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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

                return !__instance.CasterPawn.stances.FullBodyBusy;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public class Verb_TryCastNextBurstShot
    {
        [HarmonyPriority(Priority.Low)]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var patched = false;
            var setStance = AccessTools.Method(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.SetStance));

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Callvirt || !(code[i].operand is MethodInfo mi) ||
                    mi != setStance) continue;

                code[i] = new CodeInstruction(OpCodes.Call,
                    typeof(Verb_TryCastNextBurstShot).GetMethod("SetStanceOffHand"));
                patched = true;
            }

            if(!patched)
            {
                Log.Error("Unable to patch SetStance for DualWield. This causes dual wielding weapons to have no cooldown. " +
                          "It's likely that another mod is also patching this method, but I haven't been able to narrow it down yet. - Meme Goddess");
            }

            return code;
        }

        public static void SetStanceOffHand(Pawn_StanceTracker stanceTracker, Stance_Cooldown stance)
        {
            var isOffhand = false;


            if (stance.verb.EquipmentSource != null &&
                DualWield.Instance.GetExtendedDataStorage().TryGetExtendedDataFor(stance.verb.EquipmentSource,
                    out var twcdata) && twcdata.isOffHand)
            {
                var offHandEquip = stance.verb.EquipmentSource;
                isOffhand = offHandEquip.TryGetComp<CompEquippable>() != null;
            }

            if (isOffhand)
            {
                var offhandStanceTracker = stanceTracker.pawn.GetStancesOffHand();
                offhandStanceTracker.SetStance(stance);
                return;
            }

            stanceTracker.SetStance(stance.GetType().Name != "Stance_RunAndGun_Cooldown"
                ? new Stance_Cooldown_DW(stance.ticksLeft, stance.focusTarg, stance.verb)
                : stance);
        }
    }
}
