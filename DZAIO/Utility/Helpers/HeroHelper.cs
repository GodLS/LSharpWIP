using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace DZAIO.Utility.Helpers
{
    class HeroHelper
    {
        public static List<Obj_AI_Hero> GetEnemiesNearMouse(float range)
        {
            return HeroManager.Enemies.Where(hero => hero.IsValidTarget(range, true, Game.CursorPos) && hero.HealthPercentage() <= 15).ToList();
        }
        public static List<Obj_AI_Hero> GetLhEnemiesNearMouse(float range)
        {
            return HeroManager.Enemies.Where(hero => hero.IsValidTarget(range, true, Game.CursorPos) && hero.HealthPercentage() <= 15).ToList();
        }

        public static List<Obj_AI_Hero> GetAlliesNearMouse(float range)
        {
            return HeroManager.Allies.Where(hero => hero.IsValidTarget(range, false, Game.CursorPos)).ToList();
        }

        public static List<Obj_AI_Hero> GetLhAlliesNearMouse(float range)
        {
            return HeroManager.Allies.Where(hero => !hero.IsMe && hero.IsAlly && hero.IsValidTarget(range, false, Game.CursorPos) && hero.HealthPercentage() <= 15).ToList();
                    
        }

        public static List<Obj_AI_Hero> GetLhEnemiesNearPosition(Vector3 position, float range)
        {
            return HeroManager.Enemies.Where(hero => hero.IsValidTarget(range, true, position) && hero.HealthPercentage() <= 15).ToList();
        }

        public static float GetComboDamage(Dictionary<SpellSlot,Spell> spells,Obj_AI_Hero unit)
        {
            if (!unit.IsValidTarget())
                return 0;
            return spells.Where(spell => spell.Value.IsReady()).Sum(spell => (float) DZAIO.Player.GetSpellDamage(unit, spell.Key)) + (float)DZAIO.Player.GetAutoAttackDamage(unit)*2 + ItemManager.GetItemsDamage(unit);
        }

        public static bool IsUnderAllyTurret(Obj_AI_Hero target)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Where(t => !t.IsDead && t.IsAlly).Any(turret => target.Distance(turret.Position) <= 975f);
        }
        public static bool IsUnderEnemyTurret(Obj_AI_Hero target)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Where(t => !t.IsDead && t.IsEnemy).Any(turret => target.Distance(turret.Position) <= 975f);
        }
    }

}
