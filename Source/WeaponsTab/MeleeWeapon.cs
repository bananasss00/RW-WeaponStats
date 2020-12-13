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

		private List<WeaponPartTool> tools;
		
		public float ceCounterParry { get; set; }
		public MeleeWeapon () : base ()
		{

		}

		private float getDps ()
		{
			return (float)Math.Round (this.damage / this.cooldown, 2);
		}

		public new void fillFromThing (Thing th, bool ce = false)
		{
			base.fillFromThing (th);
			tools = new List<WeaponPartTool>();
			try {
				ThingDef material = th.Stuff;
				if (material != null) {
					this.label = material.label + " " + this.label;
				}
				float tmpCldwn = 1f;
				float tmpDmg = 0f;
				bool usethis = false;
				if (ce)
				{
					armorPenetration = th.GetStatValue(StatDef.Named("MeleePenetrationFactor"));
					ceCounterParry = th.GetStatValue(StatDef.Named("MeleeCounterParryBonus"));
				}
				else
				{
					armorPenetration = th.GetStatValue(StatDefOf.MeleeWeapon_AverageArmorPenetration);
				}

				if (th.def != null && th.def.tools != null) {
					WeaponPartTool tmptool;
					foreach (Tool tl in th.def.tools)
					{
						tmptool = new WeaponPartTool();
						tmptool.fillFromTool(tl, ce);
						this.tools.Add(tmptool);
						usethis = false;
						if (tmpDmg / tmpCldwn < tl.power / tl.cooldownTime) {
							this.cooldown = tl.cooldownTime;
							this.damage = tl.power;
							usethis = true;
						}
						
						if (usethis) {
							foreach (ToolCapacityDef tcd in tl.capacities) {
								this.damageType = tcd.label + " (" + tl.label + ")";
							}
						}
					}
				}
				this.dps = (float)Math.Round (th.GetStatValue (StatDefOf.MeleeWeapon_AverageDPS), 2);
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}
		}
	}
}
