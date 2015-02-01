using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace DZAIO.Utility
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

    }

}
