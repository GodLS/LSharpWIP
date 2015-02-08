using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility.Helpers
{
    static class MenuHelper
    {
        public static bool IsEnabledAndReady(this Spell spell, Mode mode)
        {
            if (DZAIO.Player.IsDead)
                return false;
            try
            {
                var manaPercentage = getSliderValue("dzaio.champion." + DZAIO.Player.ChampionName.ToLowerInvariant() + ".manamanager." + GetStringFromSpellSlot(spell.Slot).ToLowerInvariant() + "mana" + GetStringFromMode(mode).ToLowerInvariant());
                var enabledCondition = isMenuEnabled("dzaio.champion." + DZAIO.Player.ChampionName.ToLowerInvariant() + ".use" + GetStringFromSpellSlot(spell.Slot).ToLowerInvariant() + GetStringFromMode(mode));
                return spell.IsReady() && (ObjectManager.Player.ManaPercentage() >= manaPercentage) && enabledCondition;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return false;
        }

        public static void AddManaManager(this Menu menu, Mode mode, SpellSlot[] spellList, int[] manaCosts)
        {
            var mmMenu = new Menu("Mana Manager", "dzaio.champion."+DZAIO.Player.ChampionName.ToLowerInvariant()+".mm." + GetStringFromMode(mode));
            for (var i = 0; i < spellList.Count(); i++)
            {
                mmMenu.AddItem(
                    new MenuItem(
                        "dzaio.champion."+DZAIO.Player.ChampionName.ToLowerInvariant()+".manamanager."+GetStringFromSpellSlot(spellList[i]).ToLowerInvariant() + "mana" + GetStringFromMode(mode).ToLowerInvariant(),
                        GetStringFromSpellSlot(spellList[i]) + " Mana").SetValue(new Slider(manaCosts[i])));
            }
            menu.AddSubMenu(mmMenu);
        }

        public static void AddModeMenu(this Menu menu, Mode mode, SpellSlot[] spellList, bool[] values)
        {
            for (var i = 0; i < spellList.Count(); i++)
            {
                menu.AddItem(
                    new MenuItem(
                        "dzaio.champion."+DZAIO.Player.ChampionName.ToLowerInvariant()+".use" + GetStringFromSpellSlot(spellList[i]).ToLowerInvariant() + GetStringFromMode(mode),
                        "Use " + GetStringFromSpellSlot(spellList[i]) + " " + GetFullNameFromMode(mode)).SetValue(values[i]));
            }
        }

        public static void AddDrawMenu(this Menu menu, Dictionary<SpellSlot,Spell> dictionary,Color myColor)
        {
            foreach (var entry in dictionary)
            {
                var slot = entry.Key;
                if (entry.Value.Range < 4000f)
                {
                    menu.AddItem(new MenuItem("dzaio."+ObjectManager.Player.ChampionName.ToLowerInvariant() + ".drawing.draw" + GetStringFromSpellSlot(slot), "Draw " + GetStringFromSpellSlot(slot)).SetValue(new Circle(true, myColor)));
                }
            }
        }

        public static void AddHitChanceSelector(this Menu menu)
        {
            menu.AddItem(
                    new MenuItem("dzaio.champion.customhitchance", "Hitchance").SetValue(
                        new StringList(new[] { "Low", "Medium", "High", "Very High" }, 2)));
        }

        public static void AddNoUltiMenu(this Menu menu,bool allies)
        {
            var _menu = new Menu("Don't ult", "dzaio.champion."+DZAIO.Player.ChampionName.ToLowerInvariant()+".noult");
            foreach (var player in ObjectManager.Get<Obj_AI_Hero>().Where(h => !h.IsMe && allies ? h.IsAlly : h.IsEnemy))
            {
                _menu.AddItem(new MenuItem("dzaio.champion."+DZAIO.Player.ChampionName.ToLowerInvariant()+".noult."+player.ChampionName, player.ChampionName).SetValue(false));
            }
            menu.AddSubMenu(_menu);
        }

        public static bool isMenuEnabled(String item)
        {
            var startString = item.StartsWith("Use") ? DZAIO.Player.ChampionName : "";
            return DZAIO.Config.Item(startString+item).GetValue<bool>();
        }

        public static int getSliderValue(String item)
        {
            return DZAIO.Config.Item(item) != null ?DZAIO.Config.Item(item).GetValue<Slider>().Value:-1;
        }

        public static bool getKeybindValue(String item)
        {
            return DZAIO.Config.Item(item).GetValue<KeyBind>().Active;
        }

        public static HitChance GetHitchance()
        {
            switch (DZAIO.Config.Item("dzaio.champion.customhitchance").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.Medium;
            }
        }

        public static String GetStringFromSpellSlot(SpellSlot sp)
        {
            //TODO Test if this works
            //return sp.ToString();
            switch (sp)
            {
                case SpellSlot.Q:
                    return "Q";
                case SpellSlot.W:
                    return "W";
                case SpellSlot.E:
                    return "E";
                case SpellSlot.R:
                    return "R";
                default:
                    return "unk";
            }
        }
        static String GetStringFromMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.Combo:
                    return "C";
                case Mode.Harrass:
                    return "H";
                case Mode.Lasthit:
                    return "LH";
                case Mode.Laneclear:
                    return "LC";
                case Mode.Farm:
                    return "F";
                default:
                    return "unk";
            }
        }
        static String GetFullNameFromMode(Mode mode)
        {
            return mode.ToString();
        }
    }

    enum Mode
    {
        Combo,
        Harrass,
        Lasthit,
        Laneclear,
        Farm
    }
}
