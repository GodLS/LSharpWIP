using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace PennyJinx
{
    class SpriteManager
    {
        public class KillableHero
        {
            private readonly Render.Sprite _sprite;
            private readonly Obj_AI_Hero _hero;
            private bool _active;
            private bool _Killable;
            public KillableHero(Obj_AI_Hero hero)
            {
                _active = PennyJinx.IsMenuEnabled("SpriteDraw");
                if (!_active)
                    return;
                _Killable = false;
                _hero = hero;
                _sprite = new Render.Sprite(Properties.Resources.scope, new Vector2(0, 0))
                {
                    VisibleCondition = sender => _Killable,
                    PositionUpdate =
                        () =>
                            new Vector2(Drawing.WorldToScreen(hero.Position).X-65, Drawing.WorldToScreen(hero.Position).Y-75)
                            
                };
                _sprite.Scale = new Vector2(0.60f, 0.60f);
                _sprite.Add(0);
                Game.OnGameUpdate += Game_OnGameUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
                Drawing.OnEndScene += Drawing_OnEndScene;
                Drawing.OnPreReset += Drawing_OnPreReset;
                Drawing.OnPostReset += Drawing_OnPostReset;
            }

            void Drawing_OnPostReset(EventArgs args)
            {
               _sprite.OnPostReset();
            }

            void Drawing_OnPreReset(EventArgs args)
            {
                _sprite.OnPreReset();
            }

            void Drawing_OnEndScene(EventArgs args)
            {
               _sprite.OnEndScene();
            }

            void Drawing_OnDraw(EventArgs args)
            {
                _sprite.OnDraw();
            }

            private void Game_OnGameUpdate(EventArgs args)
            {
              
                if (_hero.IsValidTarget(PennyJinx._r.Range) &&
                    PennyJinx._r.GetDamage(_hero) >=
                    HealthPrediction.GetHealthPrediction(
                        _hero, (int) (ObjectManager.Player.Distance(_hero) / 2000f) * 1000))
                {
                    _Killable = true;
                }
                else
                {
                    _Killable = false;
                }
            } 
        }
        
    }
}