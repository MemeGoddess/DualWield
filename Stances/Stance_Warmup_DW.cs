using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DualWield.Stances
{
    class Stance_Warmup_DW : Stance_Warmup
    {
        public override bool StanceBusy => true;

        public Stance_Warmup_DW()
        {
        }

        public Stance_Warmup_DW(int ticks, LocalTargetInfo focusTarg, Verb verb) : base(ticks, focusTarg, verb)
        {
        }

        public override void StanceDraw()
        {
            if (!Find.Selector.IsSelected(stanceTracker.pawn)) return;

            var shooter = stanceTracker.pawn;
            var target = focusTarg;
            var facing = 0f;
            if (target.Cell != shooter.Position)
                facing = target.Thing != null 
                    ? (target.Thing.DrawPos - shooter.Position.ToVector3Shifted()).AngleFlat() 
                    : (target.Cell - shooter.Position).AngleFlat;

            var zOffSet = 0f;
            var xOffset = 0f;

            if (shooter.Rotation == Rot4.East)
                zOffSet = 0.1f;
            else if (shooter.Rotation == Rot4.West)
                zOffSet = -0.1f;
            else if (shooter.Rotation == Rot4.South)
                xOffset = 0.1f;
            else
                xOffset = -0.1f;

            GenDraw.DrawAimPieRaw(shooter.DrawPos + new Vector3(xOffset, 0.2f, zOffSet), facing,
                (int)((float)ticksLeft * pieSizeFactor));
        }

        public override void StanceTick()
        {
            base.StanceTick();

            if (!Pawn.RunAndGunEnabled() && Pawn.pather.MovingNow) 
                stanceTracker.pawn.GetStancesOffHand().SetStance(new Stance_Mobile());
        }

        protected override void Expire()
        {
            verb.WarmupComplete();
            if (stanceTracker.curStance == this) 
                stanceTracker.pawn.GetStancesOffHand().SetStance(new Stance_Mobile());
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "CleanupCurrentJob")]
    public static class Stance_Warmup_DW_Cancel
    {
        private static FieldInfo pawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
        public static void Postfix(Pawn_JobTracker __instance,
            JobCondition condition,
            bool releaseReservations,
            bool cancelBusyStancesSoft = true,
            bool canReturnToPool = false,
            bool? carryThingAfterJobOverride = null)
        {
            if (!cancelBusyStancesSoft)
                return;

            var pawn = pawnField.GetValue(__instance) as Pawn;

            var stances = pawn?.GetStancesOffHand();

            stances?.CancelBusyStanceSoft();
        }
    }
}
