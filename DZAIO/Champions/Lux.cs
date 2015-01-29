using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Champions
{
    class Lux : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 950f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 950f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 425f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1200f) }
        };

        private static Obj_AI_Base LuxEGameObject
        {
            get
            {
                return ObjectManager.Get<Obj_AI_Base>().First(h => h.Name == "LuxLightstrike_tar_green" || h.Name == "LuxLightstrike_tar_red");
            }
        }
        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "LuxCombo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });

            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });

            var skillOptionMenu = new Menu("Skill Options", "LuxSkOption");
            skillOptionMenu.AddItem(new MenuItem("LuxEAfterR", "Detonate E After R").SetValue(true));
            comboMenu.AddSubMenu(skillOptionMenu);

            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "LuxHarrass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { true, true, false });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 30, 35, 20 });
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "LuxFarm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.E }, new[] { false });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.E }, new[] { 35 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("LuxAntiGPQ", "W AntiGapcloser").SetValue(true));
            }
            miscMenu.AddHitChanceSelector();
            menu.AddSubMenu(miscMenu);
        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        public void SetUpSpells()
        {
            //TODO Change these
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
            var comboTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range,TargetSelector.DamageType.Magical);
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo) && comboTarget.IsValidTarget(_spells[SpellSlot.Q].Range))
            {
                var qPrediction = _spells[SpellSlot.Q].GetPrediction(comboTarget);
                if (qPrediction.Hitchance >= MenuHelper.GetHitchance())
                {
                    _spells[SpellSlot.Q].Cast(qPrediction.CastPosition);
                }
                if (qPrediction.Hitchance == HitChance.Collision)
                {
                    var collisionObjs = qPrediction.CollisionObjects;
                    if (collisionObjs.Count == 1)
                    {
                        _spells[SpellSlot.Q].Cast(qPrediction.CastPosition);
                    }
                }
            }
            Obj_AI_Hero eRTarget;
            if (
                DZAIO.Player.GetEnemiesInRange(_spells[SpellSlot.E].Range)
                    .Any(h => h.IsValidTarget() && h.HasBuff("LuxQBuff", true)))
            {
                eRTarget =
                    DZAIO.Player.GetEnemiesInRange(_spells[SpellSlot.E].Range)
                        .First(h => h.IsValidTarget() && h.HasBuff("LuxQBuff", true));
            }
            else
            {
                eRTarget = comboTarget;
            }

            
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && !IsSecondE() && eRTarget.IsValidTarget(_spells[SpellSlot.E].Range))
            {
                _spells[SpellSlot.E].CastIfHitchanceEquals(eRTarget, MenuHelper.GetHitchance());
            }

            AutoDetonate();
        }

        private void Harrass()
        {

        }

        private void Farm()
        {

        }

        public void AutoDetonate()
        {
            if (LuxEGameObject == null)
                return;
            if (LuxEGameObject.CountEnemiesInRange(450f) > 1 && _spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && IsSecondE())//TODO Find real range
            {
                var Enemy = LuxEGameObject.GetEnemiesInRange(450f).OrderBy(h => h.HealthPercentage()).First();
                var RPred = _spells[SpellSlot.R].GetPrediction(Enemy);
                if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && IsKillable(Enemy) &&
                    RPred.Hitchance >= MenuHelper.GetHitchance())
                {
                    _spells[SpellSlot.R].Cast(RPred.CastPosition);
                    LeagueSharp.Common.Utility.DelayAction.Add(500, () => _spells[SpellSlot.E].Cast());
                }
                else
                {
                    if (!HasPassive(Enemy) && DZAIO.Player.Distance(Enemy) < Orbwalking.GetRealAutoAttackRange(null))
                    {
                        _spells[SpellSlot.E].Cast();
                    }
                    if (!(DZAIO.Player.Distance(Enemy) < Orbwalking.GetRealAutoAttackRange(null)))
                    {
                        _spells[SpellSlot.E].Cast();
                    }
                }
            }
        }
        public bool IsSecondE()
        {
            return DZAIO.Player.Spellbook.GetSpell(SpellSlot.E).Name == "Lux2ndEName";
        }
        public static bool HasPassive(Obj_AI_Hero hero)
        {
            return hero.HasBuff("luxilluminatingfraulein", true);
        }

        public bool IsKillable(Obj_AI_Hero target)
        {
            var PassiveDamage = HasPassive(target)?0:0; //TODO Add this in
            return _spells[SpellSlot.E].GetDamage(target) + _spells[SpellSlot.R].GetDamage(target) + PassiveDamage > target.Health + 20;
        }

        void Drawing_OnDraw(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
