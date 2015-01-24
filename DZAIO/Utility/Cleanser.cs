using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DZAIO.Utility
{
    class Cleanser
    {
        //TODO Cleanser Rework class.
        //Lazy DZ Get to work already!
        private static List<QSSSpell> qssSpells = new List<QSSSpell>();

        public static void initList()
        {
            qssSpells.Add(new QSSSpell { ChampName = "Warwick", IsEnabled = true, SpellBuff = "InfiniteDuress", SpellName = "Warwick R", OnlyKill = false });
            qssSpells.Add(new QSSSpell { ChampName = "Zed", IsEnabled = true, SpellBuff = "zedulttargetmark", SpellName = "Zed R", OnlyKill = true });
            qssSpells.Add(new QSSSpell { ChampName = "Rammus", IsEnabled = true, SpellBuff = "PuncturingTaunt", SpellName = "Rammus E", OnlyKill = false });
            /** Danger Level 4 Spells*/
            qssSpells.Add(new QSSSpell { ChampName = "Skarner", IsEnabled = true, SpellBuff = "SkarnerImpale", SpellName = "Skaner R", OnlyKill = false });
            qssSpells.Add(new QSSSpell { ChampName = "Fizz", IsEnabled = true, SpellBuff = "FizzMarinerDoom", SpellName = "Fizz R", OnlyKill = false });
            qssSpells.Add(new QSSSpell { ChampName = "Galio", IsEnabled = true, SpellBuff = "GalioIdolOfDurand", SpellName = "Galio R", OnlyKill = false });
            qssSpells.Add(new QSSSpell { ChampName = "Malzahar", IsEnabled = true, SpellBuff = "AlZaharNetherGrasp", SpellName = "Malz R", OnlyKill = false });
            /** Danger Level 3 Spells*/
            qssSpells.Add(new QSSSpell { ChampName = "Zilean", IsEnabled = false, SpellBuff = "timebombenemybuff", SpellName = "Zilean Q", OnlyKill = true });
            qssSpells.Add(new QSSSpell { ChampName = "Vladimir", IsEnabled = false, SpellBuff = "VladimirHemoplague", SpellName = "Vlad R", OnlyKill = true });
            qssSpells.Add(new QSSSpell { ChampName = "Mordekaiser", IsEnabled = true, SpellBuff = "MordekaiserChildrenOfTheGrave", SpellName = "Morde R", OnlyKill = true });
            /** Danger Level 2 Spells*/
            qssSpells.Add(new QSSSpell { ChampName = "Poppy", IsEnabled = true, SpellBuff = "PoppyDiplomaticImmunity", SpellName = "Poppy R", OnlyKill = false });
        }
    }

    internal class QSSSpell
    {
        public String ChampName { get; set; }
        public String SpellName { get; set; }
        public String SpellBuff { get; set; }
        public bool IsEnabled { get; set; }
        public bool OnlyKill { get; set; }
    }
}
