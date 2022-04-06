using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
    public class MainTabWindow_WeaponStats : MainTabWindow
    {
        private bool weaponStorageLoaded = false;
        private bool outfitStorageLoaded = false;
        private bool ce = false;

        private const int ROW_HEIGHT = 30;
        private const int PAWN_WIDTH = 20;
        private const int STAT_WIDTH = 60;
        private const int ICON_WIDTH = 29;
        private const int GOTO_WIDTH = 0;
        private const int LABEL_WIDTH = 250;
        private const int QUALITY_WIDTH = STAT_WIDTH;
        private const int ACCURACY_WIDTH = 2 * STAT_WIDTH;
        private const int DTYPE_WIDTH = 3 * STAT_WIDTH;

        private Assembly weaponStorageAssembly = null;
        private Assembly changeDresserAssembly = null;

        private struct colDef
        {
            public string label;
            public string property;

            public colDef(string l, string p)
            {
                label = l;
                property = p;
            }
        }

        private string sortProperty;
        private string sortOrder;

        private bool showGround = true;
        private bool showEquipped = true;
        private bool showEquippedP = false;
        private bool showEquippedH = false;
        private bool showEquippedF = false;
        private bool showEquippedC = false;
        private bool showCraftable = false;
        private bool showStorage = false;

        private bool accRbAll = true;
        private bool accRbTouch = false;
        private bool accRbShort = false;
        private bool accRbMedium = false;
        private bool accRbLong = false;


        private bool isDirty = true;
        private int listUpdateNext = 0;

        public Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight;
        private float tableHeight;

        private TemperatureDisplayMode tdm;
        private string tempUnit;
        private float tempCoeff;

        private enum WeaponsTab : byte
        {
            None,
            Ranged,
            Melee,
            Grenades,
            Other,
            Apparel,
            Tools
        }

        private WeaponsTab curTab = WeaponsTab.Ranged;

        List<RangedWeapon> rangedList;
        List<MeleeWeapon> meleeList;
        List<GrenadeWeapon> grenadeList;
        List<OtherWeapon> otherList;
        List<Apparel> apparelList;
        List<ToolWeapon> toolList;

        private int rangedCount = 0;
        private int meleeCount = 0;
        private int grenadeCount = 0;
        private int otherCount = 0;
        private int apparelCount = 0;
        private int toolCount = 0;

        public MainTabWindow_WeaponStats()
        {
            this.doCloseX = true;
            // this.closeOnEscapeKey = true;
            this.doCloseButton = false;
            this.closeOnClickedOutside = true;
            this.sortProperty = "marketValue";
            this.sortOrder = "DESC";
            this.tdm = Prefs.TemperatureMode;
            weaponStorageLoaded = true;
            outfitStorageLoaded = true;

            if (ModsConfig.ActiveModsInLoadOrder.Any(m => "Combat Extended".Equals(m.Name)))
            {
                ce = true;
            }

            if (weaponStorageLoaded || outfitStorageLoaded)
            {
                foreach (var pack in LoadedModManager.RunningMods)
                {
                    foreach (var assembly in pack.assemblies.loadedAssemblies)
                    {
                        if (assembly.GetName().Name.Equals("WeaponStorage"))
                        {
                            weaponStorageAssembly = assembly;
                        }
                        else if (assembly.GetName().Name.Equals("ChangeDresser"))
                        {
                            changeDresserAssembly = assembly;
                        }
                    }
                }
            }
        }

        private int GetStartingWidth()
        {
            return PAWN_WIDTH + 2 + ICON_WIDTH + LABEL_WIDTH;
        }

        private void DoRangedPage(Rect rect, int count, List<RangedWeapon> rangedList)
        {
            colDef[] rangedHeaders = { };
            if (this.ce)
            {
                rangedHeaders = new colDef[]
                {
                    new colDef("Quality", "qualityNum"),
                    new colDef("HP", "hp"),
                    new colDef("DPS", "dps"),
                    new colDef("Value", "marketValue"),
                    new colDef("Range", "maxRange"),
                    new colDef("Cooldwn", "cooldown"),
                    new colDef("Warmup", "warmup"),
                    new colDef("Sights", "ceSightsEfficiency"),
                    new colDef("Spread", "ceShotSpread"),
                    new colDef("Sway", "ceSwayFactor"),
                    new colDef("MagazineCapacity", "ceMagazineCapacity"),
                    // new colDef("Bulk", "ceBulk"),
                };
            }
            else
            {
                rangedHeaders = new colDef[]
                {
                    new colDef("Quality", "qualityNum"),
                    new colDef("HP", "hp"),
                    new colDef("DPS", "dps"),
                    new colDef("DPSA", "dpsa"),
                    new colDef("Value", "marketValue"),
                    new colDef("Damage", "damage"),
                    new colDef("ArmorPenetration", "armorPenetration"),
                    new colDef("Range", "maxRange"),
                    new colDef("Cooldwn", "cooldown"),
                    new colDef("Warmup", "warmup")
                };
            }

            // accuracy radio buttons
            rect.y += 30;
            if (!this.ce)
            {
                float currentX = rect.x;
                var label = "WeaponStats.ColRange".Translate() + ": ";
                var lsize = Text.CalcSize(label).x;
                Widgets.Label(new Rect(currentX, rect.y + 3, lsize, 30), label);
                currentX += lsize;

                PrintAutoRadio("WeaponStats.AccRbName.All".Translate(), ref accRbAll, ref currentX, ref rect);
                PrintAutoRadio("WeaponStats.AccRbName.Touch".Translate(), ref accRbTouch, ref currentX, ref rect);
                PrintAutoRadio("WeaponStats.AccRbName.Short".Translate(), ref accRbShort, ref currentX, ref rect);
                PrintAutoRadio("WeaponStats.AccRbName.Medium".Translate(), ref accRbMedium, ref currentX, ref rect);
                PrintAutoRadio("WeaponStats.AccRbName.Long".Translate(), ref accRbLong, ref currentX, ref rect);
            }

            // end of accuracy rb
            rect.y += 35;
            GUI.BeginGroup(rect);
            tableHeight = count * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 10, tableHeight + 100);
            int num = 0;
            int ww = GetStartingWidth();
            DrawCommon(-1, inRect.width);
            foreach (colDef h in rangedHeaders)
            {
                if (h.property == "dpsa")
                {
                    if (accRbAll) printCellSort(WeaponsTab.Ranged, "dpsa", "WeaponStats.ColDPSA".Translate(), ww, ACCURACY_WIDTH);
                    else if (accRbTouch) printCellSort(WeaponsTab.Ranged, "dpsaTouch", "WeaponStats.ColDPSA".Translate(), ww, ACCURACY_WIDTH);
                    else if (accRbShort) printCellSort(WeaponsTab.Ranged, "dpsaShort", "WeaponStats.ColDPSA".Translate(), ww, ACCURACY_WIDTH);
                    else if (accRbMedium) printCellSort(WeaponsTab.Ranged, "dpsaMedium", "WeaponStats.ColDPSA".Translate(), ww, ACCURACY_WIDTH);
                    else if (accRbLong) printCellSort(WeaponsTab.Ranged, "dpsaLong", "WeaponStats.ColDPSA".Translate(), ww, ACCURACY_WIDTH);
                }
                else
                {
                    printCellSort(WeaponsTab.Ranged, h.property, ("WeaponStats.Col" + h.label).Translate(), ww);
                }

                ww += STAT_WIDTH;
            }

            if (!this.ce)
            {
                if (accRbAll) printCellHeader(WeaponsTab.Ranged, "accuracy", "WeaponStats.ColAccuracy".Translate(), ww, ACCURACY_WIDTH);
                else if (accRbTouch) printCellSort(WeaponsTab.Ranged, "accuracyTouch", "WeaponStats.ColAccuracy".Translate(), ww, ACCURACY_WIDTH);
                else if (accRbShort) printCellSort(WeaponsTab.Ranged, "accuracyShort", "WeaponStats.ColAccuracy".Translate(), ww, ACCURACY_WIDTH);
                else if (accRbMedium) printCellSort(WeaponsTab.Ranged, "accuracyMedium", "WeaponStats.ColAccuracy".Translate(), ww, ACCURACY_WIDTH);
                else if (accRbLong) printCellSort(WeaponsTab.Ranged, "accuracyLong", "WeaponStats.ColAccuracy".Translate(), ww, ACCURACY_WIDTH);
                ww += ACCURACY_WIDTH;
                //printCellHeader(WeaponsTab.Ranged, "dmgType", "WeaponStats.ColType".Translate(), ww, DTYPE_WIDTH);
                ww += DTYPE_WIDTH;
            }

            rect.y -= 35;
            Rect scrollRect = new Rect(rect.x, rect.y - 35, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (RangedWeapon rng in rangedList)
            {
                DrawRangedRow(rng, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DoMeleePage(Rect rect, int count, List<MeleeWeapon> meleeList)
        {
            colDef[] meleeHeaders = { };
            if (this.ce)
            {
                meleeHeaders = new colDef[]
                {
                    new colDef("Quality", "qualityNum"),
                    new colDef("HP", "hp"),
                    new colDef("DPS", "dps"),
                    new colDef("Value", "marketValue"),
                    new colDef("Damage", "damage"),
                    new colDef("ArmorPenetration", "armorPenetration"),
                    new colDef("Cooldwn", "cooldown"),
                    new colDef("CounterParry", "ceCounterParry")
                };
            }
            else
            {
                meleeHeaders = new colDef[]
                {
                    new colDef("Quality", "qualityNum"),
                    new colDef("HP", "hp"),
                    new colDef("DPS", "dps"),
                    new colDef("Value", "marketValue"),
                    new colDef("Damage", "damage"),
                    new colDef("ArmorPenetration", "armorPenetration"),
                    new colDef("Cooldwn", "cooldown")
                };
            }

            rect.y += 30;
            GUI.BeginGroup(rect);
            tableHeight = count * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 10, tableHeight + 100);
            int num = 0;
            int ww = GetStartingWidth();
            DrawCommon(-1, inRect.width);
            foreach (colDef h in meleeHeaders)
            {
                printCellSort(WeaponsTab.Melee, h.property, ("WeaponStats.Col" + h.label).Translate(), ww);
                ww += STAT_WIDTH;
            }

            printCell("WeaponStats.ColType".Translate(), num, ww, DTYPE_WIDTH);
            ww += DTYPE_WIDTH;
            Rect scrollRect = new Rect(rect.x, rect.y - 35, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (MeleeWeapon ml in meleeList)
            {
                DrawMeleeRow(ml, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DoGrenadePage(Rect rect, int count, List<GrenadeWeapon> grenadeList)
        {
            colDef[] grenadeHeaders =
            {
                new colDef("Quality", "qualityNum"),
                new colDef("HP", "hp"),
                new colDef("Value", "marketValue"),
                new colDef("Damage", "damage"),
                new colDef("ArmorPenetration", "armorPenetration"),
                new colDef("Range", "maxRange"),
                new colDef("Cooldwn", "cooldown"),
                new colDef("Warmup", "warmup"),
                new colDef("Radius", "explosionRadius"),
                new colDef("Delay", "explosionDelay")
            };
            rect.y += 30;
            GUI.BeginGroup(rect);
            tableHeight = count * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 10, tableHeight + 100);
            int num = 0;
            int ww = GetStartingWidth();
            DrawCommon(-1, inRect.width);
            foreach (colDef h in grenadeHeaders)
            {
                printCellSort(WeaponsTab.Grenades, h.property, ("WeaponStats.Col" + h.label).Translate(), ww);
                ww += STAT_WIDTH;
            }

            printCell("WeaponStats.ColType".Translate(), num, ww, DTYPE_WIDTH);
            ww += DTYPE_WIDTH;
            Rect scrollRect = new Rect(rect.x, rect.y - 35, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (GrenadeWeapon g in grenadeList)
            {
                DrawGrenadeRow(g, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DoOtherPage(Rect rect, int count, List<OtherWeapon> otherList)
        {
            colDef[] otherHeaders =
            {
                new colDef("HP", "hp"),
                new colDef("Value", "marketValue")
            };
            rect.y += 30;
            GUI.BeginGroup(rect);
            tableHeight = count * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 10, tableHeight + 100);
            int num = 0;
            int ww = GetStartingWidth();
            DrawCommon(-1, inRect.width);
            foreach (colDef h in otherHeaders)
            {
                printCellSort(WeaponsTab.Other, h.property, ("WeaponStats.Col" + h.label).Translate(), ww);
                ww += STAT_WIDTH;
            }

            Rect scrollRect = new Rect(rect.x, rect.y - 35, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (OtherWeapon o in otherList)
            {
                DrawOtherRow(o, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DoApparelPage(Rect rect, int count, List<Apparel> apparelList)
        {
            colDef[] apparelHeaders =
            {
                new colDef("Quality", "qualityNum"),
                new colDef("HP", "hp"),
                new colDef("Value", "marketValue"),
                new colDef("Blunt", "armorBlunt"),
                new colDef("Sharp", "armorSharp"),
                new colDef("Heat", "armorHeat"),
                new colDef("ICold", "insulation"),
                new colDef("IHeat", "insulationh"),
                new colDef ("Work", "workSpeed"),
                new colDef ("Learn", "learnFactor")
            };
            rect.y += 30;
            GUI.BeginGroup(rect);
            tableHeight = count * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 10, tableHeight + 100);
            int num = 0;
            int ww = GetStartingWidth();
            DrawCommon(-1, inRect.width);
            foreach (colDef h in apparelHeaders)
            {
                printCellSort(WeaponsTab.Apparel, h.property, ("WeaponStats.Col" + h.label).Translate(), ww);
                ww += STAT_WIDTH;
            }

            Rect scrollRect = new Rect(rect.x, rect.y - 35, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (Apparel a in apparelList)
            {
                DrawApparelRow(a, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DoToolsPage(Rect rect, int count, List<ToolWeapon> toolList)
        {
            colDef[] toolHeaders =
            {
                new colDef("HP", "hp"),
                new colDef("Value", "marketValue")
            };
            rect.y += 30;
            GUI.BeginGroup(rect);
            tableHeight = count * ROW_HEIGHT;
            Rect inRect = new Rect(0, 0, rect.width - 10, tableHeight + 100);
            int num = 0;
            int ww = GetStartingWidth();
            DrawCommon(-1, inRect.width);
            foreach (colDef h in toolHeaders)
            {
                printCellSort(WeaponsTab.Tools, h.property, h.label, ww);
                ww += STAT_WIDTH;
            }

            Rect scrollRect = new Rect(rect.x, rect.y - 35, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, inRect);
            foreach (ToolWeapon t in toolList)
            {
                DrawToolRow(t, num, inRect.width);
                num++;
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawCommon(int num, float w)
        {
            int fnum = num;
            var height = 0;
            if (num == -1)
            {
                fnum = 0;
                height = ROW_HEIGHT;
            }
            else
            {
                height = ROW_HEIGHT;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0, height * (fnum + 1), w);
            GUI.color = Color.white;
            Rect rowRect = new Rect(0, height * num, w, height);
            if (num > -1)
            {
                if (Mouse.IsOver(rowRect))
                {
                    GUI.DrawTexture(rowRect, TexUI.HighlightTex);
                }
            }
        }

        private int DrawCommonButtons(int x, int rowNum, float rowWidth, Thing t, Weapon w)
        {
            if (Prefs.DevMode)
            {
                Rect tmpRec = new Rect(rowWidth - 20, ROW_HEIGHT * rowNum, 20, ROW_HEIGHT);
                if (Widgets.ButtonText(tmpRec, "D"))
                {
                    Dialog_WeaponDebug dlg = new Dialog_WeaponDebug(t, w);
                    Find.WindowStack.Add(dlg);
                }
            }
            else
            {
                Widgets.InfoCardButton(rowWidth - 20, ROW_HEIGHT * rowNum, t);
            }

            if (w.craftable)
            {
                if (Widgets.ButtonInvisible(new Rect(20, ROW_HEIGHT * rowNum, rowWidth, ROW_HEIGHT)))
                {
                    RimWorld.Planet.GlobalTargetInfo gti = new RimWorld.Planet.GlobalTargetInfo(w.cratablePos);
                    CameraJumper.TryJumpAndSelect(gti);
                }

                Texture2D pawnIcon = ContentFinder<Texture2D>.Get("UI/Icons/Craftable", true);
                Rect p = new Rect(0, ROW_HEIGHT * rowNum + (ROW_HEIGHT - pawnIcon.height) / 2,
                    (float) pawnIcon.width, (float) pawnIcon.height);
                GUI.DrawTexture(p, pawnIcon);
                TooltipHandler.TipRegion(p, w.cratablePos.def.label);
            }
            else if (w.inStorage)
            {
                if (Widgets.ButtonInvisible(new Rect(20, ROW_HEIGHT * rowNum, rowWidth, ROW_HEIGHT)))
                {
                    RimWorld.Planet.GlobalTargetInfo gti = new RimWorld.Planet.GlobalTargetInfo(w.storagePos, Find.CurrentMap);
                    CameraJumper.TryJumpAndSelect(gti);
                }

                Texture2D storIcon = ContentFinder<Texture2D>.Get("UI/Icons/Storage", true);
                Rect p = new Rect(0, ROW_HEIGHT * rowNum + (ROW_HEIGHT - storIcon.height) / 2,
                    (float) storIcon.width, (float) storIcon.height);
                GUI.DrawTexture(p, storIcon);
            }
            else
            {
                bool isSelected = Find.Selector.IsSelected(t);
                Rect rowRect = new Rect(20, ROW_HEIGHT * rowNum, rowWidth, ROW_HEIGHT);
                if (isSelected)
                {
                    var backupColor = GUI.color;
                    GUI.color = Color.yellow;
                    Widgets.DrawHighlight(rowRect);
                    GUI.color = backupColor;
                }
                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (Event.current.shift)
                    {
                        if (t.Map == null)
                            ;
                        else if (isSelected)
                            Find.Selector.Deselect(t);
                        else
                            Find.Selector.Select(t);
                    }
                    else
                    {
                        RimWorld.Planet.GlobalTargetInfo gti = new RimWorld.Planet.GlobalTargetInfo(t);
                        CameraJumper.TryJumpAndSelect(gti);
                    }
                }

                if (w.pawn != null)
                {
                    Texture2D pawnIcon;
                    if (w.pawnType == "prisoner")
                    {
                        pawnIcon = ContentFinder<Texture2D>.Get("UI/Icons/Prisoner", true);
                    }
                    else if (w.pawnType == "hostile")
                    {
                        pawnIcon = ContentFinder<Texture2D>.Get("UI/Icons/Hostile", true);
                    }
                    else if (w.pawnType == "friendly")
                    {
                        pawnIcon = ContentFinder<Texture2D>.Get("UI/Icons/Friendly", true);
                    }
                    else if (w.pawnType == "corpse")
                    {
                        pawnIcon = ContentFinder<Texture2D>.Get("UI/Icons/Corpse", true);
                    }
                    else
                    {
                        pawnIcon = ContentFinder<Texture2D>.Get("UI/Icons/Colonist", true);
                    }

                    Rect p = new Rect(0, ROW_HEIGHT * rowNum + (ROW_HEIGHT - pawnIcon.height) / 2,
                        (float) pawnIcon.width, (float) pawnIcon.height);
                    GUI.DrawTexture(p, pawnIcon);
                    TooltipHandler.TipRegion(p, w.pawn);
                }
            }

            Rect icoRect = new Rect(PAWN_WIDTH + 2, ROW_HEIGHT * rowNum, ICON_WIDTH, ICON_WIDTH);
            Widgets.ThingIcon(icoRect, w.thing);
            return PAWN_WIDTH + ICON_WIDTH + 2;
        }

        private void DrawRangedRow(RangedWeapon t, int num, float w)
        {
            /*
             * new colDef("Quality", "qualityNum"),
                    new colDef("HP", "hp"),
                    new colDef("Value", "marketValue"),
                    new colDef("Range", "maxRange"),
                    new colDef("Cooldwn", "cooldown"),
                    new colDef("Warmup", "warmup"),
                    new colDef("Sights", "ceSightsEfficiency"),
                    new colDef("Spread", "ceShotSpread"),
                    new colDef("Sway", "ceSwayFactor"),
                    new colDef("Bulk", "ceBulk"),
             */
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawCommonButtons(ww, num, w, t.thing, t);
            printCell(t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.quality, num, ww, QUALITY_WIDTH);
            ww += QUALITY_WIDTH;
            printCell(t.getHpStr(), num, ww);
            ww += STAT_WIDTH;
            if (this.ce)
            {
                printCell(t.dps, num, ww);
                ww += STAT_WIDTH;
                printCell(Math.Round(t.marketValue, 1), num, ww);
                ww += STAT_WIDTH;
                printCell(t.maxRange, num, ww);
                ww += STAT_WIDTH;
                printCell(t.cooldown, num, ww);
                ww += STAT_WIDTH;
                printCell(t.warmup, num, ww);
                ww += STAT_WIDTH;
                printCell(Math.Round(t.ceSightsEfficiency * 100, 1), num, ww, STAT_WIDTH, "%");
                ww += STAT_WIDTH;
                printCell(t.ceShotSpread, num, ww);
                ww += STAT_WIDTH;
                printCell(t.ceSwayFactor, num, ww);
                ww += STAT_WIDTH;
                printCell(t.ceMagazineCapacity, num, ww);
                ww += STAT_WIDTH;
                // printCell(t.ceBulk, num, ww);
                // ww += STAT_WIDTH;
            }
            else
            {
                printCell(t.dps, num, ww);
                ww += STAT_WIDTH;
                if (accRbAll) printCell(t.dpsa, num, ww);
                else if (accRbTouch) printCell(t.dpsaTouch, num, ww);
                else if (accRbShort) printCell(t.dpsaShort, num, ww);
                else if (accRbMedium) printCell(t.dpsaMedium, num, ww);
                else if (accRbLong) printCell(t.dpsaLong, num, ww);
                ww += STAT_WIDTH;
                printCell(Math.Round(t.marketValue, 1), num, ww);
                ww += STAT_WIDTH;
                printCell(Math.Round(t.damage, 2), num, ww);
                ww += STAT_WIDTH;
                printCell(Math.Round(t.armorPenetration * 100, 1), num, ww);
                ww += STAT_WIDTH;
                printCell(t.maxRange, num, ww);
                ww += STAT_WIDTH;
                printCell(t.cooldown, num, ww);
                ww += STAT_WIDTH;
                printCell(t.warmup, num, ww);
                ww += STAT_WIDTH;
                if (accRbAll) printCell(t.getAccuracyStr(), num, ww, ACCURACY_WIDTH);
                else if (accRbTouch) printCell(t.accuracyTouch, num, ww, ACCURACY_WIDTH);
                else if (accRbShort) printCell(t.accuracyShort, num, ww, ACCURACY_WIDTH);
                else if (accRbMedium) printCell(t.accuracyMedium, num, ww, ACCURACY_WIDTH);
                else if (accRbLong) printCell(t.accuracyLong, num, ww, ACCURACY_WIDTH);
                ww += ACCURACY_WIDTH;
                printCell(t.damageType, num, ww, DTYPE_WIDTH);
                ww += DTYPE_WIDTH;
            }
        }

        private void DrawMeleeRow(MeleeWeapon t, int num, float w)
        {
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawCommonButtons(ww, num, w, t.thing, t);
            printCell(t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.quality, num, ww, QUALITY_WIDTH);
            ww += QUALITY_WIDTH;
            printCell(t.getHpStr(), num, ww);
            ww += STAT_WIDTH;
            printCell(t.dps, num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.marketValue, 1), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.damage, 2), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.armorPenetration * 100, 1), num, ww);
            ww += STAT_WIDTH;
            printCell(t.cooldown, num, ww);
            ww += STAT_WIDTH;
            if (this.ce)
            {
                printCell(t.ceCounterParry, num, ww);
                ww += STAT_WIDTH;
            }

            printCell(t.damageType, num, ww, DTYPE_WIDTH);
            ww += DTYPE_WIDTH;
        }

        private void DrawGrenadeRow(GrenadeWeapon t, int num, float w)
        {
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawCommonButtons(ww, num, w, t.thing, t);
            printCell(t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.quality, num, ww, QUALITY_WIDTH);
            ww += QUALITY_WIDTH;
            printCell(t.getHpStr(), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.marketValue, 1), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.damage, 2), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.armorPenetration * 100, 1), num, ww);
            ww += STAT_WIDTH;
            printCell(t.maxRange, num, ww);
            ww += STAT_WIDTH;
            printCell(t.cooldown, num, ww);
            ww += STAT_WIDTH;
            printCell(t.warmup, num, ww);
            ww += STAT_WIDTH;
            printCell(t.explosionRadius, num, ww);
            ww += STAT_WIDTH;
            printCell(t.explosionDelay, num, ww);
            ww += STAT_WIDTH;
            printCell(t.damageType, num, ww, DTYPE_WIDTH);
            ww += DTYPE_WIDTH;
        }

        private void DrawOtherRow(OtherWeapon t, int num, float w)
        {
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawCommonButtons(ww, num, w, t.thing, t);
            printCell(t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.getHpStr(), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.marketValue, 1), num, ww);
            ww += STAT_WIDTH;
        }

        private void DrawApparelRow(Apparel t, int num, float w)
        {
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawCommonButtons(ww, num, w, t.thing, t);
            printCell(/*t.stuff + " " + */t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.quality, num, ww, QUALITY_WIDTH);
            ww += QUALITY_WIDTH;
            printCell(t.getHpStr(), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.marketValue, 1), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(this.ce ? t.armorBlunt : t.armorBlunt * 100, 1), num, ww, STAT_WIDTH, "%");
            ww += STAT_WIDTH;
            printCell(Math.Round(this.ce ? t.armorSharp : t.armorSharp * 100, 1), num, ww, STAT_WIDTH, "%");
            ww += STAT_WIDTH;
            printCell(Math.Round(t.armorHeat * 100, 1), num, ww, STAT_WIDTH, "%");
            ww += STAT_WIDTH;
            printCell(Math.Round(t.insulation * this.tempCoeff, 1), num, ww, STAT_WIDTH, this.tempUnit);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.insulationh * this.tempCoeff, 1), num, ww, STAT_WIDTH, this.tempUnit);
            ww += STAT_WIDTH;
            printCell (Math.Round(t.workSpeed * 100, 1), num, ww, STAT_WIDTH, "%");
            ww += STAT_WIDTH;
            printCell (Math.Round(t.learnFactor * 100, 1), num, ww, STAT_WIDTH, "%");
            ww += STAT_WIDTH;
        }

        private void DrawToolRow(ToolWeapon t, int num, float w)
        {
            DrawCommon(num, w);
            int ww = 0;
            ww = this.DrawCommonButtons(ww, num, w, t.thing, t);
            printCell(t.label, num, ww, LABEL_WIDTH);
            ww += LABEL_WIDTH;
            printCell(t.getHpStr(), num, ww);
            ww += STAT_WIDTH;
            printCell(Math.Round(t.marketValue, 1), num, ww);
            ww += STAT_WIDTH;
        }

        private void printCell(string content, int rowNum, int x, int width = STAT_WIDTH, string tooltip = "")
        {
            Rect tmpRec = new Rect(x, ROW_HEIGHT * rowNum + 3, width, ROW_HEIGHT - 3);
            Widgets.Label(tmpRec, content);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(tmpRec, tooltip);
            }
        }

        private void printCellHeader(WeaponsTab page, string property, string content, int x, int width = STAT_WIDTH)
        {
            Rect tmpRec = new Rect(x, 2, 25, 25);
            GUI.DrawTexture(tmpRec, ContentFinder<Texture2D>.Get("UI/Icons/Wsh_" + property));
            TooltipHandler.TipRegion(tmpRec, content);
            if (Mouse.IsOver(tmpRec))
            {
                GUI.DrawTexture(tmpRec, TexUI.HighlightTex);
            }
        }

        private void printCellSort(WeaponsTab page, string sortProperty, string content, int x, int width = STAT_WIDTH)
        {
            Rect tmpRec = new Rect(x, 2, 25, 25);
            GUI.DrawTexture(tmpRec, ContentFinder<Texture2D>.Get("UI/Icons/Wsh_" + sortProperty));
            TooltipHandler.TipRegion(tmpRec, content);
            if (Mouse.IsOver(tmpRec))
            {
                GUI.DrawTexture(tmpRec, TexUI.HighlightTex);
            }

            if (Widgets.ButtonInvisible(tmpRec))
            {
                if (this.sortProperty == sortProperty)
                {
                    this.sortOrder = this.sortOrder == "ASC" ? "DESC" : "ASC";
                }
                else
                {
                    this.sortProperty = sortProperty;
                }

                this.isDirty = true;
            }

            if (this.sortProperty == sortProperty)
            {
                Texture2D texture2D = (this.sortOrder == "ASC")
                    ? ContentFinder<Texture2D>.Get("UI/Icons/Sorting", true)
                    : ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending", true);
                Rect p = new Rect(tmpRec.xMax - (float) texture2D.width - 30, tmpRec.yMax - (float) texture2D.height - 1, (float) texture2D.width,
                    (float) texture2D.height);
                GUI.DrawTexture(p, texture2D);
            }
        }

        private void printCell(float content, int rowNum, int x, int width = STAT_WIDTH)
        {
            printCell(content.ToString(), rowNum, x, width);
        }

        private void printCell(double content, int rowNum, int x, int width = STAT_WIDTH, string unit = "")
        {
            printCell(content.ToString() + unit, rowNum, x, width);
        }

        private WeaponsTab getAppropriateTab(Thing th)
        {
            try
            {
                if (th.def.IsApparel) return WeaponsTab.Apparel;
                if (th.def.IsRangedWeapon)
                {
                    bool IsGrenade = false;
                    foreach (ThingCategoryDef tc in th.def.thingCategories)
                    {
                        if (tc.defName == "Grenades")
                        {
                            IsGrenade = true;
                            break;
                        }
                    }

                    return IsGrenade ? WeaponsTab.Grenades : WeaponsTab.Ranged;
                }
                else if (th.def.IsMeleeWeapon && !th.def.IsStuff && !th.def.IsIngestible && !th.def.IsDrug)
                {
                    return WeaponsTab.Melee;
                }
                else
                {
                    return WeaponsTab.Other;
                }
            }
            catch (System.NullReferenceException e)
            {
                return WeaponsTab.Other;
            }
        }

        public IEnumerable<Weapon> UpdateListWithModdedContainers()
        {
            return new List<Weapon>();
        }

        private void UpdateListSorting()
        {
            System.Reflection.PropertyInfo pir;
            pir = typeof(RangedWeapon).GetProperty(this.sortProperty);
            if (pir != null)
            {
                rangedList = this.sortOrder == "DESC"
                    ? rangedList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : rangedList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }

            pir = typeof(MeleeWeapon).GetProperty(this.sortProperty);
            if (pir != null)
            {
                meleeList = this.sortOrder == "DESC"
                    ? meleeList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : meleeList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }

            pir = typeof(GrenadeWeapon).GetProperty(this.sortProperty);
            if (pir != null)
            {
                grenadeList = this.sortOrder == "DESC"
                    ? grenadeList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : grenadeList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }

            pir = typeof(OtherWeapon).GetProperty(this.sortProperty);
            if (pir != null)
            {
                otherList = this.sortOrder == "DESC"
                    ? otherList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : otherList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }

            pir = typeof(ToolWeapon).GetProperty(this.sortProperty);
            if (pir != null)
            {
                toolList = this.sortOrder == "DESC"
                    ? toolList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : toolList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }

            pir = typeof(Apparel).GetProperty(this.sortProperty);
            if (pir != null)
            {
                apparelList = this.sortOrder == "DESC"
                    ? apparelList.OrderByDescending(o => pir.GetValue(o, null)).ToList()
                    : apparelList.OrderBy(o => pir.GetValue(o, null)).ToList();
            }
        }

        private void UpdateList()
        {
            if (Prefs.DevMode)
            {
                Log.Message("Update weapons list");
            }

            rangedList = new List<RangedWeapon>();
            meleeList = new List<MeleeWeapon>();
            grenadeList = new List<GrenadeWeapon>();
            otherList = new List<OtherWeapon>();
            apparelList = new List<Apparel>();
            toolList = new List<ToolWeapon>();

            rangedCount = 0;
            meleeCount = 0;
            grenadeCount = 0;
            otherCount = 0;
            apparelCount = 0;
            toolCount = 0;

            RangedWeapon tmpRanged;
            MeleeWeapon tmpMelee;
            GrenadeWeapon tmpGrenade;
            OtherWeapon tmpOther;
            Apparel tmpApparel;
            ToolWeapon tmpTool;
            WeaponsTab tb;

            Lister.Update();

            if (this.showGround)
            {
                foreach (Thing th in Lister.Weapons())
                {
                    if (!th.Position.Fogged(Find.CurrentMap))
                    {
                        tb = this.getAppropriateTab(th);
                        switch (tb)
                        {
                            case WeaponsTab.Ranged:
                                tmpRanged = new RangedWeapon();
                                tmpRanged.fillFromThing(th, this.ce);
                                rangedList.Add(tmpRanged);
                                rangedCount++;
                                break;
                            case WeaponsTab.Melee:
                                tmpMelee = new MeleeWeapon();
                                tmpMelee.fillFromThing(th, this.ce);
                                meleeList.Add(tmpMelee);
                                meleeCount++;
                                break;
                            case WeaponsTab.Grenades:
                                tmpGrenade = new GrenadeWeapon();
                                tmpGrenade.fillFromThing(th, this.ce);
                                grenadeList.Add(tmpGrenade);
                                grenadeCount++;
                                break;
                            case WeaponsTab.Tools:
                                tmpTool = new ToolWeapon();
                                tmpTool.fillFromThing(th, this.ce);
                                toolList.Add(tmpTool);
                                toolCount++;
                                break;
                            case WeaponsTab.Other:
                                tmpOther = new OtherWeapon();
                                tmpOther.fillFromThing(th, this.ce);
                                otherList.Add(tmpOther);
                                otherCount++;
                                break;
                        }
                    }
                }

                foreach (Thing th in Lister.Apparels())
                {
                    if (!th.Position.Fogged(Find.CurrentMap))
                    {
                        tmpApparel = new Apparel();
                        tmpApparel.fillFromThing(th, this.ce);
                        apparelList.Add(tmpApparel);
                        apparelCount++;
                    }
                }
            }

            // Corpses
            if (this.showEquippedC)
            {
                Corpse corpse;
                foreach (Thing th in Lister.Corpses())
                {
                    corpse = (Corpse) th;
                    if (corpse.InnerPawn.apparel != null && !th.Position.Fogged(Find.CurrentMap))
                    {
                        foreach (RimWorld.Apparel pth in corpse.InnerPawn.apparel.WornApparel)
                        {
                            tmpApparel = new Apparel();
                            tmpApparel.fillFromThing(pth, this.ce);
                            tmpApparel.pawn = corpse.InnerPawn.Name.ToString();
                            tmpApparel.pawnType = "corpse";
                            apparelList.Add(tmpApparel);
                            apparelCount++;
                        }
                    }
                }
            }

            // Weapons equipped by pawns
            Faction playerFaction;
            try
            {
                playerFaction = FactionUtility.DefaultFactionFrom(FactionDefOf.PlayerColony);
            }
            catch (System.NullReferenceException e)
            {
                playerFaction = FactionUtility.DefaultFactionFrom(FactionDefOf.PlayerTribe);
            }

            try
            {
                string pawnType = "";
                foreach (Pawn pw in Find.CurrentMap.mapPawns.AllPawnsSpawned.ToList())
                {
                    if (pw == null || pw.AnimalOrWildMan())
                    {
                        continue;
                    }

                    if (!pw.Position.Fogged(Find.CurrentMap))
                    {
                        bool hostile = !pw.IsColonist && pw.HostileTo(playerFaction);
                        bool friendly = !pw.IsColonist && !pw.HostileTo(playerFaction);
                        if ((this.showEquipped && pw.IsColonist) ||
                            (this.showEquippedP && pw.IsPrisonerOfColony) ||
                            (this.showEquippedH && hostile) ||
                            (this.showEquippedF && friendly) ||
                            (this.showEquippedC && pw.Dead))
                        {
                            if (pw.Dead)
                            {
                                pawnType = "corpse";
                            }
                            else if (pw.IsColonist)
                            {
                                pawnType = "colonist";
                            }
                            else if (pw.IsPrisonerOfColony)
                            {
                                pawnType = "prisoner";
                            }
                            else if (hostile)
                            {
                                pawnType = "hostile";
                            }
                            else if (friendly)
                            {
                                pawnType = "friendly";
                            }

                            foreach (ThingWithComps pth in pw.equipment.AllEquipmentListForReading)
                            {
                                if (pth.def.IsRangedWeapon || pth.def.IsMeleeWeapon)
                                {
                                    tb = this.getAppropriateTab(pth);
                                    switch (tb)
                                    {
                                        case WeaponsTab.Ranged:
                                            tmpRanged = new RangedWeapon();
                                            tmpRanged.fillFromThing(pth, this.ce);
                                            tmpRanged.pawn = pw.Name.ToString();
                                            tmpRanged.pawnType = pawnType;
                                            rangedList.Insert(0, tmpRanged);
                                            rangedCount++;
                                            break;
                                        case WeaponsTab.Melee:
                                            tmpMelee = new MeleeWeapon();
                                            tmpMelee.fillFromThing(pth, this.ce);
                                            tmpMelee.pawn = pw.Name.ToString();
                                            tmpMelee.pawnType = pawnType;
                                            meleeList.Insert(0, tmpMelee);
                                            meleeCount++;
                                            break;
                                        case WeaponsTab.Grenades:
                                            tmpGrenade = new GrenadeWeapon();
                                            tmpGrenade.fillFromThing(pth, this.ce);
                                            tmpGrenade.pawn = pw.Name.ToString();
                                            tmpGrenade.pawnType = pawnType;
                                            grenadeList.Insert(0, tmpGrenade);
                                            grenadeCount++;
                                            break;
                                        case WeaponsTab.Tools:
                                            tmpTool = new ToolWeapon();
                                            tmpTool.fillFromThing(pth, this.ce);
                                            tmpTool.pawn = pw.Name.ToString();
                                            tmpTool.pawnType = pawnType;
                                            toolList.Insert(0, tmpTool);
                                            toolCount++;
                                            break;
                                        case WeaponsTab.Other:
                                            tmpOther = new OtherWeapon();
                                            tmpOther.fillFromThing(pth, this.ce);
                                            tmpOther.pawn = pw.Name.ToString();
                                            tmpOther.pawnType = pawnType;
                                            otherList.Insert(0, tmpOther);
                                            otherCount++;
                                            break;
                                    }
                                }
                            }

                            foreach (RimWorld.Apparel pth in pw.apparel.WornApparel)
                            {
                                tmpApparel = new Apparel();
                                tmpApparel.fillFromThing(pth, this.ce);
                                tmpApparel.pawn = pw.Name.ToString();
                                tmpApparel.pawnType = pawnType;
                                apparelList.Add(tmpApparel);
                                apparelCount++;
                            }

                            // things in inventories
                            foreach (Thing pth in pw.inventory.innerContainer.ToList())
                            {
                                if (pth.def.IsRangedWeapon || pth.def.IsMeleeWeapon)
                                {
                                    tb = this.getAppropriateTab(pth);
                                    switch (tb)
                                    {
                                        case WeaponsTab.Ranged:
                                            tmpRanged = new RangedWeapon();
                                            tmpRanged.fillFromThing(pth, this.ce);
                                            tmpRanged.pawn = pw.Name.ToString();
                                            tmpRanged.pawnType = pawnType;
                                            rangedList.Insert(0, tmpRanged);
                                            rangedCount++;
                                            break;
                                        case WeaponsTab.Melee:
                                            tmpMelee = new MeleeWeapon();
                                            tmpMelee.fillFromThing(pth, this.ce);
                                            tmpMelee.pawn = pw.Name.ToString();
                                            tmpMelee.pawnType = pawnType;
                                            meleeList.Insert(0, tmpMelee);
                                            meleeCount++;
                                            break;
                                        case WeaponsTab.Grenades:
                                            tmpGrenade = new GrenadeWeapon();
                                            tmpGrenade.fillFromThing(pth, this.ce);
                                            tmpGrenade.pawn = pw.Name.ToString();
                                            tmpGrenade.pawnType = pawnType;
                                            grenadeList.Insert(0, tmpGrenade);
                                            grenadeCount++;
                                            break;
                                        case WeaponsTab.Tools:
                                            tmpTool = new ToolWeapon();
                                            tmpTool.fillFromThing(pth, this.ce);
                                            tmpTool.pawn = pw.Name.ToString();
                                            tmpTool.pawnType = pawnType;
                                            toolList.Insert(0, tmpTool);
                                            toolCount++;
                                            break;
                                        case WeaponsTab.Other:
                                            tmpOther = new OtherWeapon();
                                            tmpOther.fillFromThing(pth, this.ce);
                                            tmpOther.pawn = pw.Name.ToString();
                                            tmpOther.pawnType = pawnType;
                                            otherList.Insert(0, tmpOther);
                                            otherCount++;
                                            break;
                                    }
                                } /* else if (pth.def.IsApparel) {
									tmpApparel = new Apparel ();
									tmpApparel.fillFromThing (pth, this.ce);
									tmpApparel.pawn = pw.Name.ToString ();
									tmpApparel.pawnType = pawnType;
									apparelList.Add (tmpApparel);
									apparelCount++;
								}*/
                            }

                            pawnType = "";
                        }
                    }
                }
            }
            catch (System.NullReferenceException e)
            {
                Log.Message(e.Message);
            }

            if (this.showStorage)
            {
                var storages = Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>();
                IEnumerable<ThingWithComps> things = null;
                IEnumerable<RimWorld.Apparel> apparels = null;
                foreach (var s in storages)
                {
                    things = null;
                    apparels = null;
                    if (s.GetType().FullName == "WeaponStorage.Building_WeaponStorage")
                    {
                        MethodInfo weaponsMethod = s.GetType().GetMethod("GetWeapons");
                        var thingsProp = weaponsMethod.Invoke(s, new object[] {true});

                        if (thingsProp != null)
                        {
                            things = (IEnumerable<ThingWithComps>) thingsProp;
                        }
                    }
                    else if (s.GetType().FullName == "ChangeDresser.Building_Dresser")
                    {
                        PropertyInfo thingsProp = s.GetType().GetProperty("Apparel");
                        if (thingsProp != null)
                        {
                            apparels = (IEnumerable<RimWorld.Apparel>) thingsProp.GetValue(s, null);
                        }
                    }

                    if (things != null)
                    {
                        foreach (var pth in things)
                        {
                            if (pth.def.IsRangedWeapon || pth.def.IsMeleeWeapon)
                            {
                                tb = this.getAppropriateTab(pth);
                                switch (tb)
                                {
                                    case WeaponsTab.Ranged:
                                        tmpRanged = new RangedWeapon();
                                        tmpRanged.fillFromThing(pth, this.ce);
                                        tmpRanged.inStorage = true;
                                        tmpRanged.storagePos = s.Position;
                                        rangedList.Insert(0, tmpRanged);
                                        rangedCount++;
                                        break;
                                    case WeaponsTab.Melee:
                                        tmpMelee = new MeleeWeapon();
                                        tmpMelee.fillFromThing(pth, this.ce);
                                        tmpMelee.inStorage = true;
                                        tmpMelee.storagePos = s.Position;
                                        meleeList.Insert(0, tmpMelee);
                                        meleeCount++;
                                        break;
                                    case WeaponsTab.Grenades:
                                        tmpGrenade = new GrenadeWeapon();
                                        tmpGrenade.fillFromThing(pth, this.ce);
                                        tmpGrenade.inStorage = true;
                                        tmpGrenade.storagePos = s.Position;
                                        grenadeList.Insert(0, tmpGrenade);
                                        grenadeCount++;
                                        break;
                                    case WeaponsTab.Tools:
                                        tmpTool = new ToolWeapon();
                                        tmpTool.fillFromThing(pth, this.ce);
                                        tmpTool.inStorage = true;
                                        tmpTool.storagePos = s.Position;
                                        toolList.Insert(0, tmpTool);
                                        toolCount++;
                                        break;
                                    case WeaponsTab.Other:
                                        tmpOther = new OtherWeapon();
                                        tmpOther.fillFromThing(pth, this.ce);
                                        tmpOther.inStorage = true;
                                        tmpOther.storagePos = s.Position;
                                        otherList.Insert(0, tmpOther);
                                        otherCount++;
                                        break;
                                }
                            }
                        }
                    }

                    if (apparels != null)
                    {
                        foreach (var pth in apparels)
                        {
                            tmpApparel = new Apparel();
                            tmpApparel.fillFromThing(pth, this.ce);
                            tmpApparel.inStorage = true;
                            tmpApparel.storagePos = s.Position;
                            apparelList.Add(tmpApparel);
                            apparelCount++;
                        }
                    }
                }
            }

            if (this.showCraftable)
            {
                var buildingList = Find.CurrentMap.listerBuildings.allBuildingsColonist;
                foreach (var building in buildingList)
                {
                    if (!(building is Building_WorkTable)) continue;
                    var workTable = building as Building_WorkTable;
                    var recipeList = workTable.def.AllRecipes;
                    foreach (var recipe in recipeList)
                    {
                        if (!recipe.AvailableNow) continue;
                        var thingDef = recipe.ProducedThingDef;
                        if (thingDef == null) continue;
                        if (!thingDef.IsWeapon && !thingDef.IsApparel) continue;
                        var thingsFromRecipe = new List<Thing>();
                        if (thingDef.MadeFromStuff)
                        {
                            var stuff = GenStuff.DefaultStuffFor(thingDef);
                            thingsFromRecipe.Add(ThingMaker.MakeThing(thingDef, stuff));
                        }
                        else
                        {
                            thingsFromRecipe.Add(ThingMaker.MakeThing(thingDef));
                        }

                        foreach (var th in thingsFromRecipe)
                        {
                            tb = this.getAppropriateTab(th);
                            switch (tb)
                            {
                                case WeaponsTab.Ranged:
                                    tmpRanged = new RangedWeapon();
                                    tmpRanged.fillFromThing(th, this.ce);
                                    tmpRanged.craftable = true;
                                    tmpRanged.cratablePos = workTable;
                                    rangedList.Add(tmpRanged);
                                    rangedCount++;
                                    break;
                                case WeaponsTab.Melee:
                                    tmpMelee = new MeleeWeapon();
                                    tmpMelee.fillFromThing(th, this.ce);
                                    tmpMelee.craftable = true;
                                    tmpMelee.cratablePos = workTable;
                                    meleeList.Add(tmpMelee);
                                    meleeCount++;
                                    break;
                                case WeaponsTab.Grenades:
                                    tmpGrenade = new GrenadeWeapon();
                                    tmpGrenade.fillFromThing(th, this.ce);
                                    tmpGrenade.craftable = true;
                                    tmpGrenade.cratablePos = workTable;
                                    grenadeList.Add(tmpGrenade);
                                    grenadeCount++;
                                    break;
                                case WeaponsTab.Apparel:
                                    tmpApparel = new Apparel();
                                    tmpApparel.fillFromThing(th, this.ce);
                                    tmpApparel.craftable = true;
                                    tmpApparel.cratablePos = workTable;
                                    apparelList.Add(tmpApparel);
                                    apparelCount++;
                                    break;
                                case WeaponsTab.Tools:
                                    tmpTool = new ToolWeapon();
                                    tmpTool.fillFromThing(th, this.ce);
                                    tmpTool.craftable = true;
                                    tmpTool.cratablePos = workTable;
                                    toolList.Add(tmpTool);
                                    toolCount++;
                                    break;
                                case WeaponsTab.Other:
                                    tmpOther = new OtherWeapon();
                                    tmpOther.fillFromThing(th, this.ce);
                                    tmpOther.craftable = true;
                                    tmpOther.cratablePos = workTable;
                                    otherList.Add(tmpOther);
                                    otherCount++;
                                    break;
                            }
                        }
                    }
                }
            }

            UpdateListWithModdedContainers();
            UpdateListSorting();

            this.listUpdateNext = Find.TickManager.TicksGame + Verse.GenTicks.TickRareInterval;
            this.isDirty = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.isDirty = true;
            this.tdm = Prefs.TemperatureMode;
            if (this.tdm == TemperatureDisplayMode.Celsius)
            {
                this.tempUnit = "°C";
                this.tempCoeff = 1.0f;
            }
            else if (this.tdm == TemperatureDisplayMode.Fahrenheit)
            {
                this.tempUnit = "°F";
                this.tempCoeff = 1.8f;
            }
            else if (this.tdm == TemperatureDisplayMode.Kelvin)
            {
                this.tempUnit = "K";
                this.tempCoeff = 1.0f;
            }
            else
            {
                this.tempUnit = "";
                this.tempCoeff = 1.0f;
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);
            this.windowRect.width += 100;
            rect.yMin += 35;

            if (this.listUpdateNext < Find.TickManager.TicksGame)
            {
                this.isDirty = true;
            }

            if (this.isDirty)
            {
                this.UpdateList();
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            List<TabRecord> list = new List<TabRecord>();
            list.Add(new TabRecord("WeaponStats.Ranged".Translate(),
                delegate { this.curTab = WeaponStats.MainTabWindow_WeaponStats.WeaponsTab.Ranged; }, this.curTab == WeaponsTab.Ranged));
            list.Add(new TabRecord("WeaponStats.Melee".Translate(),
                delegate { this.curTab = WeaponStats.MainTabWindow_WeaponStats.WeaponsTab.Melee; }, this.curTab == WeaponsTab.Melee));
            list.Add(new TabRecord("WeaponStats.Grenades".Translate(),
                delegate { this.curTab = WeaponStats.MainTabWindow_WeaponStats.WeaponsTab.Grenades; }, this.curTab == WeaponsTab.Grenades));
            list.Add(new TabRecord("WeaponStats.Apparel".Translate(),
                delegate { this.curTab = WeaponStats.MainTabWindow_WeaponStats.WeaponsTab.Apparel; }, this.curTab == WeaponsTab.Apparel));
            if (Prefs.DevMode && false)
            {
                list.Add(new TabRecord("WeaponStats.Tools".Translate(),
                    delegate { this.curTab = WeaponStats.MainTabWindow_WeaponStats.WeaponsTab.Tools; }, this.curTab == WeaponsTab.Tools));
                list.Add(new TabRecord("WeaponStats.Other".Translate(),
                    delegate { this.curTab = WeaponStats.MainTabWindow_WeaponStats.WeaponsTab.Other; },
                    this.curTab == WeaponsTab.Other));
            }

            TabDrawer.DrawTabs(rect, list);
            WeaponsTab wpTab = this.curTab;

            bool showGroundOld = this.showGround;
            bool showEquippedOld = this.showEquipped;
            bool showEquippedPOld = this.showEquippedP;
            bool showEquippedHOld = this.showEquippedH;
            bool showEquippedFOld = this.showEquippedF;
            bool showEquippedCOld = this.showEquippedC;
            bool showCraftableOld = this.showCraftable;
            bool showStorageOld = this.showStorage;

            float currentX = rect.x;
            rect.y += 5;

            PrintAutoCheckbox("WeaponStats.Ground".Translate(), ref this.showGround, ref currentX, ref rect);
            PrintAutoCheckbox("WeaponStats.Colonists".Translate(), ref this.showEquipped, ref currentX, ref rect);
            PrintAutoCheckbox("WeaponStats.Prisoners".Translate(), ref this.showEquippedP, ref currentX, ref rect);
            PrintAutoCheckbox("WeaponStats.Hostiles".Translate(), ref this.showEquippedH, ref currentX, ref rect);
            PrintAutoCheckbox("WeaponStats.Friendlies".Translate(), ref this.showEquippedF, ref currentX, ref rect);
            PrintAutoCheckbox("WeaponStats.Corpses".Translate(), ref this.showEquippedC, ref currentX, ref rect);
            if (weaponStorageLoaded || outfitStorageLoaded)
            {
                PrintAutoCheckbox("WeaponStats.Storage".Translate(), ref this.showStorage, ref currentX, ref rect);
            }

            PrintAutoCheckbox("WeaponStats.Craftable".Translate(), ref this.showCraftable, ref currentX, ref rect);

            if (showGroundOld != this.showGround || showEquippedOld != this.showEquipped || showEquippedPOld != this.showEquippedP ||
                showEquippedHOld != this.showEquippedH
                || showEquippedFOld != this.showEquippedF || showEquippedCOld != this.showEquippedC || showStorageOld != this.showStorage ||
                showCraftableOld != this.showCraftable)
            {
                this.isDirty = true;
            }

            switch (wpTab)
            {
                case WeaponsTab.Ranged:
                    DoRangedPage(rect, rangedCount, rangedList);
                    break;
                case WeaponsTab.Melee:
                    DoMeleePage(rect, meleeCount, meleeList);
                    break;
                case WeaponsTab.Grenades:
                    DoGrenadePage(rect, grenadeCount, grenadeList);
                    break;
                case WeaponsTab.Other:
                    DoOtherPage(rect, otherCount, otherList);
                    break;
                case WeaponsTab.Tools:
                    DoToolsPage(rect, toolCount, toolList);
                    break;
                case WeaponsTab.Apparel:
                    DoApparelPage(rect, apparelCount, apparelList);
                    break;
                default:
                    DoRangedPage(rect, rangedCount, rangedList);
                    break;
            }
        }

        private void PrintAutoCheckbox(string text, ref bool value, ref float currentX, ref Rect rect, bool defaultValue = false)
        {
            var textWidth = Text.CalcSize(text).x + 25f;
            Widgets.CheckboxLabeled(new Rect(currentX, rect.y, textWidth, 30), text, ref value, defaultValue);
            currentX += textWidth + 25f;
        }

        private void PrintAutoRadio(string text, ref bool chosen, ref float currentX, ref Rect rect)
        {
            var textWidth = Text.CalcSize(text).x + 25f;
            if (Widgets.RadioButtonLabeled(new Rect(currentX, rect.y, textWidth, 30), text, chosen))
            {
                accRbAll = false;
                accRbTouch = false;
                accRbShort = false;
                accRbMedium = false;
                accRbLong = false;
                chosen = true;
            }

            currentX += textWidth + 25f;
        }
    }
}