using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public class ScopeSprite
        {
            private readonly Render.Sprite _sprite;
            private readonly Render.Text _KillableText;

            private static readonly Font _font = new Font(
            Drawing.Direct3DDevice,
            new FontDescription
            {
               FaceName = "Calibri",
               Height = 15,
               OutputPrecision = FontPrecision.Default,
               Quality = FontQuality.Default,
            });

            private Obj_AI_Hero hero
            {
                get
                {
                    //return ObjectManager.Player;
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

            private Vector2 _pos
            {
                get {
                    var Condition = (hero != null && PennyJinx.IsMenuEnabled("SpriteDraw") && PennyJinx._r.IsReady());
                    return Condition?new Vector2(Drawing.WorldToScreen(hero.Position).X-hero.BoundingRadius*2+hero.BoundingRadius/2.5f, Drawing.WorldToScreen(hero.Position).Y-hero.BoundingRadius*2):Drawing.WorldToScreen(new Vector2(-200, -200).To3D()); 
                }
            }

            private Vector2 _TextPos
            {
                get
                {
                    return  Drawing.WorldToScreen(new Vector2(_pos.X,_pos.Y+25).To3D());
                }
            }

            private String getHP
            {
                get
                {
                    var Condition = (hero != null && PennyJinx.IsMenuEnabled("SpriteDraw") && PennyJinx._r.IsReady());
                    return Condition?"Killable! " + hero.Health + " HP":"Error getting HP";
                }
            }
            //Constructor
            public ScopeSprite()
            {
                var Condition = Render.OnScreen(_pos) && (hero != null && PennyJinx.IsMenuEnabled("SpriteDraw") && PennyJinx._r.IsReady());

                _sprite = new Render.Sprite(Properties.Resources.scope, new Vector2(0, 0))
                {
                    VisibleCondition = s => Condition,
                    PositionUpdate =
                        () => _pos                   
                };
                

                
                _sprite.Scale = new Vector2(0.65f, 0.65f);
                _sprite.Add(0);
                Drawing.OnDraw += Drawing_OnDraw;
                Drawing.OnEndScene += Drawing_OnEndScene;
                Drawing.OnPreReset += Drawing_OnPreReset;
                Drawing.OnPostReset += Drawing_OnPostReset;
                AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
                AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;

            }

            private void CurrentDomainOnDomainUnload(object sender, EventArgs e)
            {
                _sprite.Dispose();
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
        }
        
    }
}