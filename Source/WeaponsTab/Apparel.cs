using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
    public class Apparel : Weapon
    {
        public float armorBlunt { get; set; }

        public float armorSharp { get; set; }

        public float armorHeat { get; set; }

        public float insulation { get; set; }

        public float insulationh { get; set; }
        public float ceBulk { get; set; }
        public float ceWornBulk { get; set; }
        public float ceCarryWeight { get; set; }
        public float ceCarryBulk { get; set; }


        public Apparel() : base()
        {
        }

        public new void fillFromThing(Thing th, bool ce = false)
        {
            base.fillFromThing(th);
            try
            {
                armorBlunt = th.GetStatValue(StatDefOf.ArmorRating_Blunt);
                armorSharp = th.GetStatValue(StatDefOf.ArmorRating_Sharp);
                armorHeat = th.GetStatValue(StatDefOf.ArmorRating_Heat);
                insulation = th.GetStatValue(StatDefOf.Insulation_Cold);
                insulationh = th.GetStatValue(StatDefOf.Insulation_Heat);
                if (ce)
                {
                    ceBulk = th.GetStatValue(StatDef.Named("Bulk"));
                    ceWornBulk = th.GetStatValue(StatDef.Named("WornBulk"));
                    ceCarryWeight = th.GetStatValue(StatDef.Named("CarryWeight"));
                    ceCarryBulk = th.GetStatValue(StatDef.Named("CarryBulk"));
                }
            }
            catch (System.NullReferenceException e)
            {
                this.exceptions.Add(e);
            }
        }
    }
}