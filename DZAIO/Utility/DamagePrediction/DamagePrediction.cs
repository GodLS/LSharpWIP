using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility.DamagePrediction
{
    internal class DamagePrediction
    {
        //TODO Damage Prediction Event

        public delegate void OnKillableDelegate(Obj_AI_Hero sender,Obj_AI_Hero target);
        public static event OnKillableDelegate OnSpellWillKill;

        static DamagePrediction()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!(sender is Obj_AI_Hero) || !(args.Target is Obj_AI_Hero))
                return;
            var sender_H = sender as Obj_AI_Hero;
            var target_H = args.Target as Obj_AI_Hero;
            var damage = getDamage(sender_H,target_H, sender_H.GetSpellSlot(args.SData.Name));

            DebugHelper.AddEntry("Damage to "+target_H.ChampionName,damage.ToString());

            if (damage > target_H.Health + 20)
            {
                if (OnSpellWillKill != null)
                {
                    OnSpellWillKill(sender_H, target_H);
                }
            }
        }

        static float getDamage(Obj_AI_Hero hero,Obj_AI_Hero target,SpellSlot Slot)
        {
            return (float)hero.GetSpellDamage(target, Slot);
        }
    }
}
