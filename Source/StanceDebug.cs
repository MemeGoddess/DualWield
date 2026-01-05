using System;
using System.Collections.Generic;
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
            var stance = __instance.stances.curStance;
            var offHandStance = __instance.GetStancesOffHand()?.curStance;

            var stanceString = "Stances: ";
            if (stance != null)
                stanceString += "\n" + stance + $" (<color={(stance.StanceBusy ? "red" : "green")}>{(stance is Stance_Busy stanceBusy ? stanceBusy.ticksLeft.ToString() : " ")}</color>)";

            if (offHandStance != null)
                stanceString += "\n" + offHandStance + $" (<color={(offHandStance.StanceBusy ? "red" : "green")}>{(offHandStance is Stance_Busy stanceBusy ? stanceBusy.ticksLeft.ToString() : " ")}</color>)";

            __result += "\n" + stanceString;
        } 
    }
#endif
}
