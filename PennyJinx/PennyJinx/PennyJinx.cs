using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace PennyJinx
{
    internal class PennyJinx
    {
        private const String ChampName = "Jinx";
        private const HitChance CustomHitChance = HitChance.Medium;
        public static Obj_AI_Hero Player;
        private static Spell _q, _w, _e, _r;
        public static Menu Menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly StringList QMode = new StringList(new[] {"AOE mode", "Range mode", "Both"}, 2);

        public PennyJinx()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != ChampName)
            {
                return;
            }

            SetUpMenu();
            SetUpSpells();

            Game.PrintChat("<font color='#7A6EFF'>PennyJinx</font> <font color='#FFFFFF'>Loaded!</font>");

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var target = args.Target;
            if (!target.IsValidTarget())
                return;
            if (!(target is Obj_AI_Minion) || _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                return;
            var t2 = target as Obj_AI_Minion;
            if (CountEnemyMinions(t2, 150) < 4)
            {
                SwitchLc();
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            //TODO Stuff
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Auto();
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    ComboLogic();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    HarrassLogic();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    SwitchLc();
                    break;
            }
        }

        

        #region Various

        private void SwitchLc()
        {
            if (!IsMenuEnabled("SwitchQLC") || !_q.IsReady())
                return;

            if (IsFishBone())
            {
                _q.Cast();
            }
        }

        #endregion

        #region Drawing

        private static void Drawing_OnDraw(EventArgs args)
        {
            {
                if (!IsMenuEnabled("DrawW"))
                {
                    return;
                }
                {
                Utility.DrawCircle(ObjectManager.Player.Position, 1500f, System.Drawing.Color.CornflowerBlue);
                }              
            }
        }

        #endregion

        #region Combo/Harrass/Auto

        private void Auto()
        {  
            AutoWEmpaired();
            AutoWEmpaired();
            if(getEMode() == 0){ECast_DZ();} else{ ECast();}
        }

        private void HarrassLogic()
        {
            WCast(_orbwalker.ActiveMode);
            QManager("H");
        }

        private void ComboLogic()
        {    
            WCast(_orbwalker.ActiveMode);
            RCast();
            QManager("C");
            if (getEMode() == 0) { ECast_DZ(); } else { ECast(); }
        }
        #endregion

        #region Spell Casting

        private void QManager(String Mode)
        {
            if (!_q.IsReady())
            {
                return;
            }

            var aaRange = Orbwalking.GetRealAutoAttackRange(null);
            var target = TargetSelector.GetTarget(aaRange + GetFishboneRange() + 65, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget(aaRange + GetFishboneRange() + 65))
            {
                return;
            }

            switch (Menu.Item("QMode").GetValue<StringList>().SelectedIndex)
            {
                    //AOE Mode
                case 0:
                    if (IsFishBone() && GetPerValue(true) <= GetSliderValue("QMana" + Mode))
                    {
                        _q.Cast();
                        return;
                    }
                    if (target.CountEnemysInRange(150) > 1)
                    {
                        if (!IsFishBone())
                        {
                            _q.Cast();
                        }
                    }
                    else
                    {
                        if (IsFishBone() )
                        {
                            _q.Cast();
                        }
                    }
                    break;
                    //Range Mode
                case 1:
                    if (IsFishBone())
                    {
                        //Switching to Minigun
                        if (Player.Distance(target) < aaRange || GetPerValue(true) <= GetSliderValue("QMana" + Mode))
                        {
                            _q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > aaRange && GetPerValue(true) >= GetSliderValue("QMana" + Mode))
                        {
                            _q.Cast();
                        }
                    }
                    break;
                    //Both
                case 2:
                    if (IsFishBone())
                    {
                        //Switching to Minigun
                        if (Player.Distance(target) < aaRange || GetPerValue(true) <= GetSliderValue("QMana" + Mode))
                        {
                            _q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > aaRange && GetPerValue(true) >= GetSliderValue("QMana" + Mode) ||
                            target.CountEnemysInRange(150) > 1)
                        {
                            _q.Cast();
                        }
                    }
                    break;
            }
        }

        private void WCast(Orbwalking.OrbwalkingMode mode)
        {
            if (mode != Orbwalking.OrbwalkingMode.Combo && mode != Orbwalking.OrbwalkingMode.Mixed || !_w.IsReady())
            {
                return;
            }
            if (Player.CountEnemysInRange((int) Player.AttackRange) != 0)
                return;

            //If the mode is combo then we use the WManaC, if the mode is Harrass we use the WManaH
            var str = (mode == Orbwalking.OrbwalkingMode.Combo) ? "C" : "H";
            //Get a target in W range
            var wTarget = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            if (!wTarget.IsValidTarget(_w.Range))
                return;

            var wMana = GetSliderValue("WMana" + str);
            if (GetPerValue(true) >= wMana && IsMenuEnabled("UseWC"))
            {
                _w.CastIfHitchanceEquals(wTarget, CustomHitChance, Packets());
            }
        }

        private void ECast()
        {
            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(_e.Range - 150)))
            {
                //Something from Marksman
                if (IsMenuEnabled("AutoE") && _e.IsReady() && enemy.HasBuffOfType(BuffType.Slow))
                {
                    var castPosition =
                        Prediction.GetPrediction(
                            new PredictionInput
                            {
                                Unit = enemy,
                                Delay = 0.7f,
                                Radius = 120f,
                                Speed = 1750f,
                                Range = 900f,
                                Type = SkillshotType.SkillshotCircle,
                            }).CastPosition;
                    if (GetSlowEndTime(enemy) >= (Game.Time + _e.Delay + 0.5f))
                    {
                        _e.Cast(castPosition);
                    }
                    if (IsMenuEnabled("AutoE") && _e.IsReady() &&
                    (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                    enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                    enemy.HasBuffOfType(BuffType.Taunt)))
                    {
                        _e.CastIfHitchanceEquals(enemy, HitChance.High);
                    }
                }
            }
        }

        private void ECast_DZ()
        {
            if(!_e.IsReady())
                return;

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(_e.Range - _e.Width) && (IsEmpaired(h))))
            {
                //E necessary mana. If the mode is combo: Combo mana, if not AutoE mana
                var EMana = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo
                    ? GetSliderValue("EManaC")
                    : GetSliderValue("AutoE_Mana");
                
                if (IsMenuEnabled("UseEC") || IsMenuEnabled("AutoE"))
                {
                    //If it is slowed & moving
                    if (IsEmpairedLight(enemy) && isMoving(enemy))
                    { 
                        //Has enough E Mana ?
                        if (GetPerValue(true) >= EMana)
                        {
                            //Casting using predictions
                            _e.CastIfHitchanceEquals(enemy, HitChance.High, Packets());
                            return;
                        }
                    }
                    //If the empairement ends later, cast the E
                    if (GetPerValue(true) >= EMana)
                    {
                        //Casting using predictions
                        _e.CastIfHitchanceEquals(enemy, HitChance.High, Packets());
                    }
                }
            }
        }

        private void RCast()
        {
            //TODO R Collision
            if (!_r.IsReady())
            {
                return;
            }

            var rTarget = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Physical);
            if (!rTarget.IsValidTarget(_r.Range))
            {
                return;
            }
            //If the distance is lower than Rocket Range && it's rockets
            //Or the distance is lower than minigun range && it's minigun
            //The target can be killed with the X autoattack buffer
            //There are allies that could killsteal it
            //Or the distance is too close compared to the buffer
            if ((rTarget.Distance(Player) <= Player.AttackRange+GetFishboneRange() && IsFishBone()) ||
                (rTarget.Distance(Player) <= Player.AttackRange && !IsFishBone()) &&
                (rTarget.Health < Player.GetAutoAttackDamage(rTarget) * GetSliderValue("AABuffer")) ||
                CountAllyPlayers(rTarget,400) > 0 ||
                Player.Distance(rTarget)<= GetSliderValue("MinRDist"))
            {
                return;
            }

            //Check for Mana && for target Killable. Also check for hitchance
            if (GetPerValue(true) >= GetSliderValue("RManaC") && IsMenuEnabled("UseRC") &&
                _r.GetDamage(rTarget) >=
                HealthPrediction.GetHealthPrediction(rTarget, (int)(Player.Distance(rTarget) / 2000f)*1000))
            {
                _r.CastIfHitchanceEquals(rTarget,CustomHitChance, Packets());
            }
        }

        #endregion

        #region AutoSpells

        private void AutoWHarass()
        {
            //Uses W in Harrass, factoring hitchance
            if (!IsMenuEnabled("AutoW"))
            {
                return;
            }

            var wTarget = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            var autoWMana = GetSliderValue("AutoW_Mana");
            if (GetPerValue(true) >= autoWMana || isKillableWAA(wTarget))
            {
                _w.CastIfHitchanceEquals(wTarget, CustomHitChance, Packets());
                
            }
        }

        private void AutoWEmpaired()
        {
            //Uses W on whoever is empaired
            foreach (
                var enemy in
                    from enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(_w.Range))
                    let autoWMana = GetSliderValue("AutoW_Mana")
                    where GetPerValue(true) >= autoWMana
                    select enemy)
            {
                _w.CastIfHitchanceEquals(enemy, CustomHitChance, Packets());
            }
        }

        #endregion

        #region Utility

        private bool Packets()
        {
            return IsMenuEnabled("Packets");
        }

        private static float GetFishboneRange()
        {
            return 50 + 25*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static bool isKillableWAA(Obj_AI_Hero wTarget)
        {

            return (Player.GetAutoAttackDamage(wTarget) + _w.GetDamage(wTarget) >
                    HealthPrediction.GetHealthPrediction(
                        wTarget,
                        (int)
                            ((Player.Distance(wTarget) / _w.Speed) * 1000 +
                             (Player.Distance(wTarget) / Orbwalking.GetMyProjectileSpeed()) * 1000) + (Game.Ping / 2)) &&
                    Player.Distance(wTarget) > Orbwalking.GetRealAutoAttackRange(null));

        }


        private static int CountAllyPlayers(Obj_AI_Hero from,float distance)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe && h.Distance(from) <= distance).ToList().Count;
        }

        private static int CountEnemyMinions(Obj_AI_Base from, float distance)
        {
            return MinionManager.GetMinions(from.Position, distance).ToList().Count;
        }

        private static bool IsFishBone()
        {
            return Player.AttackRange > 565;
        }

        private int getEMode()
        {
            return Menu.Item("EMode").GetValue<StringList>().SelectedIndex;
        }

        private static bool IsEmpaired(Obj_AI_Hero enemy)
        {
            return (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                    enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                    enemy.HasBuffOfType(BuffType.Taunt) || IsEmpairedLight(enemy));
        }

        private static bool IsEmpairedLight(Obj_AI_Hero enemy)
        {
            return (enemy.HasBuffOfType(BuffType.Slow));
        }

        private static float GetEmpairedEndTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => getEmpairedBuffs().Contains(buff.Type))
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }

        private static float GetSlowEndTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Slow)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }

        private static bool IsMenuEnabled(String opt)
        {
            return Menu.Item(opt).GetValue<bool>();
        }

        private static int GetSliderValue(String opt)
        {
            return Menu.Item(opt).GetValue<Slider>().Value;
        }

        private static float GetPerValue(bool mana)
        {
            return mana ? Player.ManaPercentage() : Player.HealthPercentage();
        }

        private static bool isMoving(Obj_AI_Base obj)
        {
            return obj.Path.Count() > 1;
        }

        private static List<BuffType> getEmpairedBuffs()
        {
            return new List<BuffType> { BuffType.Stun, BuffType.Snare, BuffType.Charm, BuffType.Fear, BuffType.Taunt, BuffType.Slow};
        }

        #endregion

        #region Menu and spells setup

        private static void SetUpSpells()
        {
            _q = new Spell(SpellSlot.Q);
            _w = new Spell(SpellSlot.W, 1500f);
            _e = new Spell(SpellSlot.E, 900f);
            _r = new Spell(SpellSlot.R, 2000f);
            _w.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            _e.SetSkillshot(1.1f, 120f, 1750f, false, SkillshotType.SkillshotCircle);
            _r.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);
        }

        private static void SetUpMenu()
        {
            Menu = new Menu("PennyJinx", "PJinx", true);

            var orbMenu = new Menu("Orbwalker", "OW");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            var tsMenu = new Menu("Target Selector", "TS");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(orbMenu);
            Menu.AddSubMenu(tsMenu);
            var comboMenu = new Menu("[PJ] Combo", "Combo");
            {
                comboMenu.AddItem(new MenuItem("UseQC", "Use Q Combo").SetValue(true));
                comboMenu.AddItem(new MenuItem("UseWC", "Use W Combo").SetValue(true));
                comboMenu.AddItem(new MenuItem("UseEC", "Use E Combo").SetValue(true));
                comboMenu.AddItem(new MenuItem("UseRC", "Use R Combo").SetValue(true));
                comboMenu.AddItem(new MenuItem("QMode", "Q Usage Mode").SetValue(QMode));
                comboMenu.AddItem(new MenuItem("EMode", "E Mode").SetValue(new StringList(new []{"PennyJinx","Marksman"})));
                comboMenu.AddItem(new MenuItem("AABuffer", "AA Buffer for R").SetValue(new Slider(2, 0, 5)));
                comboMenu.AddItem(new MenuItem("MinRDist", "Min R Distance").SetValue(new Slider(500, 0, 1200)));
            }
            var manaManagerCombo = new Menu("Mana Manager", "mm_Combo");
            {
                manaManagerCombo.AddItem(new MenuItem("QManaC", "Q Mana Combo").SetValue(new Slider(15)));
                manaManagerCombo.AddItem(new MenuItem("WManaC", "W Mana Combo").SetValue(new Slider(35)));
                manaManagerCombo.AddItem(new MenuItem("EManaC", "E Mana Combo").SetValue(new Slider(25)));
                manaManagerCombo.AddItem(new MenuItem("RManaC", "R Mana Combo").SetValue(new Slider(5)));
            }
            comboMenu.AddSubMenu(manaManagerCombo);
            Menu.AddSubMenu(comboMenu);

            var harassMenu = new Menu("[PJ] Harrass", "Harass");
            {
                harassMenu.AddItem(new MenuItem("UseQH", "Use Q Harass").SetValue(true));
                harassMenu.AddItem(new MenuItem("UseWH", "Use W Harass").SetValue(true));
            }
            var manaManagerHarrass = new Menu("Mana Manager", "mm_Harrass");
            {
                manaManagerHarrass.AddItem(new MenuItem("QManaH", "Q Mana Harass").SetValue(new Slider(15)));
                manaManagerHarrass.AddItem(new MenuItem("WManaH", "W Mana Harass").SetValue(new Slider(35)));
            }
            harassMenu.AddSubMenu(manaManagerHarrass);
            Menu.AddSubMenu(harassMenu);

            var miscMenu = new Menu("[PJ] Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("Packets", "Use Packets").SetValue(true));
                miscMenu.AddItem(new MenuItem("AntiGP", "Anti Gapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("Interrupter", "Use Interrupter").SetValue(true));
                miscMenu.AddItem(new MenuItem("SwitchQLC", "Switch Minigun Laneclear").SetValue(true));
                miscMenu.AddItem(new MenuItem("DrawW", "Draw W range").SetValue(false));
            }
            Menu.AddSubMenu(miscMenu);

            var autoMenu = new Menu("[PJ] Auto Harrass", "Auto");
            {
                autoMenu.AddItem(new MenuItem("AutoE", "Auto E Slow/Immobile").SetValue(true));
                autoMenu.AddItem(new MenuItem("AutoE_Mana", "Auto E Mana").SetValue(new Slider(35)));
                autoMenu.AddItem(new MenuItem("AutoW", "Auto W").SetValue(true));
                autoMenu.AddItem(new MenuItem("AutoW_Mana", "Auto W Mana").SetValue(new Slider(40)));
            }
            Menu.AddSubMenu(autoMenu);

            Menu.AddToMainMenu();
        }

        #endregion
    }
}