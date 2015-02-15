using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility
{
    class Cleanser
    {
        //DONE

        #region
        private static readonly BuffType[] Buffs = { BuffType.Blind, BuffType.Charm, BuffType.CombatDehancer, BuffType.Fear, BuffType.Flee, BuffType.Knockback, BuffType.Knockup, BuffType.Polymorph, BuffType.Silence, BuffType.Sleep, BuffType.Snare, BuffType.Stun, BuffType.Suppression, BuffType.Taunt };
        private static float _lastCheckTick;
        private static readonly Menu MenuInstance = DZAIO.Config;

        public static double HealthBuffer
        {
            get { return MenuHelper.getSliderValue("dzaio.cleanser.hpbuffer"); }
        }
        public static float Delay
        {
            get { return MenuHelper.getSliderValue("dzaio.cleanser.delay"); }
        }

        private static readonly List<QssSpell> QssSpells = new List<QssSpell>
        {
            new QssSpell
            {
                ChampName = "Warwick",
                IsEnabled = true,
                SpellBuff = "InfiniteDuress",
                SpellName = "Warwick R",
                RealName = "warwickR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Zed",
                IsEnabled = true,
                SpellBuff = "zedulttargetmark",
                SpellName = "Zed R",
                RealName = "zedultimate",
                OnlyKill = true,
                Slot = SpellSlot.R,
                Delay = 800f
            },
            new QssSpell
            {
                ChampName = "Rammus",
                IsEnabled = true,
                SpellBuff = "PuncturingTaunt",
                SpellName = "Rammus E",
                RealName = "rammusE",
                OnlyKill = false,
                Slot = SpellSlot.E,
                Delay = 100f                
            },
            /** Danger Level 4 Spells*/
            new QssSpell
            {
                ChampName = "Skarner",
                IsEnabled = true,
                SpellBuff = "SkarnerImpale",
                SpellName = "Skaner R",
                RealName = "skarnerR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Fizz",
                IsEnabled = true,
                SpellBuff = "FizzMarinerDoom",
                SpellName = "Fizz R",
                RealName = "FizzR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Galio",
                IsEnabled = true,
                SpellBuff = "GalioIdolOfDurand",
                SpellName = "Galio R",
                RealName = "GalioR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Malzahar",
                IsEnabled = true,
                SpellBuff = "AlZaharNetherGrasp",
                SpellName = "Malz R",
                RealName = "MalzaharR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 200f
            },
            /** Danger Level 3 Spells*/
            new QssSpell
            {
                ChampName = "Zilean",
                IsEnabled = false,
                SpellBuff = "timebombenemybuff",
                SpellName = "Zilean Q",
                OnlyKill = true,
                Slot = SpellSlot.Q,
                Delay = 700f
            },
            new QssSpell
            {
                ChampName = "Vladimir",
                IsEnabled = false,
                SpellBuff = "VladimirHemoplague",
                SpellName = "Vlad R",
                RealName = "VladimirR",
                OnlyKill = true,
                Slot = SpellSlot.R,
                Delay = 700f
            },
            new QssSpell
            {
                ChampName = "Mordekaiser",
                IsEnabled = true,
                SpellBuff = "MordekaiserChildrenOfTheGrave",
                SpellName = "Morde R",
                OnlyKill = true,
                 Slot = SpellSlot.R,
                Delay = 800f
            },
            /** Danger Level 2 Spells*/
            new QssSpell
            {
                ChampName = "Poppy",
                IsEnabled = true,
                SpellBuff = "PoppyDiplomaticImmunity",
                SpellName = "Poppy R",
                RealName = "PoppyR",
                OnlyKill = false,
                 Slot = SpellSlot.R,
                Delay = 100f
            }
        };
        #endregion

        public static void OnLoad()
        {
            var cName = DZAIO.Player.ChampionName;
            var spellSubmenu = new Menu(cName + " - Cleanser", cName + "Cleanser");

            //Spell Cleanser Menu
            var spellCleanserMenu = new Menu("Cleanser - Spell Cleanser", "dzaio.cleanser.spell");
            foreach (var spell in QssSpells.Where(h => GetChampByName(h.ChampName) != null))
            {
                var sMenu = new Menu(spell.SpellName, cName + spell.SpellBuff);
                sMenu.AddItem(
                    new MenuItem("dzaio.cleanser.spell."+ spell.SpellBuff + "A", "Always").SetValue(!spell.OnlyKill));
                sMenu.AddItem(
                    new MenuItem("dzaio.cleanser.spell." + spell.SpellBuff + "K", "Only if killed by it").SetValue(spell.OnlyKill));
                sMenu.AddItem(
                    new MenuItem("dzaio.cleanser.spell." + spell.SpellBuff + "D", "Delay before cleanse").SetValue(new Slider((int)spell.Delay, 0, 10000)));
                spellCleanserMenu.AddSubMenu(sMenu);
            }
            //Bufftype cleanser menu
            var buffCleanserMenu = new Menu("Cleanser - Bufftype Cleanser", cName + "dzaio.cleanser.bufftype");

            foreach (var buffType in Buffs)
            {
                buffCleanserMenu.AddItem(new MenuItem(cName + buffType, buffType.ToString()).SetValue(true));
            }

            buffCleanserMenu.AddItem(new MenuItem("dzaio.cleanser.bufftype.minbuffs", "Min Buffs").SetValue(new Slider(2, 1, 5)));

            var allyMenu = new Menu("Cleanser - Use On", "UseOn");
            foreach (var ally in HeroManager.Allies)
            {
                allyMenu.AddItem(new MenuItem("dzaio.cleanser.allies.useon." + ally.ChampionName, ally.ChampionName).SetValue(true));
            }

            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.items.qss", "Use QSS").SetValue(true));
            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.items.scimitar", "Use Mercurial Scimitar").SetValue(true));
            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.items.dervish", "Use Dervish Blade").SetValue(true));
            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.items.michael", "Use Mikael's Crucible").SetValue(true));
            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.items.cleanse", "Use Cleanse").SetValue(true));
            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.hpbuffer", "Health Buffer").SetValue(new Slider(20)));
            spellSubmenu.AddItem(new MenuItem("dzaio.cleanser.delay", "Global Delay (Prevents Lag)").SetValue(new Slider(100,0,200)));

            spellSubmenu.AddSubMenu(spellCleanserMenu);
            spellSubmenu.AddSubMenu(buffCleanserMenu);
            spellSubmenu.AddSubMenu(allyMenu);
            MenuInstance.AddSubMenu(spellSubmenu);

            //Subscribe the Events
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Environment.TickCount - _lastCheckTick < Delay)
                return;
            _lastCheckTick = Environment.TickCount;

            KillCleansing();
            SpellCleansing();
            BuffTypeCleansing();
        }

        #region BuffType Cleansing
        /// <summary>
        /// Cleanses by BuffType on player
        /// </summary>
        static void BuffTypeCleansing()
        {

            //MASSIVE TODO: Check if the buff is "stuns, roots, taunts, fears, silences and slows" before using Mikaels
            //Player Cleansing
            if (OneReady())
            {
                var buffCount = Buffs.Count(buff => DZAIO.Player.HasBuffOfType(buff) && BuffTypeEnabled(buff));
                if (buffCount >= MenuHelper.getSliderValue("dzaio.cleanser.bufftype.minbuffs"))
                {
                    CastCleanseItem(ObjectManager.Player);
                }
            }
            //Ally Cleansing
            if (!MichaelReady())
            {
                return;
            }
            var allies = DZAIO.Player.GetAlliesInRange(600f);
            var highestAlly = ObjectManager.Player;
            var highestCount = 0;
            foreach (var ally in allies)
            {
                var allyBCount = Buffs.Count(buff => ally.HasBuffOfType(buff) && BuffTypeEnabled(buff));
                if (allyBCount > highestCount && allyBCount >= MenuHelper.getSliderValue("dzaio.cleanser.bufftype.minbuffs") && MenuHelper.isMenuEnabled("dzaio.cleanser.allies.useon." + ally.ChampionName))
                {
                    highestCount = allyBCount;
                    highestAlly = ally;
                }
            }
            if (!highestAlly.IsMe )
            {
                CastCleanseItem(highestAlly);
            }
        }
        #endregion

        #region Spell Cleansing
        /// <summary>
        /// Cleanses using the SpellList buffs as input
        /// </summary>
        static void SpellCleansing()
        {
            if (OneReady())
            {
                QssSpell mySpell = null;
                if (
                    QssSpells.Where(
                        spell => DZAIO.Player.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff))
                        .OrderBy(
                            spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                        .Any())
                {
                mySpell =
                    QssSpells.Where(
                        spell => DZAIO.Player.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff))
                        .OrderBy(
                            spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                        .First();
                }
                if (mySpell != null)
                {
                    UseCleanser(mySpell, ObjectManager.Player);
                }
            }
            if (!MichaelReady())
            {
                return;
            }
            //Ally Cleansing
            var allies = DZAIO.Player.GetAlliesInRange(600f);
            var highestAlly = ObjectManager.Player;
            var highestDamage = 0f;
            QssSpell highestSpell = null;
            foreach (var ally in allies)
            {
                QssSpell theSpell = null;
                if (QssSpells.Where(spell => ally.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff)).OrderBy(spell => GetChampByName(spell.ChampName).GetSpellDamage(ally, spell.Slot)).Any())
                {
                    theSpell = QssSpells.Where(
                        spell => ally.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff))
                        .OrderBy(spell => GetChampByName(spell.ChampName).GetSpellDamage(ally, spell.Slot))
                        .First();
                }
                if (theSpell != null)
                {
                    var damageDone = GetChampByName(theSpell.ChampName).GetSpellDamage(ally, theSpell.Slot);
                    if (damageDone >= highestDamage && MenuHelper.isMenuEnabled("dzaio.cleanser.allies.useon." + ally.ChampionName))
                    {
                        highestSpell = theSpell;
                        highestDamage = (float)damageDone;
                        highestAlly = ally;
                    }
                }
            }
            if (!highestAlly.IsMe && highestSpell != null)
            {
                UseCleanser(highestSpell,highestAlly);
            }
        }
        #endregion

        #region Spell Will Kill Cleansing
        /// <summary>
        /// Will Cleanse only on Kill
        /// </summary>
        static void KillCleansing()
        {
            if (OneReady())
            {
                QssSpell mySpell = null;
                if (
                    QssSpells.Where(
                        spell => DZAIO.Player.HasBuff(spell.SpellBuff, true) && SpellEnabledOnKill(spell.SpellBuff) && GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot) > DZAIO.Player.Health + HealthBuffer)
                        .OrderBy(
                            spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                        .Any())
                {
                    mySpell =
                        QssSpells.Where(
                            spell => DZAIO.Player.HasBuff(spell.SpellBuff, true) && SpellEnabledOnKill(spell.SpellBuff))
                            .OrderBy(
                                spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                            .First();
                }
                if (mySpell != null)
                {
                    UseCleanser(mySpell, ObjectManager.Player);
                }
            }
            if (!MichaelReady())
            {
                return;
            }
            //Ally Cleansing
            var allies = DZAIO.Player.GetAlliesInRange(600f);
            var highestAlly = ObjectManager.Player;
            var highestDamage = 0f;
            QssSpell highestSpell = null;
            foreach (var ally in allies)
            {
                QssSpell theSpell = null;
                if (QssSpells.Where(spell => ally.HasBuff(spell.SpellBuff, true) && SpellEnabledOnKill(spell.SpellBuff) && GetChampByName(spell.ChampName).GetSpellDamage(ally, spell.Slot) > ally.Health + HealthBuffer).OrderBy(spell => GetChampByName(spell.ChampName).GetSpellDamage(ally, spell.Slot)).Any())
                {
                    theSpell = QssSpells.Where(
                        spell => ally.HasBuff(spell.SpellBuff, true) && SpellEnabledOnKill(spell.SpellBuff))
                        .OrderBy(spell => GetChampByName(spell.ChampName).GetSpellDamage(ally, spell.Slot))
                        .First();
                }
                if (theSpell != null)
                {
                    var damageDone = GetChampByName(theSpell.ChampName).GetSpellDamage(ally, theSpell.Slot);
                    if (damageDone >= highestDamage && MenuHelper.isMenuEnabled("dzaio.cleanser.allies.useon." + ally.ChampionName))
                    {
                        highestSpell = theSpell;
                        highestDamage = (float)damageDone;
                        highestAlly = ally;
                    }
                }
            }
            if (!highestAlly.IsMe && highestSpell != null)
            {
                UseCleanser(highestSpell, highestAlly);
            }
        }


        #endregion

        #region Cleansing
        static void UseCleanser(QssSpell spell,Obj_AI_Hero target)
        {
            LeagueSharp.Common.Utility.DelayAction.Add(SpellDelay(spell.RealName), () => CastCleanseItem(target));
        }
        static void CastCleanseItem(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return;
            }

            if (MenuHelper.isMenuEnabled("dzaio.cleanser.items.michael") && Items.HasItem(3222) &&
                   Items.CanUseItem(3222) && target.IsValidTarget(600f)) //TODO Put Michaels buff id
            {
                Items.UseItem(3222, target);
                return;
            }

            if (MenuHelper.isMenuEnabled("dzaio.cleanser.items.cleanse") && SummonerSpells.Cleanse.IsReady()) //TODO Put Michaels buff id
            {
                SummonerSpells.Cleanse.Cast();
                return;
            }

            if (MenuHelper.isMenuEnabled("dzaio.cleanser.items.qss") && Items.HasItem(3140) &&
                Items.CanUseItem(3140) && target.IsMe)
            {
                Items.UseItem(3140, ObjectManager.Player);
                return;
            }

            if (MenuHelper.isMenuEnabled("dzaio.cleanser.items.scimitar") && Items.HasItem(3139) &&
                Items.CanUseItem(3139) && target.IsMe)
            {
                Items.UseItem(3139, ObjectManager.Player);
                return;
            }

            if (MenuHelper.isMenuEnabled("dzaio.cleanser.items.dervish") && Items.HasItem(3137) &&
                Items.CanUseItem(3137) && target.IsMe)
            {
                Items.UseItem(3137, ObjectManager.Player);
            }
        }
        #endregion

        #region Utility Methods

        private static bool OneReady()
        {
            return (MenuHelper.isMenuEnabled("dzaio.cleanser.items.qss") && Items.HasItem(3140) &&
                    Items.CanUseItem(3140)) ||
                   (MenuHelper.isMenuEnabled("dzaio.cleanser.items.scimitar") && Items.HasItem(3139) &&
                    Items.CanUseItem(3139)) ||
                   (MenuHelper.isMenuEnabled("dzaio.cleanser.items.dervish") && Items.HasItem(3137) &&
                    Items.CanUseItem(3137)) ||
                   (MenuHelper.isMenuEnabled("dzaio.cleanser.items.cleanse") && 
                   SummonerSpells.Cleanse.IsReady());
        }
        private static bool MichaelReady()
        {
            return (MenuHelper.isMenuEnabled("dzaio.cleanser.items.michael") && Items.HasItem(3222) &&
                    Items.CanUseItem(3222));
        }
        private static bool BuffTypeEnabled(BuffType buffType)
        {
            return MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + buffType);
        }
        private static int SpellDelay(String sName)
        {
            return MenuHelper.getSliderValue("dzaio.cleanser.spell." + sName + "D");
        }
        private static bool SpellEnabledOnKill(String sName)
        {
            return MenuHelper.isMenuEnabled("dzaio.cleanser.spell." + sName + "K");
        }
        private static bool SpellEnabledAlways(String sName)
        {
            return MenuHelper.isMenuEnabled("dzaio.cleanser.spell." + sName + "A");
        }

        private static Obj_AI_Hero GetChampByName(String enemyName)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Find(h => h.IsEnemy && h.ChampionName == enemyName);
        }
        #endregion
    }

    internal class QssSpell
    {
        public String ChampName { get; set; }
        public String SpellName { get; set; }
        public String RealName { get; set; }
        public String SpellBuff { get; set; }
        public bool IsEnabled { get; set; }
        public bool OnlyKill { get; set; }
        public SpellSlot Slot { get; set; }
        public float Delay { get; set; }
    }
}
