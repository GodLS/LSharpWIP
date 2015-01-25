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
        #region
        private static readonly List<QssSpell> QssSpells = new List<QssSpell>()
        {
            new QssSpell
            {
                ChampName = "Warwick",
                IsEnabled = true,
                SpellBuff = "InfiniteDuress",
                SpellName = "Warwick R",
                OnlyKill = false
            },
            new QssSpell
            {
                ChampName = "Zed",
                IsEnabled = true,
                SpellBuff = "zedulttargetmark",
                SpellName = "Zed R",
                OnlyKill = true
            },
            new QssSpell
            {
                ChampName = "Rammus",
                IsEnabled = true,
                SpellBuff = "PuncturingTaunt",
                SpellName = "Rammus E",
                OnlyKill = false
            },
            /** Danger Level 4 Spells*/
            new QssSpell
            {
                ChampName = "Skarner",
                IsEnabled = true,
                SpellBuff = "SkarnerImpale",
                SpellName = "Skaner R",
                OnlyKill = false
            },
            new QssSpell
            {
                ChampName = "Fizz",
                IsEnabled = true,
                SpellBuff = "FizzMarinerDoom",
                SpellName = "Fizz R",
                OnlyKill = false
            },
            new QssSpell
            {
                ChampName = "Galio",
                IsEnabled = true,
                SpellBuff = "GalioIdolOfDurand",
                SpellName = "Galio R",
                OnlyKill = false
            },
            new QssSpell
            {
                ChampName = "Malzahar",
                IsEnabled = true,
                SpellBuff = "AlZaharNetherGrasp",
                SpellName = "Malz R",
                OnlyKill = false
            },
            /** Danger Level 3 Spells*/
            new QssSpell
            {
                ChampName = "Zilean",
                IsEnabled = false,
                SpellBuff = "timebombenemybuff",
                SpellName = "Zilean Q",
                OnlyKill = true
            },
            new QssSpell
            {
                ChampName = "Vladimir",
                IsEnabled = false,
                SpellBuff = "VladimirHemoplague",
                SpellName = "Vlad R",
                OnlyKill = true
            },
            new QssSpell
            {
                ChampName = "Mordekaiser",
                IsEnabled = true,
                SpellBuff = "MordekaiserChildrenOfTheGrave",
                SpellName = "Morde R",
                OnlyKill = true
            },
            /** Danger Level 2 Spells*/
            new QssSpell
            {
                ChampName = "Poppy",
                IsEnabled = true,
                SpellBuff = "PoppyDiplomaticImmunity",
                SpellName = "Poppy R",
                OnlyKill = false
            }
        };
        #endregion

        public static void InitList()
        {
            
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
}
