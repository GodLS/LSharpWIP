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
        public static SharpDX.Direct3D9.Texture taco;
        public static SharpDX.Direct3D9.Sprite sprite;
        public static SharpDX.Direct3D9.Device dxDevice = Drawing.Direct3DDevice;
        public static int i = 0;

        public static void Game_OnGameLoad(EventArgs args)
        {
            sprite = new Sprite(dxDevice);
            taco = Texture.FromMemory(
                     Drawing.Direct3DDevice,
                     (byte[])new ImageConverter().ConvertTo(LoadPicture("http://puu.sh/e6NdL/f485b348f4.png"), typeof(byte[])), 70, 70, 0,
                     Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs e)
        {
            sprite.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            sprite.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            sprite.OnLostDevice();
        }
        private static Bitmap LoadPicture(string url)
        {
            System.Net.WebRequest request = System.Net.WebRequest.Create(url);
            System.Net.WebResponse response = request.GetResponse();
            System.IO.Stream responseStream = response.GetResponseStream();
            Bitmap bitmap2 = new Bitmap(responseStream);
            Console.WriteLine(bitmap2.Size);
            return (bitmap2);
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            DrawSprite();
        }

        static void DrawSprite()
        {
            sprite.Begin();
            sprite.Draw(taco, new ColorBGRA(255, 255, 255, 255), null, new Vector3(Drawing.WorldToScreen(ObjectManager.Player.Position).X, Drawing.WorldToScreen(ObjectManager.Player.Position).Y, 0));
            sprite.End();
            if (!PennyJinx.IsMenuEnabled("SpriteDraw"))
                return;
            foreach (
                var hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            h =>
                                h.IsValidTarget(PennyJinx._r.Range) &&
                                PennyJinx._r.GetDamage(h) >=
                                HealthPrediction.GetHealthPrediction(h, (int) (ObjectManager.Player.Distance(h) / 2000f) * 1000)))
            {
                sprite.Begin();
                sprite.Draw(taco, new ColorBGRA(255, 255, 255, 255), null, new Vector3(Drawing.WorldToScreen(hero.Position).X, Drawing.WorldToScreen(hero.Position).Y,0));
                sprite.End();
            }
        
        }
    }
}