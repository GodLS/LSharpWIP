using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace PennyJinx
{
    class PennyJinx
    {
        private static readonly String champName = "Jinx";
        public static Obj_AI_Hero Player;

        private static Spell Q, W, E, R;

        public static Menu Menu;
        private static Orbwalking.Orbwalker Orbwalker;

        private static readonly StringList QMode = new StringList(new []{"AOE mode","Range mode","Both"},2);

        private static HitChance customHitChance = HitChance.Medium;

        public PennyJinx()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != champName)
                return;

            setUpMenu();
            setUpSpells();

            Game.PrintChat("PennyJinx Loaded!");
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
        }

        void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    
                    ComboLogic();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:

                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    switchLC();
                    break;
                default:
                    break;
            }
        }
        #region Various

        void switchLC()
        {
            if (!isMenuEnabled("SwitchQLC") || !Q.IsReady())
                return;
            if (isFishBone())
                Q.Cast();
        }
        #endregion


        #region Combo Logic

        void ComboLogic()
        {
           
            WCast(Orbwalker.ActiveMode);
            ECast();
            RCast();
            QManager();
        }


        void QManager()
        {
            
            if (!Q.IsReady())
                return;
            var AARange = Orbwalking.GetRealAutoAttackRange(null);
            var target = TargetSelector.GetTarget(AARange + getFishboneRange() + 65, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget(AARange+getFishboneRange()+65))
                return;
           
            switch (Menu.Item("QMode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    if (target.CountEnemysInRange(150) > 1)
                    {
                       if(!isFishBone())
                           Q.Cast();

                    }else
                    {
                        if(isFishBone())
                            Q.Cast();
                    }
                    break;
                case 1:
                    if (isFishBone())
                    {
                        //Switching to Minigun
                        if (Player.Distance(target) < AARange || getPerValue(true) <= getSliderValue("QManaC"))
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > AARange && getPerValue(true) >= getSliderValue("QManaC"))
                        {
                            Q.Cast();
                        }
                    }
                    break;
                case 2:
                    if (isFishBone())
                    {
                        //Switching to Minigun
                        if (Player.Distance(target) < AARange || getPerValue(true) <= getSliderValue("QManaC"))
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > AARange && getPerValue(true) >= getSliderValue("QManaC") || target.CountEnemysInRange(150) > 1)
                        {
                            Q.Cast();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        void WCast(Orbwalking.OrbwalkingMode mode)
        {
            if (mode != Orbwalking.OrbwalkingMode.Combo && mode != Orbwalking.OrbwalkingMode.Mixed || !W.IsReady())
                return;

            var str = (mode == Orbwalking.OrbwalkingMode.Combo) ? "C" : "H";
            var WTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (!WTarget.IsValidTarget(W.Range))
                return;
            var WMana = getSliderValue("WMana"+str);

            if (getPerValue(true) >= WMana && isMenuEnabled("UseWC"))
            {
                W.CastIfHitchanceEquals(WTarget, customHitChance, Packets());
            }
        }

        void ECast()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range - 100) && isEmpaired(h)))
            {
                //TODO Stuff
            }
        }
        private void RCast()
        {
            if (!R.IsReady())
                return;
            var RTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (!RTarget.IsValidTarget(R.Range))
                return;
            if ((RTarget.Distance(Player) <= getFishboneRange() && isFishBone()) ||
                (RTarget.Distance(Player) <= Player.AttackRange && !isFishBone()) &&
                (RTarget.Health < Player.GetAutoAttackDamage(RTarget) * getSliderValue("AABuffer")))
                return;
            var Prediction = R.GetPrediction(RTarget);
            var CastPosition = Prediction.CastPosition;
            if (getPerValue(true) >= getSliderValue("RManaC") && isMenuEnabled("UseRC") && R.GetDamage(RTarget) >= HealthPrediction.GetHealthPrediction(RTarget,(int)(Player.Distance(RTarget)/2000f)))
            {
                R.Cast(CastPosition,Packets());
            }
        }
        #endregion


        #region Spell Casting


        #endregion


        #region Drawing

        void Drawing_OnDraw(EventArgs args)
        {
            
        }
        #endregion


        #region AutoSpells

        void AutoWHarass()
        {
            if (!isMenuEnabled("AutoW"))
                return;
            var WTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            var AutoWMana = getSliderValue("AutoW_Mana");
            if (getPerValue(true) >= AutoWMana)
            {
                W.CastIfHitchanceEquals(WTarget, customHitChance, Packets());
            }
        }

        void autoWEmpaired()
        {   
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
            {

                var AutoWMana = getSliderValue("AutoW_Mana");
                if (getPerValue(true) >= AutoWMana)
                {
                    W.CastIfHitchanceEquals(enemy, customHitChance, Packets());
                } 
            }
        }

        #endregion


        #region Utility

        bool Packets()
        {
            return isMenuEnabled("Packets");
        }
        float getFishboneRange()
        {
            return 50 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        bool isFishBone()
        {
            return Player.AttackRange > 565;
        }

        bool isEmpaired(Obj_AI_Hero enemy)
        {
            return (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow));
        }
        bool isMenuEnabled(String opt)
        {
            return Menu.Item(opt).GetValue<bool>();
        }

        int getSliderValue(String opt)
        {
            return Menu.Item(opt).GetValue<Slider>().Value;
        }

        float getPerValue(bool mana)
        {
            if (mana)
                return Player.ManaPercentage();
            return Player.HealthPercentage();
        }
        #endregion


        #region Menu and spells
        private void setUpSpells()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 25000f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.1f, 20f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);

        }

        private void setUpMenu()
        {
            Menu = new Menu("PennyJinx","PJinx",true);

            var orbMenu = new Menu("Orbwalker", "OW");
            Orbwalker = new Orbwalking.Orbwalker(orbMenu);
            var TSMenu = new Menu("Target Selector", "TS");
            TargetSelector.AddToMenu(TSMenu);
            Menu.AddSubMenu(orbMenu);
            Menu.AddSubMenu(TSMenu);
            var ComboMenu = new Menu("[PJ] Combo", "Combo");
            {
                ComboMenu.AddItem(new MenuItem("UseQC", "Use Q Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("UseWC", "Use W Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("UseEC", "Use E Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("UseRC", "Use R Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("QMode", "Q Usage Mode").SetValue(QMode));
                ComboMenu.AddItem(new MenuItem("AABuffer", "AA Buffer for R").SetValue(new Slider(2,0,5)));
            }
            var ManaManagerCombo = new Menu("Mana Manager", "mm_Combo");
            {
                ManaManagerCombo.AddItem(new MenuItem("QManaC", "Q Mana Combo").SetValue(new Slider(15)));
                ManaManagerCombo.AddItem(new MenuItem("WManaC", "W Mana Combo").SetValue(new Slider(35)));
                ManaManagerCombo.AddItem(new MenuItem("EManaC", "E Mana Combo").SetValue(new Slider(25)));
                ManaManagerCombo.AddItem(new MenuItem("RManaC", "R Mana Combo").SetValue(new Slider(5)));
            }
            ComboMenu.AddSubMenu(ManaManagerCombo);
            Menu.AddSubMenu(ComboMenu);

            var HarassMenu = new Menu("[PJ] Harrass", "Harass");
            {
                HarassMenu.AddItem(new MenuItem("UseQH", "Use Q Harass").SetValue(true));  
                HarassMenu.AddItem(new MenuItem("UseWH", "Use W Harass").SetValue(true));            
            }
            var ManaManagerHarrass = new Menu("Mana Manager", "mm_Harrass");
            {
                ManaManagerHarrass.AddItem(new MenuItem("QManaH", "Q Mana Harass").SetValue(new Slider(15)));
                ManaManagerHarrass.AddItem(new MenuItem("WManaH", "W Mana Harass").SetValue(new Slider(35)));
            }
            HarassMenu.AddSubMenu(ManaManagerHarrass);
            Menu.AddSubMenu(HarassMenu);

            var MiscMenu = new Menu("[PJ] Misc", "Misc");
            {
                MiscMenu.AddItem(new MenuItem("Packets", "Use Packets").SetValue(true));
                MiscMenu.AddItem(new MenuItem("AntiGP", "Anti Gapcloser").SetValue(true));
                MiscMenu.AddItem(new MenuItem("Interrupter", "Use Interrupter").SetValue(true));               
                MiscMenu.AddItem(new MenuItem("SwitchQLC", "Switch Minigun Laneclear").SetValue(true));
            }
            Menu.AddSubMenu(MiscMenu);

            var AutoMenu = new Menu("[PJ] Auto Harrass", "Auto");
            {
                AutoMenu.AddItem(new MenuItem("AutoE", "Auto E Slow/Immobile").SetValue(true));
                AutoMenu.AddItem(new MenuItem("AutoE_Mana", "Auto E Mana").SetValue(new Slider(35)));
                AutoMenu.AddItem(new MenuItem("AutoW", "Auto W").SetValue(true));
                AutoMenu.AddItem(new MenuItem("AutoW_Mana", "Auto W Mana").SetValue(new Slider(40)));
            }
            Menu.AddSubMenu(AutoMenu);

            Menu.AddToMainMenu();
        }
        #endregion


    }
}
