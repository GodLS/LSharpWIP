using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace DZAIO.Champions
{
    class Caitlyn : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 1100f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 800f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 980f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 3000f) }
        };

        private Vector3 pos1;
        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "dzaio.caitlyn.combo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });

            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q,SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 30, 30, 5 });

            var skillOptionMenu = new Menu("Skill Options", "dzaio.caitlyn.combo.skilloptions");
            comboMenu.AddSubMenu(skillOptionMenu);

            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "dzaio.caitlyn.harass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.E }, new[] { true, true, false });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.E }, new[] { 30, 35, 20 });
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "dzaio.caitlyn.farm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.Q }, new[] { false });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.Q }, new[] { 35 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "dzaio.caitlyn.misc");
            {
                miscMenu.AddItem(new MenuItem("dzaio.caitlyn.antigp", "E Anti Gapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.caitlyn.interrupt", "W Interrupter").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.caitlyn.dashtomouse", "Dash to mouse").SetValue(new KeyBind("S".ToCharArray()[0],KeyBindType.Press)));
                miscMenu.AddItem(new MenuItem("dzaio.caitlyn.manualr", "Manual R").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
                miscMenu.AddItem(new MenuItem("dzaio.caitlyn.debug", "debug").SetValue(new KeyBind("K".ToCharArray()[0], KeyBindType.Toggle)));


            }
            miscMenu.AddHitChanceSelector();
            menu.AddSubMenu(miscMenu);
            var drawMenu = new Menu(cName + " - Drawings", "dzaio.caitlyn.drawing");
            drawMenu.AddDrawMenu(_spells, Color.Aquamarine);
            DZAIO.Config.Item("dzaio.caitlyn.debug").ValueChanged += Caitlyn_ValueChanged;
            menu.AddSubMenu(drawMenu);
        }

        void Caitlyn_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
          
        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsValidTarget(_spells[SpellSlot.E].Range) && _spells[SpellSlot.E].IsReady() && MenuHelper.isMenuEnabled("dzaio.caitlyn.antigp"))
            {
                if (HeroHelper.IsSafePosition(GetPositionAfterE(gapcloser.Sender.ServerPosition)))
                {
                    _spells[SpellSlot.E].CastOnUnit(gapcloser.Sender);
                }
            }
        }

        void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuHelper.isMenuEnabled("dzaio.caitlyn.interrupt") && _spells[SpellSlot.W].IsReady() && sender.IsValidTarget(_spells[SpellSlot.W].Range) && args.DangerLevel >= Interrupter2.DangerLevel.Medium)
            {
                _spells[SpellSlot.W].Cast(sender);
            }
        }

        public void SetUpSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.625f, 90f, 2200f, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.W].SetSkillshot(1.5f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.E].SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harrass();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
                default:
                    return;
            }
        }
        private void Combo()
        {
            var comboTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            var eqTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range + 505f, TargetSelector.DamageType.Physical);
            var rTarget = TargetSelector.GetTarget(_spells[SpellSlot.R].Range, TargetSelector.DamageType.Physical);
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo) && comboTarget.IsValidTarget(_spells[SpellSlot.Q].Range))
            {
                _spells[SpellSlot.Q].CastIfHitchanceEquals(comboTarget, MenuHelper.GetHitchance());
            }

            if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo) && comboTarget.IsValidTarget(_spells[SpellSlot.W].Range) &&
                (HeroHelper.IsEmpaired(comboTarget) || comboTarget.IsRecalling()))
            {
                _spells[SpellSlot.W].CastIfHitchanceEquals(comboTarget, MenuHelper.GetHitchance());
            }

            if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo))
            {
                if (HeroHelper.IsSafePosition(ObjectManager.Player.ServerPosition) && _spells[SpellSlot.R].GetDamage(rTarget) >= rTarget.Health + 25 &&
                    rTarget.CountAlliesInRange(450f) <= 1 && rTarget.CountEnemiesInRange(450f) <= 2 && rTarget.Distance(ObjectManager.Player) >= ObjectManager.Player.AttackRange)
                {
                    _spells[SpellSlot.R].CastOnUnit(rTarget);
                }
            }

            #region E Combos
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && !_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && eqTarget.IsValidTarget() && eqTarget.Distance(ObjectManager.Player) >= Orbwalking.GetRealAutoAttackRange(null))
            {

                var afterEPosition = GetPositionAfterE(WhereToEForPosition(eqTarget.ServerPosition));
                if (!HeroHelper.IsSafePosition(afterEPosition))
                {
                    return;
                }
                var delay = (int)Math.Ceiling(ObjectManager.Player.Distance(afterEPosition) / _spells[SpellSlot.E].Speed * 1000 + 100);
                var delayPrediction = Prediction.GetPrediction(eqTarget, delay);
                //E AA
                if (afterEPosition.Distance(eqTarget.ServerPosition) <= Orbwalking.GetRealAutoAttackRange(null) - 50 && ObjectManager.Player.GetAutoAttackDamage(eqTarget) >= eqTarget.Health + 20)
                {
                    _spells[SpellSlot.E].Cast(WhereToEForPosition(eqTarget.ServerPosition));
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(delay + Game.Ping/2f), Orbwalking.ResetAutoAttackTimer);
                    _orbwalker.ForceTarget(eqTarget);
                    
                    //DebugHelper.PrintDebug("Done E AA");
                    return;
                }
                //E Q
                PredictionOutput customPrediction = PredictionHelper.GetP(afterEPosition, _spells[SpellSlot.Q], eqTarget, false);
                if (_spells[SpellSlot.Q].IsKillable(eqTarget) && _spells[SpellSlot.Q].IsReady() &&
                    afterEPosition.Distance(eqTarget.ServerPosition) <= _spells[SpellSlot.Q].Range &&
                    !(afterEPosition.Distance(eqTarget.ServerPosition) <= Orbwalking.GetRealAutoAttackRange(null) &&
                      ObjectManager.Player.GetAutoAttackDamage(eqTarget) >= eqTarget.Health + 20))
                {
                    _spells[SpellSlot.E].Cast(WhereToEForPosition(eqTarget.ServerPosition));
                    LeagueSharp.Common.Utility.DelayAction.Add(delay, () => _spells[SpellSlot.Q].Cast(customPrediction.CastPosition));
                }
            }
            #endregion
        }

        private void Harrass()
        {

        }

        private void Farm()
        {

        }

        void Drawing_OnDraw(EventArgs args)
        {
           // Render.Circle.DrawCircle(GetPositionAfterE(Game.CursorPos), 100f, System.Drawing.Color.DarkRed);
               
          // Render.Circle.DrawCircle(WhereToEForPosition(Game.CursorPos),100f,System.Drawing.Color.Blue);
            var eqTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range + _spells[SpellSlot.E].Range, TargetSelector.DamageType.Physical);
            if(eqTarget.IsValidTarget() && (ObjectManager.Player.GetAutoAttackDamage(eqTarget) >= eqTarget.Health + 20))
            {
                Render.Circle.DrawCircle(eqTarget.Position,65f,Color.DarkRed,20);
            }
           
        }

        private Vector3 GetPositionAfterE(Vector3 eCastPosition)
        {
            return eCastPosition.Extend(
                ObjectManager.Player.ServerPosition,
                eCastPosition.Distance(ObjectManager.Player.ServerPosition) + 505f);
        }
        private Vector3 WhereToEForPosition(Vector3 finalPosition)
        {
            return finalPosition.Extend(
                ObjectManager.Player.ServerPosition,
                finalPosition.Distance(ObjectManager.Player.ServerPosition) + 250f);
        }
        private Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
