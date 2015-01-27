using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace DZAIO.Utility.Drawing
{
    /**
     * Full Credits to Hellsing
     * */
    public class DamageIndicator
    {
        private static bool _initialized = false;
        private const float BarWidth = 104;

        private static readonly Line Line = new Line(LeagueSharp.Drawing.Direct3DDevice) { Width = 9 };

        private static LeagueSharp.Common.Utility.HpBarDamageIndicator.DamageToUnitDelegate _damageToUnit;

        private static Vector2 BarOffset
        {
            get { return new Vector2(10, 20); }
        }

        private static ColorBGRA _colorBgra = new ColorBGRA(0x00, 0xFF, 0xFF, 90);
        private static System.Drawing.Color _color = System.Drawing.Color.Aqua;
        public static System.Drawing.Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _colorBgra = new ColorBGRA(value.R, value.G, value.B, 90);
            }
        }

        public static void Initialize(LeagueSharp.Common.Utility.HpBarDamageIndicator.DamageToUnitDelegate damageToUnit)
        {
            if (_initialized)
                return;

            // Apply needed field delegate for damage calculation
            _damageToUnit = damageToUnit;
            Color = System.Drawing.Color.Yellow;

            // Register event handlers
            LeagueSharp.Drawing.OnDraw += Drawing_OnDraw;
            LeagueSharp.Drawing.OnPreReset += Drawing_OnPreReset;
            LeagueSharp.Drawing.OnPostReset += Drawing_OnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += OnProcessExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            _initialized = true;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(u => u.IsValidTarget()))
            {
                // Get damage to unit
                var damage = _damageToUnit(unit);

                // Continue on 0 damage
                if (damage == 0)
                    continue;

                // Get remaining HP after damage applied in percent and the current percent of health
                var damagePercentage = ((unit.Health - damage) > 0 ? (unit.Health - damage) : 0) / unit.MaxHealth;
                var currentHealthPercentage = unit.Health / unit.MaxHealth;

                // Calculate start and end point of the bar indicator
                var startPoint = new Vector2((int)(unit.HPBarPosition.X + BarOffset.X + damagePercentage * BarWidth), (int)(unit.HPBarPosition.Y + BarOffset.Y) + 4);
                var endPoint = new Vector2((int)(unit.HPBarPosition.X + BarOffset.X + currentHealthPercentage * BarWidth) + 1, (int)(unit.HPBarPosition.Y + BarOffset.Y) + 4);

                // Draw the DirectX line
                Line.Begin();
                Line.Draw(new[] { startPoint, endPoint }, _colorBgra);
                Line.End();
            }
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            Line.OnLostDevice();
        }

        private static void Drawing_OnOnPostReset(EventArgs args)
        {
            Line.OnResetDevice();
        }

        private static void OnProcessExit(object sender, EventArgs eventArgs)
        {
            Line.Dispose();
        }
    }
}