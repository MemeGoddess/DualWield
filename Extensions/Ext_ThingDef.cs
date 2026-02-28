using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tacticowl;
using Verse;

namespace DualWield
{
    public static class Ext_ThingDef
    {
        public static bool CanBeOffHand(this ThingDef td)
        {
            return td.IsWeapon && TacticowlMod.Settings.DualWield.DualWieldSelection != null && TacticowlMod.Settings.DualWield.DualWieldSelection.TryGetValue(td.defName, out Settings.Record value) && value.isSelected; 
        }
        public static bool IsTwoHand(this ThingDef td)
        {
            return td.IsWeapon && TacticowlMod.Settings.DualWield.TwoHandSelection != null && TacticowlMod.Settings.DualWield.TwoHandSelection.TryGetValue(td.defName, out Settings.Record value) && value.isSelected;
        }
    }
}
