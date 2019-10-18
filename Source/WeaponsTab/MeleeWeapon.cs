using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
	public class MeleeWeapon : Weapon
	{
		/* MELEE:
			 * th.def.statBases = MaxHitPoints, Flammability, DeteriorationRate, Beauty, SellPriceFactor, WorkToMake, Mass,
			 * 	MeleeWeapon_DamageAmount, MeleeWeapon_Cooldown
			 * th.def.weaponTags = [Melee]
			*/
		public MeleeWeapon () : base ()
		{

		}

		private float getDps ()
		{
			return (float)Math.Round (this.damage / this.cooldown, 2);
		}

		public new void fillFromThing (Thing th)
		{
			base.fillFromThing (th);
			try {
				ThingDef material = th.Stuff;
				if (material != null) {
					this.label = material.label + " " + this.label;
				}
				float tmpCldwn = 1f;
				float tmpDmg = 0f;
				bool usethis = false;

				if (th.def != null && th.def.tools != null) {
					foreach (Tool tl in th.def.tools) {
						usethis = false;
						//Log.Message(tl.label + " " + tl.power + " " + tl.cooldownTime + " " + tl.power / tl.cooldownTime + " " + tmpDmg / tmpCldwn);
						if (tmpDmg / tmpCldwn < tl.power / tl.cooldownTime) {
							this.cooldown = tl.cooldownTime;
							this.damage = tl.power;
							// tl.armorPenetration
							usethis = true;
						}
						if (usethis) {
							foreach (ToolCapacityDef tcd in tl.capacities) {
								this.damageType = tcd.label + " (" + tl.label + ")";
							}
						}
					}
				}
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}
			this.dps = (float)Math.Round (th.GetStatValue (StatDefOf.MeleeWeapon_AverageDPS), 2);
		}
	}
}
