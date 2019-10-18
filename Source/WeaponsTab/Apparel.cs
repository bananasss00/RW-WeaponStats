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


		public Apparel () : base ()
		{

		}

		public new void fillFromThing (Thing th)
		{
			base.fillFromThing (th);
			try {
				armorBlunt = th.GetStatValue (StatDefOf.ArmorRating_Blunt);
				armorSharp = th.GetStatValue (StatDefOf.ArmorRating_Sharp);
				armorHeat = th.GetStatValue (StatDefOf.ArmorRating_Heat);
				insulation = th.GetStatValue (StatDefOf.Insulation_Cold);
				insulationh = th.GetStatValue (StatDefOf.Insulation_Heat);
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}
		}
	}
}

