using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using DZAIO.Utility.Drawing;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace DZAIO.Champions
{
    class Graves : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 800f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 950f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 425f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1100f) } //TODO Tweak this. It has 1000 range + 800 in cone
        };

        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "dzaio.graves.combo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });
            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });
            var comboOptions = new Menu("Skills Options", "dzaio.graves.combo.skilloptions");
            {
                comboOptions.AddItem(new MenuItem("dzaio.graves.combo.minwenemiesc", "Only W if hit x enemies").SetValue(new Slider(2, 1, 5)));
                comboOptions.AddItem(new MenuItem("dzaio.graves.combo.emaxrange", "E Distance").SetValue(new Slider(350, 1, 425)));
                comboOptions.AddItem(new MenuItem("dzaio.graves.combo.ecancel", "Use E to cancel Q & R animation").SetValue(true));
            }
            comboMenu.AddSubMenu(comboOptions);
            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "dzaio.graves.harass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { 30, 35 });
            harrassMenu.AddItem(new MenuItem("dzaio.graves.harass.minwenemiesh", "Only W if hit x enemies").SetValue(new Slider(2, 1, 5)));
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "dzaio.graves.farm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.Q }, new[] { true });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.Q }, new[] { 40 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "GravesMisc");
            {
                miscMenu.AddItem(new MenuItem("dzaio.graves.misc.antigpw", "W AntiGapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.graves.misc.antigpe", "E AntiGapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.graves.misc.manualr", "Manual R").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            }
            miscMenu.AddHitChanceSelector();
            menu.AddSubMenu(miscMenu);

            var drawMenu = new Menu(cName + " - Drawings", "dzaio.graves.drawings");
            drawMenu.AddDrawMenu(_spells, Color.LightSalmon);

            menu.AddSubMenu(drawMenu);

        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            DamageIndicator.Initialize(GetComboDamage);

        }

        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var endPoint = gapcloser.End;
            if (MenuHelper.isMenuEnabled("dzaio.graves.misc.antigpw") && _spells[SpellSlot.W].IsReady())
            {
                _spells[SpellSlot.W].Cast(endPoint);
            }
            if (MenuHelper.isMenuEnabled("dzaio.graves.misc.antigpe") && _spells[SpellSlot.E].IsReady())
            {
                var extended = gapcloser.Start.Extend(ObjectManager.Player.Position, gapcloser.Start.Distance(ObjectManager.Player.ServerPosition)+_spells[SpellSlot.E].Range);
                if (OkToE(extended))
                {
                    _spells[SpellSlot.W].Cast(extended);
                }
            }
        }

        void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo || !MenuHelper.isMenuEnabled("dzaio.graves.combo.ecancel"))
            {
                return;
            }
            switch (args.SData.Name)
            {
                case "GravesClusterShot":
                    if (OkToE(DZAIO.Player.Position.Extend(Game.CursorPos, MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"))) && _spells[SpellSlot.E].IsReady() && MenuHelper.isMenuEnabled("dzaio.graves.combo.ecancel"))
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(100, () => _spells[SpellSlot.E].Cast(DZAIO.Player.Position.Extend(Game.CursorPos, MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"))));
                    }
                    break;
                case "GravesChargeShot":
                    if (OkToE(DZAIO.Player.Position.Extend(Game.CursorPos, MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"))) && _spells[SpellSlot.E].IsReady() && MenuHelper.isMenuEnabled("dzaio.graves.combo.ecancel"))
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(100, () => _spells[SpellSlot.E].Cast(DZAIO.Player.Position.Extend(Game.CursorPos, MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"))));
                    }
                    break;
            }
        }

        public void SetUpSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.26f, 10f * 2 * (float)Math.PI / 180, 1950f, false, SkillshotType.SkillshotCone);
            _spells[SpellSlot.W].SetSkillshot(0.30f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.R].SetSkillshot(0.22f, 150f, 2100, true, SkillshotType.SkillshotLine);
        }


        public float GetComboDamage(Obj_AI_Hero unit)
        {
            return HeroHelper.GetComboDamage(_spells, unit);
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
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            var rTarget = TargetSelector.GetTarget(_spells[SpellSlot.R].Range, TargetSelector.DamageType.Physical);
            var eqTarget = TargetSelector.GetTarget(
                _spells[SpellSlot.Q].Range + MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"), TargetSelector.DamageType.Physical);
            var erTarget = TargetSelector.GetTarget(
                _spells[SpellSlot.Q].Range + MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"), TargetSelector.DamageType.Physical);

            //Q Casting in Combo

            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo) && target.IsValidTarget(_spells[SpellSlot.Q].Range))
            {
                _spells[SpellSlot.Q].CastIfHitchanceEquals(target, MenuHelper.GetHitchance());
            }

            //W Casting in Combo

            if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.W].CastIfWillHit(target, MenuHelper.getSliderValue("dzaio.graves.combo.minwenemiesc"));
            }

            //Normal R Casting in Combo
            if (rTarget.IsValidTarget(_spells[SpellSlot.R].Range))
            {
                if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && _spells[SpellSlot.R].IsKillable(rTarget) &&
                !(DZAIO.Player.Distance(rTarget) < DZAIO.Player.AttackRange))
                {
                    _spells[SpellSlot.R].CastIfHitchanceEquals(rTarget, MenuHelper.GetHitchance());
                }
            }
            
            //E-Q / E-R Casting in Combo
            var finalPosition = DZAIO.Player.Position.Extend(Game.CursorPos, MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange"));
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && OkToE(finalPosition))
            {
                if (eqTarget.IsValidTarget(MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange") + _spells[SpellSlot.Q].Range))
                {
                    if (_spells[SpellSlot.Q].IsKillable(eqTarget) && _spells[SpellSlot.Q].IsReady() &&
                    (finalPosition.Distance(eqTarget.Position) < DZAIO.Player.AttackRange) &&
                   Prediction.GetPrediction(eqTarget, (0.25f + finalPosition.Distance(eqTarget.Position) / _spells[SpellSlot.Q].Speed + 0.25f + (ObjectManager.Player.Distance(finalPosition) / 1250f))).Hitchance >= MenuHelper.GetHitchance())
                    {
                        _spells[SpellSlot.E].Cast(finalPosition);
                    }
                }
                if (erTarget.IsValidTarget(MenuHelper.getSliderValue("dzaio.graves.combo.emaxrange") + _spells[SpellSlot.R].Range))
                {
                    if (_spells[SpellSlot.R].IsKillable(erTarget) && _spells[SpellSlot.R].IsReady() &&
                    finalPosition.Distance(erTarget.Position) < DZAIO.Player.AttackRange &&
                    Prediction.GetPrediction(
                        erTarget,
                        (0.25f + finalPosition.Distance(erTarget.Position) / 2000f + 0.25f +
                         (ObjectManager.Player.Distance(finalPosition) / 1250f))).Hitchance >= MenuHelper.GetHitchance())
                    {
                        _spells[SpellSlot.E].Cast(finalPosition);
                    }
                }
            }
        }

        private void Harrass()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);

            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Harrass))
            {
                _spells[SpellSlot.Q].CastIfHitchanceEquals(target, MenuHelper.GetHitchance());
            }

            if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Harrass))
            {
                _spells[SpellSlot.W].CastIfWillHit(target, MenuHelper.getSliderValue("dzaio.graves.harass.minwenemiesh"));
            }
        }

        private void Farm()
        {
            var farmLocation = _spells[SpellSlot.Q].GetLineFarmLocation(
                MinionManager.GetMinions(DZAIO.Player.Position, _spells[SpellSlot.Q].Range));
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Farm) && farmLocation.MinionsHit > 2)
            {
                _spells[SpellSlot.Q].Cast(farmLocation.Position);
            }
        }

        void Drawing_OnDraw(EventArgs args)
        {
            DrawHelper.DrawSpellsRanges(_spells);
        }

        bool OkToE(Vector3 position)
        {
            if (position.UnderTurret(true) && !ObjectManager.Player.UnderTurret(true))
                return false;
            var allies = position.CountAlliesInRange(ObjectManager.Player.AttackRange);
            var enemies = position.CountEnemiesInRange(ObjectManager.Player.AttackRange);
            var lhEnemies = HeroHelper.GetLhEnemiesNearPosition(position, ObjectManager.Player.AttackRange).Count();

            if (enemies == 1) //It's a 1v1, safe to assume I can E
            {
                return true;
            }

            //Adding 1 for the Player
            return (allies + 1 > enemies - lhEnemies);
        }
        private Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
