using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using DZAIO.Utility.DamagePrediction;
using DZAIO.Utility.Drawing;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Champions
{
    class Zilean : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 700f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 0f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 700f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 900f) }
        };
        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;

            var comboMenu = new Menu(cName + " - Combo", "ZileanCombo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });
            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });
            

            menu.AddSubMenu(comboMenu);

            var harrassMenu = new Menu(cName + " - Harrass", "ZileanHarrass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 30, 35, 20 });

            menu.AddSubMenu(harrassMenu);

            var farmMenu = new Menu(cName + " - Farm", "ZileanFarm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.Q }, new[] { true });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.Q }, new[] { 40 });

            menu.AddSubMenu(farmMenu);

            var miscMenu = new Menu(cName + " - Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("dzaio.champion.zilean.antigpe", "E AntiGapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.champion.zilean.autoult", "Auto Ult").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.champion.zilean.autoult.mana", "Auto Ult Mana %").SetValue(new Slider(10)));
            }

            menu.AddNoUltiMenu(true);
            SummonerSpells.Heal.Cast();
            SummonerSpells.Flash.Cast(Game.CursorPos);

        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            DamagePrediction.OnSpellWillKill += DamagePrediction_OnSpellWillKill;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            DamageIndicator.Initialize(GetComboDamage);
        }

        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var sender = gapcloser.Sender;
            if (sender.IsValidTarget(_spells[SpellSlot.E].Range) && MenuHelper.isMenuEnabled("AntiGPE"))
            {
                _spells[SpellSlot.E].Cast(sender);
            }
        }

        public void SetUpSpells()
        {
        }

        public float GetComboDamage(Obj_AI_Hero unit)
        {
            return _spells.Where(spell => spell.Value.IsReady()).Sum(spell => (float)DZAIO.Player.GetSpellDamage(unit, spell.Key));
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            DebugHelper.AddEntry("Spell Q", _spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo).ToString());
            DebugHelper.AddEntry("Spell Q Menu != null", (DZAIO.Config.Item("ZileanUseQC") != null).ToString());
            DebugHelper.AddEntry("Spell Q Menu enabled", (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo)).ToString());
            DebugHelper.AddEntry("Spell E Menu enabled", (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo)).ToString());

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
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range,TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(_spells[SpellSlot.E].Range, TargetSelector.DamageType.Magical);

            
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
            {
               
                _spells[SpellSlot.Q].Cast(target);
            }
            
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.E].Cast(eTarget);
            }
            if (!_spells[SpellSlot.Q].IsReady() && _spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.W].Cast();
                if (DZAIO.Player.GetAlliesInRange(_spells[SpellSlot.E].Range).Count > 1)
                {
                    //The highest AD ally chasing too
                    var closestToTargetAd =
                        DZAIO.Player.GetAlliesInRange(_spells[SpellSlot.E].Range)
                            .OrderByDescending(h => h.PhysicalDamageDealtPlayer)
                            .First();
                    //The highest AP ally chasing too
                    var closestToTargetAp =
                        DZAIO.Player.GetAlliesInRange(_spells[SpellSlot.E].Range)
                            .OrderByDescending(h => h.MagicDamageDealtPlayer)
                            .First();
                    //If the phisical has done more dmg speed him, otherwise speed the other guy
                    if (closestToTargetAd.PhysicalDamageDealtPlayer >= closestToTargetAp.MagicDamageDealtPlayer)
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add(
                            100, () => _spells[SpellSlot.E].Cast(closestToTargetAd));
                    }
                    else
                    {
                        _spells[SpellSlot.W].Cast();
                        LeagueSharp.Common.Utility.DelayAction.Add(
                            100, () => _spells[SpellSlot.E].Cast(closestToTargetAp));
                    }

                }
                else
                {
                    //I'm the only one chasing
                    LeagueSharp.Common.Utility.DelayAction.Add(100, () => _spells[SpellSlot.E].Cast(DZAIO.Player));
                }
            }
        }

        private void Harrass()
        {
            var target =
                ObjectManager.Get<Obj_AI_Hero>().First(h => h.Distance(DZAIO.Player) < _spells[SpellSlot.Q].Range && h.HasBuff("timebombenemybuff", true)) ??
                TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Harrass))
            {
                _spells[SpellSlot.Q].Cast(target);
            }
            if (!_spells[SpellSlot.Q].IsReady() && _spells[SpellSlot.W].IsEnabledAndReady(Mode.Harrass))
            {
                _spells[SpellSlot.W].Cast();
            }
        }

        private void Farm()
        {

        }

        void DamagePrediction_OnSpellWillKill(Obj_AI_Hero sender, Obj_AI_Hero target,SpellData sData)
        {
            var targetName = target.ChampionName;
            if (sender.IsAlly)
                return;
            if (MenuHelper.isMenuEnabled("dzaio.champion." + DZAIO.Player.ChampionName.ToLowerInvariant() + ".noult."+targetName))
                return;
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && _spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && _spells[SpellSlot.R].CanCast(target))
            {
                _spells[SpellSlot.R].Cast(target);
            }
            if (_spells[SpellSlot.R].IsReady() && _spells[SpellSlot.R].CanCast(target) &&
                MenuHelper.isMenuEnabled("dzaio.champion.zilean.autoult") &&
                DZAIO.Player.ManaPercentage() >= MenuHelper.getSliderValue("dzaio.champion.zilean.autoult.mana"))
            {
                _spells[SpellSlot.R].Cast(target);
            }
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
