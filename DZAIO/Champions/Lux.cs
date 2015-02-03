using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using DZAIO.Utility.Drawing;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Champions
{
    class Lux : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 1175f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 1050f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 1100f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 3340f) }
        };

        private static GameObject LuxEGameObject { get; set; }
        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "dzaio.lux.combo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });

            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });

            var skillOptionMenu = new Menu("Skill Options", "dzaio.lux.combo.skilloptions");
            skillOptionMenu.AddItem(new MenuItem("dzaio.lux.combo.skilloptions.eafterr", "Detonate E After R").SetValue(true));
            comboMenu.AddSubMenu(skillOptionMenu);

            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "dzaio.lux.harrass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { true, true, false });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 30, 35, 20 });
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "dzaio.lux.farm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.E }, new[] { false });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.E }, new[] { 35 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "dzaio.lux.misc");
            {
                miscMenu.AddItem(new MenuItem("dzaio.lux.misc.antigapcloserq", "Q AntiGapcloser").SetValue(true));
            }
            miscMenu.AddHitChanceSelector();
            menu.AddSubMenu(miscMenu);
        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            DamageIndicator.Initialize(GetComboDamage);
        }

        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("LuxLightstrike_tar_green") || sender.Name.Contains("LuxLightstrike_tar_red"))
            {
                LuxEGameObject = null;
            }
        }

        void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("LuxLightstrike_tar_green") || sender.Name.Contains("LuxLightstrike_tar_red"))
            {
                LuxEGameObject = sender;
            }
        }

        float GetComboDamage(Obj_AI_Hero unit)
        {
           return HeroHelper.GetComboDamage(_spells,unit);
        }

        public void SetUpSpells()
        {
            //TODO Change these
            _spells[SpellSlot.Q].SetSkillshot(0.5f, 70, 1200, true, SkillshotType.SkillshotLine);
            _spells[SpellSlot.W].SetSkillshot(0.5f, 150, 1200, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.E].SetSkillshot(0.5f, 150, 1200, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.R].SetSkillshot(1.75f, 190, 3000, false, SkillshotType.SkillshotLine);
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
            var rTarget = TargetSelector.GetTarget(_spells[SpellSlot.R].Range, TargetSelector.DamageType.Magical);
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
                    .Any(h => h.IsValidTarget() && h.HasBuffOfType(BuffType.Snare)))
            {
                eRTarget =
                    DZAIO.Player.GetEnemiesInRange(_spells[SpellSlot.E].Range)
                        .Find(h => h.IsValidTarget() && h.HasBuffOfType(BuffType.Snare));
            }
            else
            {
                eRTarget = comboTarget;
            }
            
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && !IsSecondE() && eRTarget.IsValidTarget(_spells[SpellSlot.E].Range))
            {
                _spells[SpellSlot.E].CastIfHitchanceEquals(eRTarget, MenuHelper.GetHitchance());
            }
            var rPred = _spells[SpellSlot.R].GetPrediction(rTarget);

            AutoDetonate();
            if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) &&  rTarget.IsValidTarget(_spells[SpellSlot.R].Range) && !_spells[SpellSlot.E].IsReady() && _spells[SpellSlot.R].GetDamage(rTarget) > rTarget.Health + 20 &&
            rPred.Hitchance >= MenuHelper.GetHitchance())
            {
                _spells[SpellSlot.R].Cast(rPred.CastPosition);
            }
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
            {
                return;
            }
            if (LuxEGameObject.Position.CountEnemiesInRange(450f) >= 1 && _spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && IsSecondE())//TODO Find real range
            {
                var enemy = LuxEGameObject.Position.GetEnemiesInRange(450f).OrderBy(h => h.HealthPercentage()).First();
                var rPred = _spells[SpellSlot.R].GetPrediction(enemy);
                if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && IsKillable(enemy) &&
                    rPred.Hitchance >= MenuHelper.GetHitchance())
                {
                    _spells[SpellSlot.R].Cast(rPred.CastPosition);
                    LeagueSharp.Common.Utility.DelayAction.Add(1250, () => _spells[SpellSlot.E].Cast());
                }
                else
                {
                    if (!HasPassive(enemy) && DZAIO.Player.Distance(enemy) < Orbwalking.GetRealAutoAttackRange(null))
                    {
                        _spells[SpellSlot.E].Cast();
                    }
                    if (!(DZAIO.Player.Distance(enemy) < Orbwalking.GetRealAutoAttackRange(null)))
                    {
                        _spells[SpellSlot.E].Cast();
                    }
                }
            }
        }
        
        public bool IsSecondE()
        {
            return LuxEGameObject != null || DZAIO.Player.Spellbook.GetSpell(SpellSlot.E).Name == "luxlightstriketoggle";
        }
        public static bool HasPassive(Obj_AI_Hero hero)
        {
            return hero.HasBuff("luxilluminatingfraulein", true);
        }

        public bool IsKillable(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget())
                return false;
            var passiveDamage = HasPassive(target)?GetPassiveProcDamage():0;
            return _spells[SpellSlot.E].GetDamage(target) + _spells[SpellSlot.R].GetDamage(target) + passiveDamage > target.Health + 20;
        }
        public static double GetPassiveProcDamage()
        {
            return 10 + 8 * DZAIO.Player.Level + (DZAIO.Player.FlatMagicDamageMod + DZAIO.Player.BaseAbilityDamage) * 0.2d;
        }

        void Drawing_OnDraw(EventArgs args)
        {
        }

        private Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
