using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Champions
{
    class Jinx : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 950f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 950f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 425f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1100f) } //TODO Tweak this. It has 1000 range + 800 in cone
        };
        public void OnLoad(Menu menu)
        {
            Game.PrintChat("Jinx is still WIP. Please use PennyJinx :)");
        }

        public void RegisterEvents()
        {
            throw new NotImplementedException();
        }

        public void SetUpSpells()
        {
            throw new NotImplementedException();
        }
        public float getComboDamage(Obj_AI_Hero unit)
        {
            return _spells.Where(spell => spell.Value.IsReady()).Sum(spell => (float)DZAIO.Player.GetSpellDamage(unit, spell.Key));
        }
    }
}
