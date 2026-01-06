using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace DualWield.Stances
{
#if DEBUG
    [StaticConstructorOnStartup]
#endif
    class Stance_Cooldown_DW : Stance_Cooldown
    {
        private const float MaxRadius = 0.5f;
        private bool hasOffhand = false;
#if DEBUG
        private static Material AimPieMaterial;
#endif
        public override bool StanceBusy => !hasOffhand || !(Pawn?.GetStancesOffHand().curStance is Stance_Mobile);

        public Stance_Cooldown_DW()
        {
        }

#if DEBUG
        static Stance_Cooldown_DW()
        {
            AimPieMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(Color.red.r, Color.red.g, Color.red.b, 0.3f));
        }
#endif
        public Stance_Cooldown_DW(int ticks, LocalTargetInfo focusTarg, Verb verb) : base(ticks, focusTarg, verb)
        {
            hasOffhand = verb.CasterIsPawn && verb.CasterPawn.equipment != null && verb.CasterPawn.equipment.TryGetOffHandEquipment(out _);
        }
#if DEBUG
        public override void StanceDraw()
        {
            var save = GUI.color;
            GUI.color = Color.red;

            var center = this.stanceTracker.pawn.Drawer.DrawPos + new Vector3(0.0f, 0.2f, 0.0f);
            var radius = Mathf.Min(0.5f, (float)this.ticksLeft * (1f / 500f));
            var s = new Vector3(radius, 1f, radius);
            var matrix = new Matrix4x4();
            matrix.SetTRS(center, Quaternion.identity, s);
            Graphics.DrawMesh(MeshPool.circle, matrix, AimPieMaterial, 0);

            GUI.color = save;
        }
#endif
    }
}
