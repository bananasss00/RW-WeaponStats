using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
	public class OtherWeapon : Weapon
	{
		public OtherWeapon () : base ()
		{
		}

		public new void fillFromThing (Thing th, bool ce = false)
		{
			base.fillFromThing (th);
			try {
				mass = th.GetStatValue (StatDefOf.Mass);
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}
		}
	}
}

