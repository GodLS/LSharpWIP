using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility
{
    static class DZUtility
    {

        //Utility Methods will go here
        public static bool IsEnabledAndReady(this Spell spell, Mode mode)
        {
            if (DZAIO.Player.IsDead)
                return false;

            try
            {
                var mana = getSliderValue(getStringFromSpellSlot(spell.Slot) + "Mana" + getStringFromMode(mode));
                var isEn = isMenuEnabled("Use" + getStringFromSpellSlot(spell.Slot) + getStringFromMode(mode));
                return spell.IsReady() && (ObjectManager.Player.ManaPercentage() >= mana) && isEn;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return false;
        }

        public static void addManaManager(this Menu menu, Mode mode, SpellSlot[] spellList, int[] ManaCosts)
        {
            var mm_Menu = new Menu("Mana Manager", "mm_" + getStringFromMode(mode));
            for (int i = 0; i < spellList.Count(); i++)
            {
                mm_Menu.AddItem(
                    new MenuItem(
                        getStringFromSpellSlot(spellList[i]) + "Mana" + getStringFromMode(mode),
                        getStringFromSpellSlot(spellList[i]) + " Mana").SetValue(new Slider(ManaCosts[i])));
            }
            menu.AddSubMenu(mm_Menu);
        }
        public static void addModeMenu(this Menu menu, Mode mode, SpellSlot[] spellList, bool[] values)
        {
            for (int i = 0; i < spellList.Count(); i++)
            {
                menu.AddItem(
                    new MenuItem(
                        "Use" + getStringFromSpellSlot(spellList[i]) + getStringFromMode(mode),
                        "Use " + getStringFromSpellSlot(spellList[i]) + " " + getFullNameFromMode(mode)).SetValue(values[i]));
            }
        }

        public static void addHitChanceSelector(this Menu menu)
        {
            menu.AddItem(
                    new MenuItem("C_Hit", "Hitchance").SetValue(
                        new StringList(new[] { "Low", "Medium", "High", "Very High" }, 2)));
        }

        public static bool isMenuEnabled(String item)
        {
            return DZAIO.Config.Item(item).GetValue<bool>();
        }

        public static int getSliderValue(String item)
        {
            return DZAIO.Config.Item(item).GetValue<Slider>().Value;
        }

        public static bool getKeybindValue(String item)
        {
            return DZAIO.Config.Item(item).GetValue<KeyBind>().Active;
        }

        public static HitChance GetHitchance()
        {
            switch (DZAIO.Config.Item("C_Hit").GetValue<StringList>().SelectedIndex)
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

        static String getStringFromSpellSlot(SpellSlot sp)
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
        static String getStringFromMode(Mode mode)
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
        static String getFullNameFromMode(Mode mode)
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
