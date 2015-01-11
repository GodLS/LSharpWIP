using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace PennyJinx
{
    internal class Cleanser
    {
        public static List<QssSpell> QssSpells = new List<QssSpell>();
        public static bool DeathMarkCreated;
        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static void CreateQssSpellMenu()
        {
            foreach (
                var spell in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(he => he.IsEnemy)
                        .SelectMany(hero => QssSpells.Where(spell => spell.ChampName == hero.ChampionName)))
            {
                PennyJinx.Menu.SubMenu("QSSSpell")
                    .AddItem(
                        new MenuItem("en" + spell.SpellBuff, spell.SpellName + " always ?").SetValue(spell.IsEnabled));
                PennyJinx.Menu.SubMenu("QSSSpell")
                    .AddItem(
                        new MenuItem("onlyK" + spell.SpellBuff, spell.SpellName + " if killed by it?").SetValue(
                            spell.OnlyKill));
                PennyJinx.Menu.SubMenu("QSSSpell").AddItem(new MenuItem("Spacer" + spell.SpellBuff, " "));
            }
        }

        public static void CreateTypeQssMenu()
        {
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("stun", "Stuns").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("charm", "Charms").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("taunt", "Taunts").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("fear", "Fears").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("snare", "Snares").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("silence", "Silences").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("supression", "Supression").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("polymorph", "Polymorphs").SetValue(true));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("blind", "Blinds").SetValue(false));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("slow", "Slows").SetValue(false));
            PennyJinx.Menu.SubMenu("QSST").AddItem(new MenuItem("poison", "Poisons").SetValue(false));
        }

        public static void CreateQssSpellList()
        {
            /**Danger Level 5 Spells*/
            QssSpells.Add(new QssSpell
            {
                ChampName = "Warwick",
                IsEnabled = true,
                SpellBuff = "InfiniteDuress",
                SpellName = "Warwick R",
                OnlyKill = false
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Zed",
                IsEnabled = true,
                SpellBuff = "zedulttargetmark",
                SpellName = "Zed R",
                OnlyKill = true
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Rammus",
                IsEnabled = true,
                SpellBuff = "PuncturingTaunt",
                SpellName = "Rammus E",
                OnlyKill = false
            });
            /** Danger Level 4 Spells*/
            QssSpells.Add(new QssSpell
            {
                ChampName = "Skarner",
                IsEnabled = true,
                SpellBuff = "SkarnerImpale",
                SpellName = "Skaner R",
                OnlyKill = false
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Fizz",
                IsEnabled = true,
                SpellBuff = "FizzMarinerDoom",
                SpellName = "Fizz R",
                OnlyKill = false
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Galio",
                IsEnabled = true,
                SpellBuff = "GalioIdolOfDurand",
                SpellName = "Galio R",
                OnlyKill = false
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Malzahar",
                IsEnabled = true,
                SpellBuff = "AlZaharNetherGrasp",
                SpellName = "Malz R",
                OnlyKill = false
            });
            /** Danger Level 3 Spells*/
            QssSpells.Add(new QssSpell
            {
                ChampName = "Zilean",
                IsEnabled = false,
                SpellBuff = "timebombenemybuff",
                SpellName = "Zilean Q",
                OnlyKill = true
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Vladimir",
                IsEnabled = false,
                SpellBuff = "VladimirHemoplague",
                SpellName = "Vlad R",
                OnlyKill = true
            });
            QssSpells.Add(new QssSpell
            {
                ChampName = "Mordekaiser",
                IsEnabled = true,
                SpellBuff = "MordekaiserChildrenOfTheGrave",
                SpellName = "Morde R",
                OnlyKill = true
            });
            /** Danger Level 2 Spells*/
            QssSpells.Add(new QssSpell
            {
                ChampName = "Poppy",
                IsEnabled = true,
                SpellBuff = "PoppyDiplomaticImmunity",
                SpellName = "Poppy R",
                OnlyKill = false
            });
        }

        internal static void CleanUselessSpells()
        {
            var nameList = ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy).Select(h => h.ChampionName).ToList();
            foreach (var spell in QssSpells.Where(spell => !nameList.Contains(spell.ChampName)))
            {
                QssSpells.Remove(spell);
            }
        }

        public static void CleanserBySpell()
        {
            var hasIt = Items.HasItem(3139) || Items.HasItem(3140) || Items.HasItem(3137);
            if (!PennyJinx.IsMenuEnabled("UseQSS") || !hasIt)
            {
                return;
            }

            var ccList = (from spell in QssSpells
                where Player.HasBuff(spell.SpellBuff)
                select new CC {BuffName = spell.SpellBuff, WillKillMe = WillSpellKillMe(spell)}).ToList();
            foreach (var cc in ccList)
            {
                if (PennyJinx.IsMenuEnabled("en" + cc.BuffName))
                {
                    Console.WriteLine(@"Should Cleanse. " + cc.BuffName + @" cause it is a spell");
                    if (cc.BuffName == "zedulttargetmark")
                    {
                        Utility.DelayAction.Add(500, Cleanse);
                    }
                    else
                    {
                        Cleanse();
                    }
                }
                if (!PennyJinx.IsMenuEnabled("onlyK" + cc.BuffName) || !cc.WillKillMe)
                {
                    continue;
                }

                Console.WriteLine(@"Should Cleanse. " + cc.BuffName + @" cause it will kill me");
                Cleanse();
            }
        }

        public static void EnableCheck()
        {
            foreach (var spell in QssSpells)
            {
                if (PennyJinx.IsMenuEnabled("en" + spell.SpellBuff))
                {
                    PennyJinx.Menu.Item("onlyK" + spell.SpellBuff).SetValue(false);
                }

                if (PennyJinx.IsMenuEnabled("onlyK" + spell.SpellBuff))
                {
                    PennyJinx.Menu.Item("en" + spell.SpellBuff).SetValue(false);
                }
            }
        }

        public static void CleanserByBuffType()
        {
            var hasIt = Items.HasItem(3139) || Items.HasItem(3140) || Items.HasItem(3137);
            if (!PennyJinx.IsMenuEnabled("UseQSS") || !hasIt)
            {
                return;
            }

            var numBuffs = UnitBuffs(Player);
            if (numBuffs >= PennyJinx.Menu.Item("QSSMinBuffs").GetValue<Slider>().Value) Cleanse();
        }

        private static bool WillSpellKillMe(QssSpell spell)
        {
            var spells = SpellSlot.R;
            if (spell.SpellName.Contains(spell.ChampName + " R"))
            {
                spells = SpellSlot.R;
            }

            if (spell.SpellName.Contains(spell.ChampName + " Q"))
            {
                spells = SpellSlot.Q;
            }

            if (spell.SpellName.Contains(spell.ChampName + " W"))
            {
                spells = SpellSlot.W;
            }

            if (spell.SpellName.Contains(spell.ChampName + " E"))
            {
                spells = SpellSlot.E;
            }

            var theDamage = GetByChampName(spell.ChampName).GetDamageSpell(Player, spells).CalculatedDamage;
            BuffInstance theBuff = null;
            foreach (var buff in Player.Buffs)
            {
                if (buff.Name == spell.SpellBuff)
                {
                    theBuff = buff;
                }
            }
            if (theBuff != null)
            {
                var endTime = theBuff.EndTime;
                var difference = endTime - Environment.TickCount; //TODO Factor Player Regen
            }
            return theDamage >= (Player.Health);
        }

        internal static void SaveMyAss()
        {
            if (DeathMarkCreated &&
                Player.HasBuff(GetSpellByName("Zed R").SpellBuff, true) && GetSpellByName("Zed R").OnlyKill)
            {
                Utility.DelayAction.Add(200, Cleanse);
            }
        }

        private static int UnitBuffs(Obj_AI_Hero unit)
        {
            //Taken from 'Oracle Activator'. Thanks Kurisuu ^.^
            var cc = 0;
            if (PennyJinx.Menu.Item("slow").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Slow))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("blind").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Blind))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("charm").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Charm))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("fear").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Fear))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("snare").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Snare))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("taunt").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Taunt))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("supression").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Suppression))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("stun").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Stun))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("polymorph").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Polymorph))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("silence").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Silence))
                {
                    cc += 1;
                }
            }

            if (PennyJinx.Menu.Item("poison").GetValue<bool>())
            {
                if (unit.HasBuffOfType(BuffType.Poison))
                {
                    cc += 1;
                }
            }

            return cc;
        }

        internal static void Cleanse()
        {
            if (Items.HasItem(3140))
            {
                PennyJinx.UseItem(3140, Player); //QSS
            }
            if (Items.HasItem(3139))
            {
                PennyJinx.UseItem(3139, Player); //Mercurial
            }
            if (Items.HasItem(3137))
            {
                PennyJinx.UseItem(3137, Player); //Dervish Blade
            }
        }

        public static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (sender.Name != "Zed_Base_R_buf_tell.troy" || !sender.IsEnemy)
            {
                return;
            }

            DeathMarkCreated = true;
            SaveMyAss();
        }

        public static void OnDeleteObj(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Zed_Base_R_buf_tell.troy" && sender.IsEnemy)
            {
                DeathMarkCreated = false;
            }
        }

        private static QssSpell GetSpellByName(String name)
        {
            return QssSpells.Find(spell => spell.SpellName == name);
        }

        private static Obj_AI_Hero GetByChampName(String name)
        {
            return ObjectManager.Get<Obj_AI_Hero>().First(h => h.ChampionName == name && h.IsEnemy);
        }
    }

    internal class QssSpell
    {
        public String ChampName { get; set; }
        public String SpellName { get; set; }
        public String SpellBuff { get; set; }
        public bool IsEnabled { get; set; }
        public bool OnlyKill { get; set; }
    }

    internal class CC
    {
        public String BuffName { get; set; }
        public bool WillKillMe { get; set; }
    }
}