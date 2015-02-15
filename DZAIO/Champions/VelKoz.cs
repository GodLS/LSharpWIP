using System;
using System.Collections.Generic;
using System.Linq;
using DZAIO.Utility;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;

namespace DZAIO.Champions
{
    class VelKoz : IChampion
    {
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 1200f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 1200f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 800f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1550f) }
        };

        private static GameObject qProjectile;

        public void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;
            var comboMenu = new Menu(cName + " - Combo", "dzaio.velkoz.combo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { true, true, true, true });

            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 30, 35, 20, 5 });

            var skillOptionMenu = new Menu("Skill Options", "dzaio.velkoz.combo.skilloptions");
            comboMenu.AddSubMenu(skillOptionMenu);

            menu.AddSubMenu(comboMenu);
            var harrassMenu = new Menu(cName + " - Harrass", "dzaio.velkoz.harass");
            harrassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { true, true, false });
            harrassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 30, 35, 20 });
            menu.AddSubMenu(harrassMenu);
            var farmMenu = new Menu(cName + " - Farm", "dzaio.velkoz.farm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.E }, new[] { false });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.E }, new[] { 35 });
            menu.AddSubMenu(farmMenu);
            var miscMenu = new Menu(cName + " - Misc", "dzaio.velkoz.misc");
            {
            }
            miscMenu.AddHitChanceSelector();
            menu.AddSubMenu(miscMenu);
            var drawMenu = new Menu(cName + " - Drawings", "dzaio.velkoz.drawing");
            drawMenu.AddDrawMenu(_spells, Color.Aquamarine);
            menu.AddSubMenu(drawMenu);
        }

        public void RegisterEvents()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
        }

        void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if ((sender is Obj_SpellMissile))
            {
                
            }
        }

        public void SetUpSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.25f, 50f, 1300f, true, SkillshotType.SkillshotLine);
            _spells[SpellSlot.W].SetSkillshot(0.25f, 85f, 1700f, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.E].SetSkillshot(0.5f, 100f, 1500f, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.R].SetSkillshot(0.3f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
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
            var comboTarget = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
            {
                var qHitchance = _spells[SpellSlot.Q].GetPrediction(comboTarget);
            }
        }

        private void Harrass()
        {

        }

        private void Farm()
        {

        }

        void Drawing_OnDraw(EventArgs args)
        {
            throw new NotImplementedException();
        }

        void CastQ()
        {
            Dictionary<Obj_AI_Hero,HitChance> hitDictionary = new Dictionary<Obj_AI_Hero, HitChance>();
            foreach (var hero in HeroManager.Enemies.Where(h => h.IsValidTarget(_spells[SpellSlot.Q].Range)))
            {
                var qPrediction = _spells[SpellSlot.Q].GetPrediction(hero);
                hitDictionary.Add(hero,qPrediction.Hitchance);
            }

            //Check for Q Split

        }

        void CheckQSplit()
        {
            
        }

        private Orbwalking.Orbwalker _orbwalker
        {
            get { return DZAIO.Orbwalker; }
        }
    }
}
