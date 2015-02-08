using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Champions
{
    class Cassiopeia : IChampion
    {
        private static float _lastCastedETick;
        private static float _lastCastedQTick;
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
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { true, true, false });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 30, 35, 20 });
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "dzaio.cassiopeia.farm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.Q,SpellSlot.W,SpellSlot.E }, new[] { false,false,false });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E}, new[] { 35,35,35 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "dzaio.cassiopeia.misc");
            {
            }
            miscMenu.AddHitChanceSelector();
            var humanizerMenu = new Menu("Humanizer", "dzaio.cassiopeia.misc.humanizer");
            {
                humanizerMenu.AddItem(new MenuItem("dzaio.cassiopeia.misc.humanizer.edelay", "E Delay").SetValue(new Slider(300,0, 1500)));
            }
            miscMenu.AddSubMenu(humanizerMenu);
            menu.AddSubMenu(miscMenu);
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
            //TODO Change these
            _spells[SpellSlot.Q].SetSkillshot(0.6f, 40f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.W].SetSkillshot(0.5f, 90f, 2500, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.E].SetTargetted(0.2f, float.MaxValue);
            _spells[SpellSlot.R].SetSkillshot(0.6f, (float)(80 * Math.PI / 180), float.MaxValue, false, SkillshotType.SkillshotCone);
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            //DebugHelper.AddEntry("QLastTick", _spells[SpellSlot.Q].LastCastAttemptT.ToString());
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
                if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo))
                {
                    if (MenuHelper.getSliderValue("dzaio.cassiopeia.combo.skilloptions.minwenemies") == 1 && (MenuHelper.isMenuEnabled("dzaio.cassiopeia.combo.skilloptions.onlywnotpoison") && !IsTargetPoisoned(comboTarget)))
                    {
                        _spells[SpellSlot.W].CastIfHitchanceEquals(comboTarget, MenuHelper.GetHitchance());
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
                        _spells[SpellSlot.R].Cast(rPrediction.CastPosition);
                    }
                }
            }
        }

        void Harrass()
        {

        }

        void Farm()
        {

        }

        bool CanKill(Obj_AI_Hero target)
        {
            var numberOfQ = 2000f / (_spells[SpellSlot.Q].Instance.Cooldown + 0.25f);
            var numberOfE = (2000f-numberOfQ*0.25f) / (_spells[SpellSlot.E].Instance.Cooldown + 0.5f);
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

        private Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
