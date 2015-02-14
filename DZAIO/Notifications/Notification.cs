using System;
using System.IO;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace Notifications
{
    public class Notification : INotification
    {
        public ColorBGRA BorderColor = new ColorBGRA(255f, 255f, 255f, 255f);
        public ColorBGRA BoxColor = new ColorBGRA(0f, 0f, 0f, 255f);
        private int decreasementTick;
        public bool Draw;
        public Font Font;
        private FileStream locationHandler;
        private Vector2 position;
        public string Text;
        public ColorBGRA TextColor = new ColorBGRA(255f, 255f, 255f, 255f);
        public bool Update;
        private Vector2 updatePosition = Vector2.Zero;
        private readonly int duration;
        private readonly string id;
        private readonly Line line;
        private readonly Sprite sprite;

        public Notification(string text, int duration, bool draw = true, bool update = true)
        {
            id = Guid.NewGuid().ToString("N");
            Draw = draw;
            Update = update;
            Text = text;

            line = new Line(Drawing.Direct3DDevice) { Antialias = false, GLLines = true, Width = 190f };
            sprite = new Sprite(Drawing.Direct3DDevice);
            Font = new Font(
                Drawing.Direct3DDevice, 0xe, 0x0, FontWeight.DoNotCare, 0x0, false, FontCharacterSet.Default,
                FontPrecision.Default, FontQuality.Antialiased,
                FontPitchAndFamily.DontCare | FontPitchAndFamily.Decorative, "Tahoma");

            this.duration = duration;
            if (duration > 0)
            {
                decreasementTick = GetNextDecreasementTick();
            }

            if (draw)
            {
                var yAxis = Notifications.GetLocation();
                if (yAxis != -1)
                {
                    locationHandler = Notifications.Reserve(GetId());
                    if (locationHandler != null)
                    {
                        position = new Vector2(Drawing.Width - 200f, yAxis);
                    }
                }
                else
                {
                    Draw = false;
                    Update = false;
                }
            }
        }

        public void OnDraw()
        {
            if (!Draw)
            {
                return;
            }

            #region Box

            line.Begin();
            var vertices = new[]
            { new Vector2(position.X + 190f / 0x2, position.Y), new Vector2(position.X + 190f / 0x2, position.Y + 25f) };
            line.Draw(vertices, BoxColor);
            line.End();

            #endregion

            #region Outline

            var x = position.X;
            var y = position.Y;
            const float w = 190f;
            const float h = 25f;
            const float px = 1f;

            line.Begin();
            line.Draw(GetBorder(x, y, w, px), BorderColor); // TOP
            line.End();

            var oWidth = line.Width;
            line.Width = px;

            line.Begin();
            line.Draw(GetBorder(x, y, px, h), BorderColor); // LEFT
            line.Draw(GetBorder(x + w, y, 1, h), BorderColor); // RIGHT
            line.End();

            line.Width = oWidth;

            line.Begin();
            line.Draw(GetBorder(x, y + h, w, 1), BorderColor); // BOTTOM
            line.End();

            #endregion

            #region Text

            sprite.Begin();

            var textDimension = Font.MeasureText(sprite, Text, 0x0);
            var rectangle = new Rectangle((int) position.X, (int) position.Y, 0xbe, 0x19);

            Font.DrawText(
                sprite, Text, rectangle.TopLeft.X + (rectangle.Width - textDimension.Width) / 0x2,
                rectangle.TopLeft.Y + (rectangle.Height - textDimension.Height) / 0x2, TextColor);

            sprite.End();

            #endregion
        }

        public void OnUpdate()
        {
            if (!Update)
            {
                return;
            }

            if (updatePosition == Vector2.Zero)
            {
                if (TextColor.A == 0 && BoxColor.A == 0 && BorderColor.A == 0)
                {
                    Draw = false;
                    Update = false;
                    Notifications.Free(locationHandler);
                    return;
                }

                if (duration > 0 && Environment.TickCount - decreasementTick > 0)
                {
                    if (TextColor.A > 0)
                    {
                        TextColor.A--;
                    }
                    if (BoxColor.A > 0)
                    {
                        BoxColor.A--;
                    }
                    if (BorderColor.A > 0)
                    {
                        BorderColor.A--;
                    }
                    decreasementTick = GetNextDecreasementTick();
                }

                var location = Notifications.GetLocation();
                if (location != -1 && position.Y > location)
                {
                    var b = Notifications.Reserve(GetId());
                    if (b != null)
                    {
                        Notifications.Free(locationHandler);
                        locationHandler = b;
                        updatePosition = new Vector2(position.X, location);
                    }
                }
            }
            else
            {
                if (Math.Abs(position.Y - updatePosition.Y) > float.Epsilon)
                {
                    var value = (updatePosition.Distance(new Vector2(position.X, position.Y - 0x1)) <
                                 updatePosition.Distance(new Vector2(position.X, position.Y + 0x1)))
                        ? -1
                        : 1;
                    position.Y += value;
                }
                else
                {
                    updatePosition = Vector2.Zero;
                }
            }
        }

        public string GetId()
        {
            return id;
        }

        private static Vector2[] GetBorder(float x, float y, float w, float h)
        {
            return new[] { new Vector2(x + w / 0x2, y), new Vector2(x + w / 0x2, y + h) };
        }

        private int GetNextDecreasementTick()
        {
            return Environment.TickCount + ((duration / 255));
        }
    }
}