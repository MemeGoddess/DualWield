using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace DualWield
{
    internal class FloatMenuOptionProvider_Drop : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool CanSelfTarget => true;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            Pawn_EquipmentTracker equipment = context.FirstSelectedPawn.equipment;
            if (equipment != null && equipment.TryGetOffHandEquipment(out ThingWithComps eq))
            {
                return true;
            }
            return false;
        }

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (clickedPawn != context.FirstSelectedPawn)
            {
                return null;
            }
            if (clickedPawn.equipment.TryGetOffHandEquipment(out ThingWithComps eq))
            {
                if (clickedPawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(eq, clickedPawn))
                {
                    return new FloatMenuOption("CannotDrop".Translate(eq.Label, eq) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                }
                Action action = delegate ()
                {
                    clickedPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, eq), new JobTag?(JobTag.Misc), false);
                };
                return new FloatMenuOption("DW_DropOffHand".Translate(eq.LabelShort, eq), action, eq, Color.white, MenuOptionPriority.Default, null, clickedPawn, 0f, null, null, true, 0);
            }
            return null;
        }
    }
}
