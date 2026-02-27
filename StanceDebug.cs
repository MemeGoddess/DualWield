using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace DualWield
{
#if DEBUG
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public class StanceDebug
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            var stanceString = __instance.GetStanceText();
            __result += stanceString;
        } 
    }

    public static class StanceText
    {
        public static string GetStanceText(this Pawn pawn)
        {
            var stance = pawn.stances.curStance;
            var stanceBusy = stance is Stance_Busy _stanceBusy ? _stanceBusy.StanceBusy : false;
            var stanceCooldown = stance is Stance_Busy _stanceBusyCooldown
                ? _stanceBusyCooldown.ticksLeft.ToString()
                : "0";
            var stanceVerb = stance is Stance_Busy _stanceCooldown
                ? (_stanceCooldown.verb.maneuver?.ToString() ?? (_stanceCooldown.verb is Verb_Shoot ? "Shoot" : ""))
                  + " by "
                  + (_stanceCooldown.verb.tool?.LabelCap ??
                     _stanceCooldown.verb.EquipmentSource?.LabelNoParenthesisCap ?? "")
                : "";

            var offHandStance = pawn.GetStancesOffHand()?.curStance;
            var offHandStanceBusy = offHandStance is Stance_Busy _offHandStanceBusy
                ? _offHandStanceBusy.StanceBusy
                : false;
            var offHandStanceCooldown = offHandStance is Stance_Busy _offHandStanceBusyCooldown
                ? _offHandStanceBusyCooldown.ticksLeft.ToString()
                : "0";
            var offHandStanceVerb = offHandStance is Stance_Busy _offHandStanceCooldown
                ? (_offHandStanceCooldown.verb?.maneuver?.ToString() ??
                   (_offHandStanceCooldown.verb is Verb_Shoot ? "Shoot" : ""))
                  + " by "
                  + (_offHandStanceCooldown.verb?.tool?.LabelCap ??
                     _offHandStanceCooldown.verb?.EquipmentSource.LabelNoParenthesisCap ?? "")
                : "";

            var stanceString = "";
            if (stance != null && !(stance is Stance_Mobile && stanceCooldown == "0"))
                stanceString += "\nM: " + stance.GetType().Name +
                                $" (<color={(stanceBusy ? "red" : "green")}>{(stanceCooldown)}</color>) {stanceVerb}";

            if (offHandStance != null && !(offHandStance is Stance_Mobile && offHandStanceCooldown == "0"))
                stanceString += "\nO: " + offHandStance.GetType().Name +
                                $" (<color={(offHandStanceBusy ? "red" : "green")}>{(offHandStanceCooldown)}</color>) {offHandStanceVerb}";

            return stanceString;
        }
    }
#endif
}
