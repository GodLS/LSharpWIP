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
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => !hero.IsMe && hero.IsValidTarget(range, true, Game.CursorPos)).ToList();
        }
        public static List<Obj_AI_Hero> GetLhEnemiesNearMouse(float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => !hero.IsMe && hero.Distance(Game.CursorPos, true) <= range * range && hero.IsValidTarget(range, true, Game.CursorPos) && hero.HealthPercentage() <= 15).ToList();
        }

        public static List<Obj_AI_Hero> GetAlliesNearMouse(float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => !hero.IsMe && hero.IsAlly && hero.IsValidTarget(range, false, Game.CursorPos)).ToList();
        }

        public static List<Obj_AI_Hero> GetLhAlliesNearMouse(float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => !hero.IsMe && hero.IsAlly && hero.IsValidTarget(range, false, Game.CursorPos) && hero.HealthPercentage() <= 15).ToList();
        }

        public static List<Obj_AI_Hero> GetLhEnemiesNearPosition(Vector3 Position, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => !hero.IsMe && hero.IsAlly && hero.IsValidTarget(range, true, Position) && hero.HealthPercentage() <= 15).ToList();
        }

    }

}
