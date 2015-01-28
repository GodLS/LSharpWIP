using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using LeagueSharp;
using LeagueSharp.Common;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace DZAIO.Utility
{
    class Cleanser
    {
        //DONE

        #region
        private static readonly BuffType[] Buffs = { BuffType.Blind, BuffType.Charm, BuffType.CombatDehancer, BuffType.Fear, BuffType.Flee, BuffType.Knockback, BuffType.Knockup, BuffType.Polymorph, BuffType.Silence, BuffType.Sleep, BuffType.Snare, BuffType.Stun, BuffType.Suppression, BuffType.Taunt };
        private static float _lastCheckTick;
        private static readonly Menu MenuInstance = DZAIO.Config;

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
            var spellCleanserMenu = new Menu("Spell Cleanser", cName+"SCleanser");
            foreach (var spell in QssSpells)
            {
                var sMenu = new Menu(cName + spell.SpellName, cName + spell.SpellBuff);
                sMenu.AddItem(
                    new MenuItem(cName + spell.SpellBuff + "A", "Always").SetValue(!spell.OnlyKill));
                sMenu.AddItem(
                    new MenuItem(cName + spell.SpellBuff + "K", "Only if killed by it").SetValue(spell.OnlyKill));
                sMenu.AddItem(
                    new MenuItem(cName + spell.SpellBuff + "D", "Delay before cleanse").SetValue(new Slider((int)spell.Delay,0,10000)));
                spellSubmenu.AddSubMenu(sMenu);
            }
            spellCleanserMenu.AddSubMenu(spellSubmenu);
            //Bufftype cleanser menu
            var buffCleanserMenu = new Menu("Bufftype Cleanser", cName + "BCleanser");
            foreach (var buffType in Buffs)
            {
                buffCleanserMenu.AddItem(new MenuItem(cName + buffType, buffType.ToString()).SetValue(true));
            }
            buffCleanserMenu.AddItem(new MenuItem(cName + "MinBuffs", "Min Buffs").SetValue(new Slider(2, 1, 5)));
            MenuInstance.AddSubMenu(spellCleanserMenu);
            MenuInstance.AddSubMenu(buffCleanserMenu);
            spellSubmenu.addUseOnMenu(true,"Cleanser");

            spellSubmenu.AddItem(new MenuItem(cName + "QSS", "Use QSS").SetValue(true));
            spellSubmenu.AddItem(new MenuItem(cName + "Scimitar", "Use Mercurial Scimitar").SetValue(true));
            spellSubmenu.AddItem(new MenuItem(cName + "Dervish", "Use Dervish Blade").SetValue(true));
            spellSubmenu.AddItem(new MenuItem(cName + "Michael", "Use Michael's Crucible").SetValue(true));
            //Subscribe the Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            DamagePrediction.DamagePrediction.OnSpellWillKill += DamagePrediction_OnSpellWillKill;
        }

        static void DamagePrediction_OnSpellWillKill(Obj_AI_Hero sender, Obj_AI_Hero target,SpellData sData)
        {
            var theSpell = QssSpells.Find(spell => spell.RealName == sData.Name);

            if (target.IsAlly && !target.IsMe)
            {
                if (theSpell == null)
                    return;
                if ((SpellEnabledOnKill(theSpell.SpellBuff) || SpellEnabledAlways(theSpell.SpellBuff)) && MenuHelper.isMenuEnabled("UseOn" + target.ChampionName))
                {
                    var _spell = QssSpells.Find(spell => spell.RealName == sData.Name);
                    if (target.IsValidTarget(600f, false) && _spell != null)
                    {
                        UseCleanser(_spell,target);
                    }
                }
            }
            if (target.IsMe)
            {
                if (SpellEnabledOnKill(theSpell.SpellBuff) || SpellEnabledAlways(theSpell.SpellBuff))
                {
                    var _spell = QssSpells.Find(spell => spell.RealName == sData.Name);
                    if (_spell != null)
                    {
                        UseCleanser(_spell,target);
                    }
                }
            }
            
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Environment.TickCount - _lastCheckTick < 150)
                return;
            _lastCheckTick = Environment.TickCount;

            SpellCleansing();
            BuffTypeCleansing();
        }

        #region BuffType Cleansing
        static void BuffTypeCleansing()
        {
            //Player Cleansing
            if (OneReady())
            {
                var buffCount = Buffs.Count(buff => DZAIO.Player.HasBuffOfType(buff) && BuffTypeEnabled(buff));
                if (buffCount >= MenuHelper.getSliderValue(ObjectManager.Player.ChampionName + "MinBuffs"))
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
                if (allyBCount > highestCount && allyBCount >= MenuHelper.getSliderValue(ObjectManager.Player.ChampionName + "MinBuffs") && MenuHelper.isMenuEnabled("UseOn" + ally.ChampionName))
                {
                    highestCount = allyBCount;
                    highestAlly = ally;
                }
            }
            if (!highestAlly.IsMe)
            {
                CastCleanseItem(highestAlly);
            }
        }
        #endregion

        #region SpellCleansing
        static void SpellCleansing()
        {
            if (OneReady())
            {
                var buffCount =
                    QssSpells.Count(
                        spell => DZAIO.Player.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff));
                var mySpell =
                    QssSpells.Where(
                        spell => DZAIO.Player.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff))
                        .OrderBy(
                            spell => GetChampByName(spell.ChampName).GetDamageSpell(ObjectManager.Player, spell.Slot))
                        .First();
                if (buffCount > 0 && mySpell != null)
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
                var theSpell = QssSpells.Where(spell => ally.HasBuff(spell.SpellBuff, true) && SpellEnabledAlways(spell.SpellBuff)).OrderBy(spell => GetChampByName(spell.ChampName).GetDamageSpell(ally, spell.Slot)).First();
                if (theSpell != null)
                {
                    var damageDone = GetChampByName(theSpell.ChampName).GetSpellDamage(ally, theSpell.Slot);
                    if (damageDone >= highestDamage && MenuHelper.isMenuEnabled("UseOn" + ally.ChampionName))
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

        #region Cleansing
        static void UseCleanser(QssSpell spell,Obj_AI_Hero target)
        {
            LeagueSharp.Common.Utility.DelayAction.Add(SpellDelay(spell.RealName), () => CastCleanseItem(target));
        }
        static void CastCleanseItem(Obj_AI_Hero target)
        {
            if (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "Michael") && Items.HasItem(0) &&
                   Items.CanUseItem(0)) //TODO Put Michaels buff id
            {
                Items.UseItem(0, target ?? ObjectManager.Player);
                return;
            }

            if (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "QSS") && Items.HasItem(3140) &&
                Items.CanUseItem(3140) && target.IsMe)
            {
                Items.UseItem(3140, ObjectManager.Player);
                return;
            }

            if (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "Scimitar") && Items.HasItem(3139) &&
                Items.CanUseItem(3139) && target.IsMe)
            {
                Items.UseItem(3139, ObjectManager.Player);
                return;
            }

            if (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "Dervish") && Items.HasItem(3137) &&
                Items.CanUseItem(3137) && target.IsMe)
            {
                Items.UseItem(3137, ObjectManager.Player);
            }
        }
        #endregion

        #region Utility Methods

        private static bool OneReady()
        {
            return (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "QSS") && Items.HasItem(3140) &&
                    Items.CanUseItem(3140)) ||
                   (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "Scimitar") && Items.HasItem(3139) &&
                    Items.CanUseItem(3139)) ||
                   (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "Dervish") && Items.HasItem(3137) &&
                    Items.CanUseItem(3137));
        }
        private static bool MichaelReady()
        {
            return (MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + "Michael") && Items.HasItem(0) &&
                    Items.CanUseItem(0)); //TODO Michael ID
        }
        private static bool BuffTypeEnabled(BuffType buffType)
        {
            return MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + buffType);
        }
        private static int SpellDelay(String sName)
        {
            return MenuHelper.getSliderValue(DZAIO.Player.ChampionName + sName + "D");
        }
        private static bool SpellEnabledOnKill(String sName)
        {
            return MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + sName + "K");
        }
        private static bool SpellEnabledAlways(String sName)
        {
            return MenuHelper.isMenuEnabled(DZAIO.Player.ChampionName + sName + "A");
        }

        private static Obj_AI_Hero GetChampByName(String EnemyName)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Find(h => h.IsEnemy && h.ChampionName == EnemyName);
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
