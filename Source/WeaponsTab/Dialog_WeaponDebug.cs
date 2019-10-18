using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
	public class Dialog_WeaponDebug : Window
	{
		private Thing thing;
		private Weapon weapon;

		public Dialog_WeaponDebug (Thing th, Weapon w)
		{
			this.doCloseX = true;
			// this.closeOnEscapeKey = true;
			this.doCloseButton = true;
			this.closeOnClickedOutside = true;
			this.thing = th;
			this.weapon = w;
		}

		public override Vector2 InitialSize {
			get {
				return new Vector2 (700, 700);
			}
		}

		protected override void SetInitialSizeAndPosition ()
		{
			base.SetInitialSizeAndPosition ();
			this.windowRect.x = (float)UI.screenWidth - this.windowRect.width;
			this.windowRect.y = (float)(UI.screenHeight - 35) - this.windowRect.height;
		}

		public override void DoWindowContents (Rect rect)
		{
			rect.yMin += 35;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			StringBuilder sb = new StringBuilder ();
			if (thing.Stuff != null) {
				sb.Append ("Stuff: ").Append (thing.Stuff.label).AppendLine ();
			}
			if (thing.def != null) {
				if (thing.def.thingCategories != null) {
					sb.Append ("--- thingCategories ---").AppendLine ();
					foreach (ThingCategoryDef tc in thing.def.thingCategories) {
						sb.Append (tc.defName).AppendLine ();
					}
				}
				if (thing.def.statBases != null) {
					sb.Append ("--- statBases ---").AppendLine ();
					foreach (StatModifier st in thing.def.statBases) {
						sb.Append (st.stat.ToString ()).Append (": ").Append (thing.GetStatValue (st.stat).ToString ()).AppendLine ();
					}
				}
				if (thing.def.weaponTags != null) {
					sb.Append ("--- weaponTags ---").AppendLine ();
					foreach (string wt in thing.def.weaponTags) {
						sb.Append (wt).AppendLine ();
					}
				}
				if (thing.def.Verbs != null) {
					sb.Append ("--- verbs ---").AppendLine ();
					ProjectileProperties proj = null;
					foreach (VerbProperties vp in thing.def.Verbs) {
						VerbCategory vcat = vp.category;
						sb.Append (vp.isPrimary ? "P" : "").Append ("+ ").Append (vp.verbClass.ToString ()).Append (" (").Append (vcat.ToString ()).Append (")").AppendLine ();
						sb.Append ("++ ").Append (vp.ToString ()).Append (": ").Append (vp.label).AppendLine ();
						if (vp.defaultProjectile != null && vp.defaultProjectile.projectile != null) {
							proj = vp.defaultProjectile.projectile;
						}
					}
					if (proj != null) {
						sb.Append ("--- projectile ---").AppendLine ();
						sb.Append ("damageDef: ").Append (proj.damageDef.label).AppendLine ();
						sb.Append ("damageAmountBase: ").Append (proj.GetDamageAmount (thing).ToString ()).AppendLine ();
						sb.Append ("speed: ").Append (proj.speed.ToString ()).AppendLine ();
						sb.Append ("explosionRadius: ").Append (proj.explosionRadius.ToString ()).AppendLine ();
					}
				}
				sb.Append ("--- exceptions ---").AppendLine ();
				foreach (Exception ex in this.weapon.exceptions) {
					sb.AppendLine (ex.Message);
					sb.AppendLine (ex.StackTrace);
					sb.AppendLine ("------");
				}
				sb.Append ("--- stats ---").AppendLine ();
				sb.AppendLine ("MeleeDPS: " + thing.GetStatValue (StatDefOf.MeleeDPS));
				sb.AppendLine ("AverageDPS: " + thing.GetStatValue (StatDefOf.MeleeWeapon_AverageDPS));
				sb.AppendLine ("CooldownMultiplier: " + thing.GetStatValue (StatDefOf.MeleeWeapon_CooldownMultiplier));
				sb.AppendLine ("RngCooldown: " + thing.GetStatValue (StatDefOf.RangedWeapon_Cooldown));
			}

			GUI.BeginGroup (rect);
			Rect tmpRec = new Rect (0, 0, 350, 700);
			Widgets.Label (tmpRec, sb.ToString ());

			StringBuilder sb2 = new StringBuilder ();
			if (thing.def.tools != null) {
				sb2.Append ("--- tools ---").AppendLine ();
				foreach (Tool tl in thing.def.tools) {
					sb2.Append (tl.label).Append (": ").AppendLine ();
					sb2.Append ("power: ").Append (tl.power).AppendLine ();
					sb2.Append ("cooldown: ").Append (tl.cooldownTime).AppendLine ();
					sb2.Append ("armor pen.: ").Append (tl.armorPenetration).AppendLine ();
					sb2.Append ("capacities: ");
					foreach (ToolCapacityDef tcd in tl.capacities) {
						sb2.Append (tcd.label).Append (", ");
					}
					sb2.AppendLine ().Append ("---").AppendLine ();
				}
			}


			Rect tmpRec2 = new Rect (350, 0, 350, 700);
			Widgets.Label (tmpRec2, sb2.ToString ());
			GUI.EndGroup ();
		}
	}
}

