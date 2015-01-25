﻿using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using DZAIO.Utility.DamagePrediction;
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

            var comboMenu = new Menu(cName + " - Combo", "Combo");
            comboMenu.addModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });
            comboMenu.addManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });
            

            menu.AddSubMenu(comboMenu);

            var harrassMenu = new Menu(cName + " - Harrass", "Harrass");
            harrassMenu.addModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            harrassMenu.addManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 30, 35, 20 });

            menu.AddSubMenu(harrassMenu);

            var farmMenu = new Menu(cName + " - Farm", "Farm");
            farmMenu.addModeMenu(Mode.Farm, new[] { SpellSlot.Q }, new[] { true });
            farmMenu.addManaManager(Mode.Farm, new[] { SpellSlot.Q }, new[] { 40 });

            menu.AddSubMenu(farmMenu);

            var miscMenu = new Menu(cName + " - Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("AntiGPE", "E AntiGapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("AutoUlt", "Auto Ult").SetValue(true));
                miscMenu.AddItem(new MenuItem("AutoUltMana", "Auto Ult Mana %").SetValue(new Slider(10)));
            }

            menu.addNoUltiMenu(true);

        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            DamagePrediction.OnSpellWillKill += DamagePrediction_OnSpellWillKill;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
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
            var target =
                ObjectManager.Get<Obj_AI_Hero>().First(h => h.Distance(DZAIO.Player) < _spells[SpellSlot.Q].Range && h.HasBuff("timebombenemybuff", true)) ??
                TargetSelector.GetTarget(_spells[SpellSlot.Q].Range,TargetSelector.DamageType.Magical);
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.Q].Cast(target);
            }
            if (!_spells[SpellSlot.Q].IsReady() && _spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo))
            {
                _spells[SpellSlot.W].Cast();
            }
            var eTarget = TargetSelector.GetTarget(_spells[SpellSlot.E].Range, TargetSelector.DamageType.Magical);
            if (!eTarget.IsFacing(DZAIO.Player) && DZAIO.Player.IsFacing(eTarget))
            {
                if (!eTarget.IsMoving || !DZAIO.Player.IsMoving)
                    return;
                //The target is not facing us, but we are facing them. And we are both moving
                //Is the Spell Enabled and Ready in combo
                if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo))
                {
                    _spells[SpellSlot.E].Cast(eTarget);
                }
                //The W spell is ready. Do E on the target and boost an ally/me
                if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo))
                {
                    //Allies in range > 1
                    if (DZAIO.Player.GetAlliesInRange(_spells[SpellSlot.E].Range).Count > 1)
                    {
                        //The highest AD ally chasing too
                        var closestToTargetAd =
                            DZAIO.Player.GetAlliesInRange(_spells[SpellSlot.E].Range)
                                .Where(h => h.IsFacing(eTarget))
                                .OrderByDescending(h => h.PhysicalDamageDealtPlayer)
                                .First();
                        //The highest AP ally chasing too
                        var closestToTargetAp =
                            DZAIO.Player.GetAlliesInRange(_spells[SpellSlot.E].Range)
                                .Where(h => h.IsFacing(eTarget))
                                .OrderByDescending(h => h.MagicDamageDealtPlayer)
                                .First();
                        //If the phisical has done more dmg speed him, otherwise speed the other guy
                        if (closestToTargetAd.PhysicalDamageDealtPlayer >= closestToTargetAp.MagicDamageDealtPlayer)
                        {
                            _spells[SpellSlot.W].Cast();
                            LeagueSharp.Common.Utility.DelayAction.Add(
                                150, () => _spells[SpellSlot.E].Cast(closestToTargetAd));
                        }
                        else
                        {
                            _spells[SpellSlot.W].Cast();
                            LeagueSharp.Common.Utility.DelayAction.Add(
                                150, () => _spells[SpellSlot.E].Cast(closestToTargetAp));
                        }

                    }
                    else
                    {
                        //I'm the only one chasing
                        _spells[SpellSlot.W].Cast();
                        LeagueSharp.Common.Utility.DelayAction.Add(150, () => _spells[SpellSlot.E].Cast(DZAIO.Player));
                    }
                }
            }else
            {
                //The E Target is facing us and we are not facing them -> We are running away
                if (!eTarget.IsMoving)
                    return;
                _spells[SpellSlot.E].Cast(DZAIO.Player);
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

        void DamagePrediction_OnSpellWillKill(Obj_AI_Hero sender, Obj_AI_Hero target)
        {
            var targetName = target.ChampionName;
            if (MenuHelper.isMenuEnabled("noUlt" + targetName))
                return;
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && _spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && _spells[SpellSlot.R].CanCast(target))
            {
                _spells[SpellSlot.R].Cast(target);
            }
            if (_spells[SpellSlot.R].IsReady() && _spells[SpellSlot.R].CanCast(target) &&
                MenuHelper.isMenuEnabled("AutoUlt") &&
                DZAIO.Player.ManaPercentage() >= MenuHelper.getSliderValue("AutoUltMana"))
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
