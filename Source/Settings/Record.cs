using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace DualWield.Settings
{
    public class Record : IExposable
    {
        public bool isSelected = false;
        public String label = "";
        public int extraRotation = 0;
        public Record()
        {

        }
        public Record(bool isSelected, String label)
        {
            this.isSelected = isSelected;
            this.label = label;
        }
        public override string ToString()
        {
            return this.isSelected + "," + this.label;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref isSelected, nameof(isSelected), false);
            Scribe_Values.Look(ref label, nameof(label), string.Empty);
            Scribe_Values.Look(ref extraRotation, nameof(extraRotation), 0);
        }
    }

}
