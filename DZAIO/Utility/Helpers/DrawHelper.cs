using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility.Helpers
{
    class DrawHelper
    {
        public static void DrawSpellsRanges(Dictionary<SpellSlot, Spell> spells)
        {
            foreach (var spell in spells.Where(s => DZAIO.Config.Item(DZAIO.Player.ChampionName + "Draw" + MenuHelper.GetStringFromSpellSlot(s.Key)).GetValue<Circle>().Active))
            {
                var value = DZAIO.Config.Item(DZAIO.Player.ChampionName + "Draw" + MenuHelper.GetStringFromSpellSlot(spell.Key)).GetValue<Circle>();
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Value.Range, value.Color);
            }
        }
    }
}
