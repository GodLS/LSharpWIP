using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility.Helpers
{
    class DrawHelper
    {
        public static void DrawSpellsRanges(Dictionary<SpellSlot, Spell> spells)
        {
            foreach (var spell in spells.Where(s => DZAIO.Config.Item("dzaio." + ObjectManager.Player.ChampionName.ToLowerInvariant() + ".drawing.draw" + MenuHelper.GetStringFromSpellSlot(s.Key)).GetValue<Circle>().Active))
            {
                var value = DZAIO.Config.Item("dzaio." + ObjectManager.Player.ChampionName.ToLowerInvariant() + ".drawing.draw" + MenuHelper.GetStringFromSpellSlot(spell.Key)).GetValue<Circle>();
                if (spell.Value.Range < 4000f)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position, spell.Value.Range,
                        spell.Value.IsReady() ? value.Color : Color.DarkRed);
                }
            }
        }
    }
}
