using DualWield.Settings;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
    class PawnRenderer_RenderPawnAt
    {
        static void Postfix(PawnRenderer __instance, ref Pawn ___pawn)
        {
            if (___pawn.Spawned && !___pawn.Dead)
            {
                ___pawn.GetStancesOffHand()?.StanceTrackerDraw();
            }
        }

        static void Prefix(PawnRenderer __instance, ref Pawn ___pawn)
        {
            if (___pawn.stances == null || ___pawn.stances.curStance != null) 
                return;

            Log.Warning($"Caught {___pawn.LabelShort} having a null stance while drawing");
            ___pawn.stances.SetStance(new Stance_Mobile());
        }

    }
}
