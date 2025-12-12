using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace DualWield
{
    public class FloatMenuOptionProvider_Equip : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool AppliesInt(FloatMenuContext context) => context.FirstSelectedPawn.equipment != null;
        protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            string labelShort = clickedThing.LabelShort;
            FloatMenuOption menuItem;

            if (clickedThing.def.IsWeapon && context.FirstSelectedPawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "IsIncapableOfViolenceLower".Translate(context.FirstSelectedPawn.LabelShort, context.FirstSelectedPawn) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (!context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (!context.FirstSelectedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (clickedThing.IsBurning())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " (" + "BurningLower".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (context.FirstSelectedPawn.HasMissingArmOrHand())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_MissArmOrHand".Translate() + " )", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (context.FirstSelectedPawn.equipment != null && context.FirstSelectedPawn.equipment.Primary != null && context.FirstSelectedPawn.equipment.Primary.def.IsTwoHand())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_WieldingTwoHanded".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (clickedThing.def.IsTwoHand())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_NoTwoHandedInOffHand".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else if (!clickedThing.def.CanBeOffHand())
            {
                menuItem = new FloatMenuOption("CannotEquip".Translate(labelShort) + " " + "DW_AsOffHand".Translate() + " ( " + "DW_CannotBeOffHand".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else
            {
                string text5 = "DW_EquipOffHand".Translate(labelShort);
                if (clickedThing.def.IsRangedWeapon && context.FirstSelectedPawn.story != null && context.FirstSelectedPawn.story.traits.HasTrait(TraitDefOf.Brawler))
                {
                    text5 = text5 + " " + "EquipWarningBrawler".Translate();
                }
                menuItem = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text5, delegate
                {
                    FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, FleckDefOf.FeedbackEquip, 1f);
                    clickedThing.SetForbidden(false, true);
                    context.FirstSelectedPawn.jobs.TryTakeOrderedJob(new Job(DW_DefOff.DW_EquipOffhand, clickedThing), JobTag.Misc);
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                }, MenuOptionPriority.High, null, null, 0f, null, null), context.FirstSelectedPawn, clickedThing, "ReservedBy");
            }

            return menuItem;
        }
    }
}
