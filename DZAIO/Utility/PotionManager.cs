using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility
{
    internal class PotionManager
    {
        private static float _lastCheckTick;
        private static readonly List<Potion> Potions = new List<Potion>
        {
            new Potion
            {
                Name = "Health Potion",
                BuffName = "RegenerationPotion",
                ItemId = (ItemId)2003,
                Type =  PotionType.Health,
                Priority = 2
            },
            new Potion
            {
                Name = "Mana Potion",
                BuffName = "FlaskOfCrystalWater",
                ItemId = (ItemId)2004,
                Type =  PotionType.Mana,
                Priority = 2
            },
            new Potion
            {
                Name = "Crystal Flask",
                BuffName = "ItemCrystalFlask",
                ItemId = (ItemId)2041,
                Type =  PotionType.Flask,
                Priority = 3
            },
            new Potion
            {
                Name = "Biscuit",
                BuffName = "ItemMiniRegenPotion",
                ItemId = (ItemId)2010,
                Type =  PotionType.Flask,
                Priority = 1
            },
        };

        //TODO Potion manager _menu here
        public static void OnLoad(Menu menu)
        {
            AddMenu(DZAIO.Config);
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Environment.TickCount - _lastCheckTick < 80)
                return;
            _lastCheckTick = Environment.TickCount;
            UsePotion();
        }

        private static void UsePotion()
        {

            if (DZAIO.Player.IsDead || DZAIO.Player.IsRecalling() || DZAIO.Player.InFountain() || DZAIO.Player.InShop())
                return;
            
            if (!HealthBuff() && DZAIO.Player.HealthPercentage() < MenuHelper.getSliderValue(DZAIO.Player.ChampionName + "minHP"))
            {
                var hpSlot = GetHpSlot();

                if (hpSlot != SpellSlot.Unknown && hpSlot.IsReady())
                {
                    DZAIO.Player.Spellbook.CastSpell(hpSlot, DZAIO.Player);
                    return;
                }
            }
            if (!ManaBuff() && DZAIO.Player.ManaPercentage() < MenuHelper.getSliderValue(DZAIO.Player.ChampionName + "minMana"))
            {
                var manaSlot = GetManaSlot();
                if (manaSlot != SpellSlot.Unknown && manaSlot.IsReady())
                {
                    DZAIO.Player.Spellbook.CastSpell(manaSlot, DZAIO.Player);
                }
            }
        }

        private static void AddMenu(Menu menu)
        {
            var cName = DZAIO.Player.ChampionName;
            var potMenu = new Menu(cName + " - Potion Manager", DZAIO.Player.ChampionName+"PotM");
            var potItems = new Menu("Potions", DZAIO.Player.ChampionName + "Pots");
            foreach (var potion in Potions)
            {
                potItems.AddItem(new MenuItem(((int)potion.ItemId).ToString(),potion.Name).SetValue(true));
            }
            potMenu.AddSubMenu(potItems);
            potMenu.AddItem(new MenuItem(DZAIO.Player.ChampionName + "minHP", "Min Health %").SetValue(new Slider(30)));
            potMenu.AddItem(new MenuItem(DZAIO.Player.ChampionName + "minMana", "Min Mana %").SetValue(new Slider(35)));
            menu.AddSubMenu(potMenu);
        }

        private static bool ManaBuff()
        {
            return Potions.Any(pot => (pot.Type == PotionType.Mana || pot.Type == PotionType.Flask) && pot.IsRunning);
        }

        private static bool HealthBuff()
        {
            return Potions.Any(pot => (pot.Type == PotionType.Health || pot.Type == PotionType.Flask) && pot.IsRunning);
        }

        private static SpellSlot GetHpSlot()
        {
            var ordered = Potions.Where(p => p.Type == PotionType.Health || p.Type == PotionType.Flask).OrderByDescending(pot => pot.Priority);
            var potSlot = SpellSlot.Unknown;
            var lastPriority = ordered.First().Priority;
            
            foreach (
                var Item in
                    ObjectManager.Player.InventoryItems.Where(
                        item =>
                            GetHpIds().Contains((int)item.Id) &&
                            MenuHelper.isMenuEnabled(((int)item.Id).ToString())))
            {
                var currentPriority = Potions.First(it => it.ItemId == Item.Id).Priority;
                if (currentPriority <= lastPriority)
                {
                    potSlot = Item.SpellSlot;
                }
            }
            return potSlot;
        }


        private static SpellSlot GetManaSlot()
        {
            var ordered = Potions.Where(p => p.Type == PotionType.Health || p.Type == PotionType.Flask).OrderByDescending(pot => pot.Priority);
            var potSlot = SpellSlot.Unknown;
            var lastPriority = ordered.First().Priority;

            foreach (
                var Item in
                    ObjectManager.Player.InventoryItems.Where(
                        item =>
                            GetManaIds().Contains((int)item.Id) &&
                            MenuHelper.isMenuEnabled(((int)item.Id).ToString())))
            {
                var currentPriority = Potions.First(it => it.ItemId == Item.Id).Priority;
                if (currentPriority <= lastPriority)
                {
                    potSlot = Item.SpellSlot;
                }
            }
            return potSlot;
        }

        private static List<int> GetHpIds()
        {
            var HPIds = new List<int>();
            foreach (var pot in Potions)
            {
                if (pot.Type == PotionType.Health || pot.Type == PotionType.Flask && Items.HasItem((int)pot.ItemId))
                {
                    HPIds.Add((int)pot.ItemId);
                }
            }
            return HPIds;
        }

        private static List<int> GetManaIds()
        {
            var ManaIds = new List<int>();
            foreach (var pot in Potions)
            {
                if (pot.Type == PotionType.Mana || pot.Type == PotionType.Flask && Items.HasItem((int)pot.ItemId))
                {
                    ManaIds.Add((int)pot.ItemId);
                }
            }
            return ManaIds;
        }
    }


    class Potion
    {
        public String Name { get; set; }
        public PotionType Type { get; set; }
        public String  BuffName { get; set; }
        public ItemId ItemId { get; set; }
        public int Priority { get; set; }
        public bool IsRunning
        {
            get { return ObjectManager.Player.HasBuff(BuffName, true); }
        }
    }

    enum PotionType
    {
        Health,
        Mana,
        Flask
    }
}
