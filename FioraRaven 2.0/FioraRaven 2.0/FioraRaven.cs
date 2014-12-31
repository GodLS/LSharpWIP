using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Color = System.Drawing.Color; 

namespace FioraRaven_2._0
{
    class FioraRaven
    {
        private static readonly String champName = "Fiora";
        public static Obj_AI_Hero Player;
        private static Menu Menu;

        private static Spell Q, W, E, R;

        private static Orbwalking.Orbwalker Orbwalker;

        private static readonly StringList QMode = new StringList(new string[]{"Immediate","Distance","Time"},2);
        private static readonly StringList HMode = new StringList(new string[] { "Q,E,Q Back","Normal" });
        private static readonly StringList QModeTwo = new StringList(new string[] { "Single Target","Different Targets" }, 0);
        
        private static Obj_AI_Hero currentQTarget;
        private static Obj_AI_Hero currentHarrassTarget;

        private static HarrassEnum currentStatus;
        private static float QTickCount = 0f;
        private static bool can2NdQ = false;
        private static float LastMoveCommandT;
        public FioraRaven()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        void Game_OnGameLoad(EventArgs args)
        {
            setUpMenu();
            setUpSpells();

            Player = ObjectManager.Player;

            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "FioraQ")
                {
                    currentQTarget = args.Target as Obj_AI_Hero;
                    QTickCount = Environment.TickCount;
                }
            }
            else
            {
                
            }
        }

        
        void Game_OnGameUpdate(EventArgs args)
        {
            var RTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if(Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed) currentStatus = HarrassEnum.None;
            
            checkQStatus();
            QModeManager();
            castR(RTarget);
            FleeMode();
        }

        

        void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && currentStatus == HarrassEnum.E)
                {
                    currentStatus = HarrassEnum.AAAfterE;
                }
                castE();
                can2NdQ = true;
            }     
        }

        void Drawing_OnDraw(EventArgs args)
        {
           
        }

        void QModeManager()
        {
            
            if (!Q.IsReady())
                return;
            var QModeStart = Menu.Item("QComboMode").GetValue<StringList>().SelectedIndex; //Single Target, Different Targets
            var QModeSecond = Menu.Item("2ndQMode").GetValue<StringList>().SelectedIndex;//Immediate,Distance,Time
            var target = (QModeStart == 0)
                ? currentQTarget
                : TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            
            
            if (isSecondQ())
            {
                if (Q.GetDamage(target) > target.Health)
                {
                    castQ(target);
                }

                if (!target.IsValidTarget(Q.Range))
                    return;
                if(!can2NdQ)return;
                switch (QModeSecond)
                {       
                    case 0:
                        castQ(target);
                        break;
                    case 1:
                        if (Player.Distance(target) >= Menu.Item("Q_Dist").GetValue<Slider>().Value)
                        {
                            castQ(target);
                        }
                        break;
                    case 2:
                        if (Environment.TickCount - QTickCount >= Menu.Item("Q_Time").GetValue<Slider>().Value)
                        {
                            castQ(target);
                        }
                        break;
                }
            }
            else
            {
                var Target_First = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                castQ(Target_First);
            }
        }

        void doHarrass()
        {
            currentStatus = getStatusFromSpells();
            if (!currentHarrassTarget.IsValidTarget() && currentStatus == HarrassEnum.None)
            {
                currentHarrassTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            }
            switch (currentStatus)
            {
                case HarrassEnum.FirstQ:
                    castQ(currentHarrassTarget);
                    currentStatus = HarrassEnum.E;
                    break;
                case HarrassEnum.E:
                    castE();
                    break;
                case HarrassEnum.AAAfterE:
                    var minToBack =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .OrderByDescending(h => h.Distance(currentHarrassTarget))
                            .First(m => m.Distance(Player) <= Q.Range && m.IsValidTarget());
                    if (minToBack != null)
                    {
                        Q.Cast(minToBack);
                        currentStatus = HarrassEnum.None;
                    }
                    break;
                default:
                    break;
            }
        }

        HarrassEnum getStatusFromSpells()
        {
            if (Q.IsReady() && E.IsReady())
                return HarrassEnum.FirstQ;
            if (isSecondQ() && E.IsReady())
                return HarrassEnum.E;
            return HarrassEnum.None;
        }
        void FleeMode()
        {
            if(Environment.TickCount - LastMoveCommandT < 80 || !Menu.Item("Flee").GetValue<KeyBind>().Active)return;

            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            LastMoveCommandT = Environment.TickCount;

            if (!Q.IsReady())
                return;

            var toQ = ObjectManager.Get<Obj_AI_Minion>().OrderByDescending(h => h.Distance(Player)).First(m => m.IsValidTarget(Q.Range));
            if (toQ.Distance(Player) > (Q.Range /4 ) && Environment.TickCount - QTickCount > 250)
            {
                Q.Cast(toQ);
            }
            
        }

        void castR (Obj_AI_Base target)
        {
            if (!R.IsReady() || !target.IsValidTarget(R.Range))
                return;
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    var RManaC = getManaMode("C", "R");
                    if (Player.ManaPercentage() >= RManaC && isMenuEnabled("UseRC") && R.GetDamage(target)> target.Health)
                        R.Cast(target);
                    break;
            }
        }

        void castQ(Obj_AI_Base target)
        {
            if (!Q.IsReady() || !target.IsValidTarget())
                return;
            
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    var QManaC = getManaMode("C", "Q");
                   
                    if (Player.ManaPercentage() >= QManaC && isMenuEnabled("UseQC"))
                        Q.Cast(target);
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    var QManaH = getManaMode("H", "Q");
                    if (Player.ManaPercentage() >= QManaH && isMenuEnabled("UseQH"))
                        Q.Cast(target);
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    var QManaLH = getManaMode("C", "Q");
                    if (Player.ManaPercentage() >= QManaLH)
                        Q.Cast(target);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    var QManaLC = getManaMode("C", "Q");
                    if (Player.ManaPercentage() >= QManaLC)
                        Q.Cast(target);
                    break;
            }
        }
        void castE()
        {
            if (!E.IsReady())
                return;
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    var EManaC = getManaMode("C", "E");
                    if (Player.ManaPercentage() >= EManaC && isMenuEnabled("UseEC"))
                        E.Cast();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    var EManaH = getManaMode("H", "E");
                    if (Player.ManaPercentage() >= EManaH && isMenuEnabled("UseEH"))
                        E.Cast();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    var EManaLH = getManaMode("C", "E");
                    if (Player.ManaPercentage() >= EManaLH)
                        E.Cast();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    var EManaLC = getManaMode("C", "E");
                    if (Player.ManaPercentage() >= EManaLC)
                        E.Cast();
                    break;
            }
        }

        #region Utility
        float getManaMode(String Mode, String Skill)
        {
            return Menu.Item(Skill + "Mana" + Mode).GetValue<Slider>().Value;
        }

        bool isSecondQ()
        {
            return Player.HasBuff("fioraqcd", true);
        }

        bool isMenuEnabled(String option)
        {
            return Menu.Item(option).GetValue<bool>();
        }
        private void checkQStatus()
        {
            if (!Q.IsReady())
            {
                currentQTarget = null;
                can2NdQ = false;
                QTickCount = 0f;
            }        
        }

        #endregion

        #region HarrassEnum
        public enum HarrassEnum
        {
            None,
            FirstQ,
            E,
            AAAfterE,
            SecondQ
        }
        #endregion
        
        #region Spells
        void setUpSpells()
        {
            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 400f);
        }
        #endregion

        #region Menu
        void setUpMenu()
        {
            

            Menu = new Menu("Fiora Raven", "fiora_raven", true);

            var OW_Menu = new Menu("[FR] Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(OW_Menu);

            var TS_Menu = new Menu("[FR] Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(TS_Menu);

            Menu.AddSubMenu(OW_Menu);
            Menu.AddSubMenu(TS_Menu);

            var ComboMenu = new Menu("[FR] Combo", "Combo");
            {
                ComboMenu.AddItem(new MenuItem("UseQC", "Use Q Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("UseEC", "Use E Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("UseRC", "Use R Combo").SetValue(true));
                ComboMenu.AddItem(new MenuItem("UseIgn", "Use Ignite").SetValue(true));
                ComboMenu.AddItem(new MenuItem("QComboMode", "Q Combo Mode").SetValue(QModeTwo));
                ComboMenu.AddItem(new MenuItem("2ndQMode", "2nd Q Mode").SetValue(QMode));
                ComboMenu.AddItem(new MenuItem("Q_Dist", "2nd Q if dist >").SetValue(new Slider(200, 1, 550)));
                ComboMenu.AddItem(new MenuItem("Q_Time", "2nd Q After (ms)").SetValue(new Slider(650, 0, 3900)));
            }
            var ManaManagerCombo = new Menu("Mana Manager", "mm_Combo");
            {
                ManaManagerCombo.AddItem(new MenuItem("QManaC", "Q Mana Combo").SetValue(new Slider(35)));
                ManaManagerCombo.AddItem(new MenuItem("EManaC", "E Mana Combo").SetValue(new Slider(25)));
                ManaManagerCombo.AddItem(new MenuItem("RManaC", "R Mana Combo").SetValue(new Slider(15)));
            }
            ComboMenu.AddSubMenu(ManaManagerCombo);
            Menu.AddSubMenu(ComboMenu);

            var HarassMenu = new Menu("[FR] Harass", "Harass");
            {
                HarassMenu.AddItem(new MenuItem("UseQH", "Use Q Harass").SetValue(true));
                HarassMenu.AddItem(new MenuItem("UseEH", "Use E Harass").SetValue(true));
                HarassMenu.AddItem(new MenuItem("HMode", "Harass Mode").SetValue(HMode));
            }
            var ManaManagerHarass = new Menu("Mana Manager", "mm_Harass");
            {
                ManaManagerHarass.AddItem(new MenuItem("QManaH", "Q Mana Harass").SetValue(new Slider(35)));
                ManaManagerHarass.AddItem(new MenuItem("WManaH", "W Mana Harass").SetValue(new Slider(15)));
                ManaManagerHarass.AddItem(new MenuItem("EManaH", "E Mana Harass").SetValue(new Slider(25)));
            }
            HarassMenu.AddSubMenu(ManaManagerHarass);
            Menu.AddSubMenu(HarassMenu);

            var MiscMenu = new Menu("[FR] Misc", "Misc");
            {
                MiscMenu.AddItem(new MenuItem("RDodge","R To dodge dangerous").SetValue(true));
                MiscMenu.AddItem(new MenuItem("WBlock", "W To Block").SetValue(true));
                MiscMenu.AddItem(new MenuItem("Flee", "Flee Key").SetValue(new KeyBind("S".ToCharArray()[0],KeyBindType.Press)));
            }
            Menu.AddSubMenu(MiscMenu);

            var ItemsMenu = new Menu("[FR] Items", "Items");
            {
                ItemsMenu.AddItem(new MenuItem("BotrkC", "Botrk Combo").SetValue(true));
                ItemsMenu.AddItem(new MenuItem("BotrkH", "Botrk Harrass").SetValue(false));
                ItemsMenu.AddItem(new MenuItem("YoumuuC", "Youmuu Combo").SetValue(true));
                ItemsMenu.AddItem(new MenuItem("YoumuuH", "Youmuu Harrass").SetValue(false));
                ItemsMenu.AddItem(new MenuItem("BilgeC", "Cutlass Combo").SetValue(true));
                ItemsMenu.AddItem(new MenuItem("BilgeH", "Cutlass Harrass").SetValue(false));
                ItemsMenu.AddItem(new MenuItem("OwnHPercBotrk", "Min Own H. % Botrk").SetValue(new Slider(50, 1)));
                ItemsMenu.AddItem(new MenuItem("EnHPercBotrk", "Min Enemy H. % Botrk").SetValue(new Slider(20, 1)));
            }
            Menu.AddSubMenu(ItemsMenu);

            var DrawMenu = new Menu("[FR] Drawing", "Drawing");
            {
                DrawMenu.AddItem(new MenuItem("DrawQ", "Draw Q").SetValue(new Circle(true, Color.Red)));
                DrawMenu.AddItem(new MenuItem("DrawR", "Draw R").SetValue(new Circle(true, Color.Red)));
            }
            Menu.AddSubMenu(DrawMenu);

            Menu.AddToMainMenu();
        }
        #endregion
    }
    
}
