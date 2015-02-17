using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DZAIO.Utility.Drawing;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Champions
{
    class Cassiopeia : IChampion
    {

        /// <summary>
        /// Cassiopeia features
        /// 
        /// Uses Q, W, E, R combo
        /// Smooth E
        /// Mana Manager for each mode
        /// 
        /// Only W if x Enemies
        /// Only W if not poisoned
        /// Min R enemies not facing
        /// Min R enemies Facing
        /// 
        /// Auto Q
        /// Auto Q Mana
        /// 
        /// Farm:
        /// Min minions for Q-W
        /// Uses Q,W,E
        /// 
        /// Use AA combo
        /// Antigapcloser R
        /// Interrupter R
        /// Auto R enemy under tower
        /// Auto R enemy when low
        /// Block R if no hit
        /// 
        /// Humanizer:
        /// E Delay
        /// Laneclear delay
        /// Use AA Laneclear
        /// 
        /// KS:
        /// With Q, E
        /// </summary>
        
        
        private static float _lastCastedETick;
        private static float _lastCastedQTick;
        private static float _lastLcTick;

        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 850f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 850f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 700f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 825f) }
        };

        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "dzaio.cassiopeia.combo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });

            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });

            var skillOptionMenu = new Menu("Skill Options", "dzaio.cassiopeia.combo.skilloptions");
            {
                skillOptionMenu.AddItem(new MenuItem("dzaio.cassiopeia.combo.skilloptions.minwenemies", "Min W Enemies").SetValue(new Slider(2, 1, 5)));
                skillOptionMenu.AddItem(new MenuItem("dzaio.cassiopeia.combo.skilloptions.onlywnotpoison", "Only W if not poisoned").SetValue(true));
                skillOptionMenu.AddItem(new MenuItem("dzaio.cassiopeia.combo.skilloptions.minrenemiesf", "Min R Enemies Facing").SetValue(new Slider(2, 1, 5)));
                skillOptionMenu.AddItem(new MenuItem("dzaio.cassiopeia.combo.skilloptions.minrenemiesnf", "Min R Enemies Not facing").SetValue(new Slider(3, 1, 5)));
            }
            comboMenu.AddSubMenu(skillOptionMenu);

            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "dzaio.cassiopeia.harass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.E }, new[] { true, true });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.E }, new[] { 30, 20 });

            var autoHarrassMenu = new Menu("AutoHarass", "dzaio.cassiopeia.harass.autoharass");
            {
                autoHarrassMenu.AddItem(new MenuItem("dzaio.cassiopeia.harass.useqah", "Use Auto Q").SetValue(false));
                autoHarrassMenu.AddItem(new MenuItem("dzaio.cassiopeia.harass.qmanaah", "Auto Q Mana").SetValue(new Slider(25)));
            }
            harrassMenu.AddSubMenu(autoHarrassMenu);
            menu.AddSubMenu(harrassMenu);

            var farmMenu = new Menu(cName + " - Farm", "dzaio.cassiopeia.farm");
            farmMenu.AddModeMenu(Mode.Laneclear, new[] { SpellSlot.Q,SpellSlot.W,SpellSlot.E }, new[] { false,false,false });
            farmMenu.AddManaManager(Mode.Laneclear, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E}, new[] { 35,35,35 });
            farmMenu.AddItem(new MenuItem("dzaio.cassiopeia.farm.minminions", "Min. Minions for Q/W").SetValue(new Slider(2, 1, 5)));
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "dzaio.cassiopeia.misc");
            {
                miscMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.aacombo", "Use AA Combo").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.antigp", "Anti Gapcloser R").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.interrupter", "R Interrupter").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.autorturret", "Auto R enemy under tower").SetValue(false));
                miscMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.autorlow", "Auto R lifesaver").SetValue(true));
                miscMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.blockr", "Block R if no hit").SetValue(true));
            }
            miscMenu.AddHitChanceSelector();
            var humanizerMenu = new Menu("Humanizer", "dzaio.cassiopeia.misc.humanizer");
            {
                humanizerMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.humanizer.edelay", "E Delay").SetValue(new Slider(300,0, 1500)));
                humanizerMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.humanizer.lcdelay", "Laneclear Delay").SetValue(new Slider(300, 0, 1000)));
                humanizerMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.humanizer.disableaalc", "Disable AA Laneclear").SetValue(false));
            }
            miscMenu.AddSubMenu(humanizerMenu);
            menu.AddSubMenu(miscMenu);
            var ksMenu = new Menu(cName + " - Killsteal", "dzaio.cassiopeia.killsteal");
            {
                ksMenu.AddItem(new MenuItem("dzaio.cassiopeia.killsteal.useq", "Use Q KS").SetValue(true));
                ksMenu.AddItem(new MenuItem("dzaio.cassiopeia.killsteal.usee", "Use E KS").SetValue(true));
                ksMenu.AddItem(new MenuItem("dzaio.cassiopeia.killsteal.eksmode", "E KS Mode").SetValue(new StringList(new []{"If Poisoned","Always"})));
            }
            menu.AddSubMenu(ksMenu);

            var drawMenu = new Menu(cName + " - Drawings", "dzaio.cassiopeia.drawing");
            drawMenu.AddDrawMenu(_spells,Color.Aquamarine);

            menu.AddSubMenu(drawMenu);
            Game.PrintChat("<b><font color='#FF0000'>[DZAIO]</font></b> <b><font color='#00FF00'>{0}</font></b> loaded! <font color='#FFFFFF'> </font>", "PennyCassio");
        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            DamageIndicator.Initialize(GetComboDamage);
        }


        void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.blockr"))
            {
                return;
            }
            if (sender.Owner.IsMe)
            {
                if (args.Slot == SpellSlot.R)
                {
                    var finalPosition = args.EndPosition;
                    var hit = HeroManager.Enemies.FindAll(m => _spells[SpellSlot.R].WillHit(m, finalPosition));
                    if (hit.Count == 0)
                    {
                        args.Process = false;
                    }
                }   
            }
            
        }

        void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (!MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.aacombo") && _spells[SpellSlot.E].IsReady())
                {
                    args.Process = false;
                }
            }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.humanizer.disableaalc"))
                {
                    args.Process = false;
                }
            }
        }

        void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.antigp") && _spells[SpellSlot.R].IsReady())
            {
                if (args.DangerLevel >= Interrupter2.DangerLevel.High && sender.IsFacing(ObjectManager.Player))
                {
                    _spells[SpellSlot.R].CastIfHitchanceEquals(sender, MenuHelper.GetHitchance());
                }
            }
        }

        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.antigp") && _spells[SpellSlot.R].IsReady())
            {
                if (gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 200f && gapcloser.Sender.IsFacing(ObjectManager.Player))
                {
                    _spells[SpellSlot.R].CastIfHitchanceEquals(gapcloser.Sender,MenuHelper.GetHitchance());
                }
            }
        }

        private float GetComboDamage(Obj_AI_Hero hero)
        {
            var qDamage = _spells[SpellSlot.Q].IsReady() ? _spells[SpellSlot.Q].GetDamage(hero) : 0;
            var wDamage = _spells[SpellSlot.W].IsReady() ? _spells[SpellSlot.W].GetDamage(hero) : 0;
            var eDamage = _spells[SpellSlot.E].IsReady() ? _spells[SpellSlot.E].GetDamage(hero) : 0;
            var rDamage = _spells[SpellSlot.R].IsReady() ? _spells[SpellSlot.R].GetDamage(hero) : 0;
            return qDamage + wDamage + eDamage * 2 + rDamage + (float)ObjectManager.Player.GetAutoAttackDamage(hero) * 2;
        }

        void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.SData.Name)
                {
                    case "CassiopeiaTwinFang":
                        _lastCastedETick = Environment.TickCount;
                        break;
                    case "CassiopeiaNoxiousBlast":
                        _lastCastedQTick = Environment.TickCount;
                        break;
                }
            }        
        }

        public void SetUpSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.6f, 40f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.W].SetSkillshot(0.5f, 90f, 2500, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.E].SetTargetted(0.2f, float.MaxValue);
            _spells[SpellSlot.R].SetSkillshot(0.6f, (float)(80 * Math.PI / 180), float.MaxValue, false, SkillshotType.SkillshotCone);
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            //DebugHelper.AddEntry("Can Cast W", (Environment.TickCount - _lastCastedQTick >= 700f).ToString());
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harrass();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
                default:
                    return;
            }
            Ks();
            AutoHarass();
            AutoRTower();
            AutoRLow();
        }
        private void Combo()
        {
            var comboTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
            var eDelay = MenuHelper.getSliderValue("dzaio.cassiopeia.misc.humanizer.edelay");

            if (PoisonedTargetInRange(_spells[SpellSlot.E].Range).Any())
            {
                comboTarget = PoisonedTargetInRange(_spells[SpellSlot.E].Range).OrderBy(h => h.HealthPercentage()).First();
            }
            if (comboTarget.IsValidTarget())
            {

                if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
                {
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(comboTarget, MenuHelper.GetHitchance());
                }
                if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo) && Environment.TickCount - _lastCastedQTick >= 700f)
                {
                    if (MenuHelper.getSliderValue("dzaio.cassiopeia.combo.skilloptions.minwenemies") == 1)
                    {
                        if ((MenuHelper.isMenuEnabled("dzaio.cassiopeia.combo.skilloptions.onlywnotpoison") &&
                             !IsTargetPoisoned(comboTarget)))
                        {
                            _spells[SpellSlot.W].CastIfHitchanceEquals(comboTarget, MenuHelper.GetHitchance());
                        }
                        else
                        {
                            _spells[SpellSlot.W].CastIfHitchanceEquals(comboTarget, MenuHelper.GetHitchance());
                        }
                    }
                    else
                    {
                         _spells[SpellSlot.W].CastIfWillHit(comboTarget,MenuHelper.getSliderValue("dzaio.cassiopeia.combo.skilloptions.minwenemies"));
                    }
                }
                if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && IsTargetPoisoned(comboTarget) && (Environment.TickCount - _lastCastedETick >= eDelay))
                {
                    _spells[SpellSlot.E].Cast(comboTarget);
                }
                if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo))
                {
                    var rPrediction = _spells[SpellSlot.R].GetPrediction(comboTarget);
                    var enemiesFacing = HeroManager.Enemies.FindAll(enemy => _spells[SpellSlot.R].WillHit(enemy, rPrediction.CastPosition) && enemy.IsFacing(ObjectManager.Player));
                    var normalEnemies = HeroManager.Enemies.FindAll(enemy => _spells[SpellSlot.R].WillHit(enemy, rPrediction.CastPosition));
                    var enemiesKillable = enemiesFacing.FindAll(CanKill).Count;
                    if ((enemiesFacing.Count >= MenuHelper.getSliderValue("dzaio.cassiopeia.combo.skilloptions.minrenemiesf") && enemiesKillable >= 1) || normalEnemies.Count >= MenuHelper.getSliderValue("dzaio.cassiopeia.combo.skilloptions.minrenemiesnf"))
                    {
                        if (normalEnemies.Count == 1)
                        {
                            if (WontOverkill(normalEnemies.First()))
                            {
                                _spells[SpellSlot.R].Cast(rPrediction.CastPosition);
                            }
                            return;
                        }
                        _spells[SpellSlot.R].Cast(rPrediction.CastPosition);
                    }
                }
            }
        }

        void Harrass()
        {
            var harassTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
            var eDelay = MenuHelper.getSliderValue("dzaio.cassiopeia.misc.humanizer.edelay");
            if (PoisonedTargetInRange(_spells[SpellSlot.E].Range).Any())
            {
                harassTarget = PoisonedTargetInRange(_spells[SpellSlot.E].Range).OrderBy(h => h.HealthPercentage()).First();
            }
            if (harassTarget.IsValidTarget())
            {

                if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
                {
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(harassTarget, MenuHelper.GetHitchance());
                }
                if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo) && IsTargetPoisoned(harassTarget) &&
                    (Environment.TickCount - _lastCastedETick >= eDelay))
                {
                    _spells[SpellSlot.E].Cast(harassTarget);
                }
            }
        }

        void Farm()
        {
            if (Environment.TickCount - _lastLcTick <
                MenuHelper.getSliderValue("dzaio.cassiopeia.misc.humanizer.lcdelay"))
            {
                return;
            }
            _lastLcTick = Environment.TickCount;

            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spells[SpellSlot.E].Range);
            var eDelay = MenuHelper.getSliderValue("dzaio.cassiopeia.misc.humanizer.edelay");
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Laneclear))
            {
                if (_spells[SpellSlot.Q].GetCircularFarmLocation(minions).MinionsHit >= MenuHelper.getSliderValue("dzaio.cassiopeia.farm.minminions"))
                {
                    _spells[SpellSlot.Q].Cast(_spells[SpellSlot.Q].GetCircularFarmLocation(minions).Position);
                }
            }
            if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Laneclear) && Environment.TickCount - _lastCastedQTick >= 700f)
            {
                if (_spells[SpellSlot.W].GetCircularFarmLocation(minions).MinionsHit >= MenuHelper.getSliderValue("dzaio.cassiopeia.farm.minminions"))
                {
                    _spells[SpellSlot.W].Cast(_spells[SpellSlot.W].GetCircularFarmLocation(minions).Position);
                }
            }
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Laneclear))
            {
                var poisonedMinions = minions.FindAll(minion => minion.HasBuffOfType(BuffType.Poison)).OrderBy(minion => minion.HealthPercentage());
                if (poisonedMinions.Any() && (Environment.TickCount - _lastCastedETick >= eDelay))
                {
                    _spells[SpellSlot.E].Cast(poisonedMinions.First());
                }
            }
        }

        void AutoHarass()
        {
            var qMana = DZAIO.Config.Item("dzaio.cassiopeia.harass.qmanaah").GetValue<Slider>().Value;
            var useQ = DZAIO.Config.Item("dzaio.cassiopeia.harass.useqah").GetValue<bool>();
            if (_spells[SpellSlot.Q].IsReady() && ObjectManager.Player.ManaPercentage() >= qMana && useQ)
            {
                var qTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
                if (qTarget.IsValidTarget(_spells[SpellSlot.Q].Range))
                {
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(qTarget, MenuHelper.GetHitchance());
                }
            }
        }

        void Ks()
        {
            var eKsTarget = TargetSelector.GetTarget(_spells[SpellSlot.E].Range,TargetSelector.DamageType.Magical);
            var qKsTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range,TargetSelector.DamageType.Magical);
            if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.killsteal.useq") && _spells[SpellSlot.Q].IsReady() && qKsTarget.IsValidTarget())
            {
                if(qKsTarget.Health+20<_spells[SpellSlot.Q].GetDamage(qKsTarget))
                {
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(qKsTarget, MenuHelper.GetHitchance());
                }
            }
            if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.killsteal.usee") && _spells[SpellSlot.E].IsReady() &&
                eKsTarget.IsValidTarget())
            {
                switch (DZAIO.Config.Item("dzaio.cassiopeia.killsteal.eksmode").GetValue<StringList>().SelectedIndex)
                {
                   //Poisoned
                    case 0:
                        if (IsTargetPoisoned(eKsTarget) &&
                            eKsTarget.Health + 20 < _spells[SpellSlot.E].GetDamage(eKsTarget))
                        {
                            _spells[SpellSlot.E].Cast(eKsTarget);
                        }
                    break;
                    //Always
                    case 1:
                        if (eKsTarget.Health + 20 < _spells[SpellSlot.E].GetDamage(eKsTarget))
                        {
                            _spells[SpellSlot.E].Cast(eKsTarget);
                        }
                   break;
                }
            }
        }

        void AutoRTower()
        {
            if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.autorturret") && _spells[SpellSlot.R].IsReady())
            {
                foreach (var enemy in HeroManager.Enemies.Where(HeroHelper.IsUnderAllyTurret))
                {
                    if (enemy.IsFacing(ObjectManager.Player) && enemy.IsValidTarget(_spells[SpellSlot.R].Range))
                    {
                        _spells[SpellSlot.R].CastIfHitchanceEquals(enemy, MenuHelper.GetHitchance());
                    }
                }
            }    
        }

        void AutoRLow()
        {
            if (MenuHelper.isMenuEnabled("dzaio.cassiopeia.misc.autorlow"))
            {
                var inRange = ObjectManager.Player.CountEnemiesInRange(450f);
                if (inRange > 0 && ObjectManager.Player.HealthPercentage() <= 20 && _spells[SpellSlot.R].IsReady())
                {
                    var closestTarget = ObjectManager.Player.GetEnemiesInRange(450f).OrderBy(h => h.Distance(ObjectManager.Player.ServerPosition)).First();
                    var rPrediction = _spells[SpellSlot.R].GetPrediction(closestTarget);
                    var enemiesFacing = HeroManager.Enemies.FindAll(enemy => _spells[SpellSlot.R].WillHit(enemy, rPrediction.CastPosition) && enemy.IsFacing(ObjectManager.Player));
                    if (enemiesFacing.Count > 0)
                    {
                        _spells[SpellSlot.R].Cast(rPrediction.CastPosition);
                    }
                }   
            }
        }

        bool WontOverkill(Obj_AI_Hero target)
        {
            return!(target.Health + 20 <= _spells[SpellSlot.E].GetDamage(target) + _spells[SpellSlot.Q].GetDamage(target));
        }

        bool CanKill(Obj_AI_Hero target)
        {
            var numberOfQ = 2;
            var numberOfE = 4;
            return target.Health + 20 <= _spells[SpellSlot.Q].GetDamage(target) * ((numberOfQ != 0) ? numberOfQ : 2) + _spells[SpellSlot.E].GetDamage(target) * ((numberOfE != 0) ? numberOfQ : 3);
        }

        bool IsTargetPoisoned(Obj_AI_Base target)
        {
            return target.HasBuffOfType(BuffType.Poison);
        }

        bool WillBePoisoned(Obj_AI_Base target,float delay)
        {
            var buffEndTime = target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time).Where(buff => buff.Type == BuffType.Poison).Select(buff => buff.EndTime).FirstOrDefault();
            return Game.Time - buffEndTime >= delay;
        }

        List<Obj_AI_Hero> PoisonedTargetInRange(float range)
        {
            return HeroManager.Enemies.FindAll(hero => hero.IsValidTarget(range) && IsTargetPoisoned(hero));
        } 

        void Drawing_OnDraw(EventArgs args)
        {
            
            DrawHelper.DrawSpellsRanges(_spells);
        }

        private static Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
