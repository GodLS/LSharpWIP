using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace DZAIO.Champions
{
    class Graves : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 950f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 950f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 425f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1200f) }
        };

        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "Combo");
            comboMenu.addModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });
            comboMenu.addManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });
            var comboOptions = new Menu("Skills Options", "COptions");
            {
                comboOptions.AddItem(new MenuItem("OnlyWEn", "Only W if hit x enemies").SetValue(new Slider(2, 1, 5)));
                comboOptions.AddItem(new MenuItem("ESlideRange", "E Distance").SetValue(new Slider(350, 1, 425)));
            }
            comboMenu.AddSubMenu(comboOptions);
            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "Harrass");
            harrassMenu.addModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            harrassMenu.addManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { 30, 35 });
            harrassMenu.AddItem(new MenuItem("OnlyWEnH", "Only W if hit x enemies").SetValue(new Slider(2, 1, 5)));
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "Farm");
            farmMenu.addModeMenu(Mode.Farm, new[] { SpellSlot.Q }, new[] { true });
            farmMenu.addManaManager(Mode.Farm, new[] { SpellSlot.Q }, new[] { 40 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("AntiGPW", "W AntiGapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("AntiGPE", "E AntiGapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("ManualR", "Manual R").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            }
            miscMenu.addHitChanceSelector();

            menu.AddSubMenu(miscMenu);
        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var endPoint = gapcloser.End;
            if (DZUtility.isMenuEnabled("AntiGPW") && _spells[SpellSlot.W].IsReady())
            {
                _spells[SpellSlot.W].Cast(endPoint);
            }
            if (DZUtility.isMenuEnabled("AntiGPE") && _spells[SpellSlot.E].IsReady())
            {
                var extended = ObjectManager.Player.Position.Extend(gapcloser.Start, -_spells[SpellSlot.E].Range);
                if (OkToE(extended))
                {
                    _spells[SpellSlot.W].Cast(extended);
                }
            }
        }

        void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) || _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            switch (args.SData.Name)
            {
                case "GravesClusterShot":
                    if (OkToE(DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"))) && _spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo))
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(100, () => _spells[SpellSlot.E].Cast(DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"))));
                    }
                    break;
                case "GravesChargeShot":
                    if (OkToE(DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"))) && _spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo))
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(100, () => _spells[SpellSlot.E].Cast(DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"))));
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
            var RTarget = TargetSelector.GetTarget(_spells[SpellSlot.R].Range, TargetSelector.DamageType.Physical);
            var EQTarget = TargetSelector.GetTarget(
                _spells[SpellSlot.Q].Range + DZUtility.getSliderValue("ESlideRange"), TargetSelector.DamageType.Physical);
            var ERTarget = TargetSelector.GetTarget(
                _spells[SpellSlot.Q].Range + DZUtility.getSliderValue("ESlideRange"), TargetSelector.DamageType.Physical);

            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.Q].CastIfHitchanceEquals(target, DZUtility.GetHitchance());
            }

            if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.W].CastIfWillHit(target, DZUtility.getSliderValue("OnlyWEn"));
            }

            if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && target.IsValidTarget(_spells[SpellSlot.R].Range) && _spells[SpellSlot.R].IsKillable(RTarget) &&
                !(DZAIO.Player.Distance(RTarget) < DZAIO.Player.AttackRange) &&
                !(_spells[SpellSlot.Q].IsKillable(RTarget) && RTarget.IsValidTarget(_spells[SpellSlot.Q].Range)))
            {
                _spells[SpellSlot.R].CastIfHitchanceEquals(RTarget, DZUtility.GetHitchance());
            }

                if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) &&
                    (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo)) &&
                    OkToE(DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"))))
                {
                    var FinalPosition = DZAIO.Player.Position.Extend(
                        Game.CursorPos, DZUtility.getSliderValue("ESlideRange"));
                    _spells[SpellSlot.Q].UpdateSourcePosition(FinalPosition);
                    if (_spells[SpellSlot.Q].GetPrediction(EQTarget).Hitchance >= DZUtility.GetHitchance())
                    {
                        _spells[SpellSlot.E].Cast(Game.CursorPos);
                        var time =
                            DZAIO.Player.Distance(
                                DZAIO.Player.Position.Extend(Game.CursorPos, _spells[SpellSlot.E].Range)) /
                                _spells[SpellSlot.E].Speed;
                        LeagueSharp.Common.Utility.DelayAction.Add(
                            (int) time, () => _spells[SpellSlot.E].Cast(Game.CursorPos));
                    }
                    _spells[SpellSlot.Q].UpdateSourcePosition(DZAIO.Player.Position);
                }
            
                if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo)) && OkToE(DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"))))
                {
                    var FinalPosition = DZAIO.Player.Position.Extend(Game.CursorPos, DZUtility.getSliderValue("ESlideRange"));
                    _spells[SpellSlot.R].UpdateSourcePosition(FinalPosition);
                    if (_spells[SpellSlot.R].GetPrediction(ERTarget).Hitchance >= DZUtility.GetHitchance() &&
                        _spells[SpellSlot.R].IsKillable(ERTarget) &&
                        !(DZAIO.Player.Distance(ERTarget) < DZAIO.Player.AttackRange) &&
                        !(_spells[SpellSlot.Q].IsKillable(ERTarget) && ERTarget.IsValidTarget(_spells[SpellSlot.Q].Range)))
                    {
                        _spells[SpellSlot.E].Cast(Game.CursorPos);
                        var time = DZAIO.Player.Distance(DZAIO.Player.Position.Extend(Game.CursorPos, _spells[SpellSlot.E].Range)) / _spells[SpellSlot.E].Speed;
                        LeagueSharp.Common.Utility.DelayAction.Add((int)time, () => _spells[SpellSlot.E].Cast(Game.CursorPos));
                    }
                    _spells[SpellSlot.R].UpdateSourcePosition(DZAIO.Player.Position);
                }
            
        }

        private void Harrass()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);

            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Harrass))
            {
                _spells[SpellSlot.Q].CastIfHitchanceEquals(target, DZUtility.GetHitchance());
            }

            if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Harrass))
            {
                _spells[SpellSlot.W].CastIfWillHit(target, DZUtility.getSliderValue("OnlyWEnH"));
            }
        }

        private void Farm()
        {

        }

        void Drawing_OnDraw(EventArgs args)
        {

        }

        bool OkToE()
        {
            if (ObjectManager.Player.CountEnemiesInRange(525f) > 0)
                return true;
            if (Game.CursorPos.UnderTurret(true) && !ObjectManager.Player.UnderTurret(true))
                return false;
            var allies = HeroHelper.GetAlliesNearMouse(ObjectManager.Player.AttackRange).Count();
            var enemies = HeroHelper.GetEnemiesNearMouse(ObjectManager.Player.AttackRange);
            var lhEnemies = HeroHelper.GetLhEnemiesNearMouse(ObjectManager.Player.AttackRange).Count();

            if (enemies.Count() == 1) //It's a 1v1, safe to assume I can E
            {
                return true;
            }

            float totalEnH = enemies.Sum(x => x.Health); //Borrowed from Ahri#. Thanks Beaving
            //Adding 1 for the Player
            return (allies + 1 > enemies.Count() - lhEnemies) || ObjectManager.Player.HealthPercentage() / totalEnH > 0.7f;
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
