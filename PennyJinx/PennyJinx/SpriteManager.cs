﻿using System;
using System.Collections.Generic;
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

            private Obj_AI_Hero hero
            {
                get
                {
                    var HList = ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(PennyJinx._r.Range) &&
                                    PennyJinx._r.GetDamage(hero) >=
                                    HealthPrediction.GetHealthPrediction(
                                        hero, (int) (ObjectManager.Player.Distance(hero) / 2000f) * 1000))
                            .OrderBy(ph => ph.HealthPercentage()).ToList();
                    if (!HList.Any())
                        return null;
                    return HList.First();

                }
            }
            private bool _active {
                get { return PennyJinx.IsMenuEnabled("SpriteDraw"); }
                }
            public KillableHero()
            {
                
                _sprite = new Render.Sprite(Properties.Resources.scope, new Vector2(0, 0))
                {
                    VisibleCondition = s => (hero != null && PennyJinx.IsMenuEnabled("SpriteDraw") && PennyJinx._r.IsReady()),
                    PositionUpdate =
                        () =>
                            new Vector2(Drawing.WorldToScreen(hero.Position).X-105, Drawing.WorldToScreen(hero.Position).Y-105)
                            
                };
                _sprite.Scale = new Vector2(0.65f, 0.65f);
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
                
            } 
        }
        
    }
}