using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace KawaiiLucian
{
    class KawaiiLucian
    {
        public static MenuWrapper Menu;
        private static String champName = "Lucian";
        private static Obj_AI_Hero Player;

        internal static Dictionary<string, MenuWrapper.BoolLink> boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        internal static Dictionary<string, MenuWrapper.CircleLink> circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        internal static Dictionary<string, MenuWrapper.KeyBindLink> keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        internal static Dictionary<string, MenuWrapper.SliderLink> sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();

        private static Spell Q, W, E, R, Q2;

        private static float NextAATime = 0f;
        private static bool ShouldHavePassive;
        private static float LastPassiveCheck = 0f;
        private static bool justCastedPassive = false;

        private static Vector3 REndPosition;
        private static Vector3 RStartPos;
        private static bool isUsingR;

        public KawaiiLucian()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        void Game_OnGameLoad(EventArgs args)
        {
           
            Player = ObjectManager.Player;
            if (Player.ChampionName != champName) return;

            CreateMenu();

            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 1000);

            Q.SetSkillshot(0.25f, 65f, 1100f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 475);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat("KawaiiLucian by AsunaChan/DZ191 Loaded");
        }

        void Drawing_OnDraw(EventArgs args)
        {
            if (getTargetForR() != null)
            {
                var V3R = getV3ForR(getTargetForR());
                Utility.DrawCircle(V3R, 100f, System.Drawing.Color.OrangeRed);
            }
        }
        
        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if(!boolLinks["GapClosers"].Value)return;

            var startPosition = gapcloser.Start;
            var endPosition = gapcloser.End;
            var LineVector = Vector3.Normalize(endPosition - startPosition);
            var PositionToE = LineVector*(-E.Range);
            if (isSafeToE(PositionToE) && E.IsReady())
            {
                E.Cast(PositionToE);
            }
        }

        void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            
            if (!hasPassive())
            {
                justCastedPassive = true;
            }
            switch (Menu.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (boolLinks["UseEC"].Value && E.IsReady() && justCastedPassive && canUseSkill("E") && isSafeToE(Game.CursorPos)) { E.Cast(Game.CursorPos); }
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (boolLinks["UseEM"].Value && E.IsReady() && justCastedPassive && canUseSkill("E") && isSafeToE(Game.CursorPos)) { E.Cast(Game.CursorPos); }
                    break;
            }
            

        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            
            switch (args.SData.Name)
            {
                case "LucianPassiveAttack":
                    ShouldHavePassive = true;
                    justCastedPassive = true;
                    break;
                case "LucianQ":
                    ShouldHavePassive = true;
                    break;
                case "LucianW":
                    ShouldHavePassive = true;
                    break;
                case "LucianE":
                    ShouldHavePassive = true;
                    break;
                case "LucianR":
                    REndPosition = args.End;
                    RStartPos = args.Start;
                    isUsingR = true;
                    break;
            }
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            passiveCheck();
            autoQ();
            autoExtQ();
            RCheck();
            RLock();      

            switch (Menu.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    doThings("C");
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    doThings("H");
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Farm(true);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
                default:
                    break;
            }
        }

        bool isKillableAAOnly(Obj_AI_Hero target)
        {
            return Player.GetAutoAttackDamage(target) >= target.Health + 5;
        }
        void doThings(String Mode)
        {
            if (ShouldHavePassive || hasPassive()) return;
            var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var QExttarget = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
            var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (boolLinks["UseQ" + Mode].Value && Q.IsReady() && canUseSkill("Q") && !isKillableAAOnly(Qtarget) && !ShouldHavePassive && Player.Distance(Qtarget)<=Q.Range) { Q.CastOnUnit(Qtarget); }
            if (boolLinks["UseEQ" + Mode].Value && Q.IsReady() && canUseSkill("Q")  &&!ShouldHavePassive && Player.Distance(QExttarget) > Q.Range && Player.Distance(QExttarget)<=Q2.Range) { CastExtendedQUnit(Qtarget); }
            if (boolLinks["UseW" + Mode].Value && W.IsReady() && canUseSkill("W")  && !ShouldHavePassive) { W.Cast(Wtarget.Position); }
        }

        bool isSafeToE(Vector3 Position)
        {
            var EnemiesList = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && !hero.IsDead && hero.Distance(Position) <= 550f).ToList();
            var AllyList = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsDead && hero.Distance(Position) <= 550f).ToList();
            var ClosestTowerToPosition =
                ObjectManager.Get<Obj_AI_Turret>().First(turret => turret.IsEnemy && turret.Distance(Position) <= 975f);
            if (ClosestTowerToPosition.IsValid) return false;
            if (EnemiesList.Count > 2 && AllyList.Count < 3) return false;
            return true;
        }
        bool canUseSkill(String Skill)
        {
            switch (Menu.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    return getManaPercentage() >= sliderLinks[Skill+"Mana"+"C"].Value.Value;
                case Orbwalking.OrbwalkingMode.Mixed:
                    return getManaPercentage() >= sliderLinks[Skill + "Mana" + "H"].Value.Value;
                case Orbwalking.OrbwalkingMode.LastHit:
                    return getManaPercentage() >= sliderLinks[Skill + "Mana" + "LH"].Value.Value;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    return getManaPercentage() >= sliderLinks[Skill + "Mana" + "LC"].Value.Value;
            }
            return false;
        }

        void RCheck()
        {
            if (!R.IsReady() && R.Level > 0)
            {
                isUsingR = false;
                REndPosition = Vector3.Zero;
                RStartPos = Vector3.Zero;
            }
        }

        void RLock()
        {
            if (isUsingR)
            {
                var Target = getTargetForR();
                if (Target != null && boolLinks["RLock"].Value)
                {
                    Vector3 PosForR = getV3ForR(Target);
                    if (isSafeToE(PosForR))
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, PosForR);
                        Menu.Orbwalker.SetOrbwalkingPoint(PosForR);
                    }
                }
                else
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
            }
        }

        void autoQ()
        {
            if (boolLinks["AutoQ"].Value)
            {
                var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (getManaPercentage() >= sliderLinks["QManaAuto"].Value.Value && Target.IsValidTarget())
                {
                    Q.CastOnUnit(Target, UsingPackets());
                   // Utility.DelayAction.Add(25, Orbwalking.ResetAutoAttackTimer);
                 //   Utility.DelayAction.Add(50, () => Player.IssueOrder(GameObjectOrder.AttackTo, Target.ServerPosition));
                }
            }
        }

        void autoExtQ()
        {
            if (boolLinks["AutoEQ"].Value)
            {
                var Target = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
                if (getManaPercentage() >= sliderLinks["QManaAuto"].Value.Value && Target.IsValidTarget())
                {
                   CastExtendedQUnit(Target);
                }
            }
        }

        void CastExtendedQUnit(Obj_AI_Base target)
        {
            if (!Q.IsReady()) return;
            //Credits to Mister xSalice ^^
            var QPrediction = Q2.GetPrediction(target, true);
            var QCollision = QPrediction.CollisionObjects;
            if (QCollision.Count > 0)
            {
                Q.CastOnUnit(QCollision[0], UsingPackets());
            }   
        }

        void Farm(bool LastHit = false)
        {
            var QMinions = MinionManager.GetMinions(Player.Position, Q.Range);
            var WMinions = MinionManager.GetMinions(Player.Position, W.Range);
            var QMinionsLH = MinionManager.GetMinions(Player.Position, Q.Range).Where(min => min.Health+10 <= Q.GetDamage(min));
            var WMinionsLH = MinionManager.GetMinions(Player.Position, W.Range).Where(min => min.Health+10 <= W.GetDamage(min));
            var ToGetQ = LastHit ? QMinionsLH.ToList() : QMinions.ToList();
            var ToGetW = LastHit ? WMinionsLH.ToList() : WMinions.ToList();
            var FarmLocation = Q.GetLineFarmLocation(ToGetQ);
            var WFarmLocation = W.GetCircularFarmLocation(ToGetW);
            var Location = FarmLocation.Position;
            var MinionNear = MinionManager.GetMinions(Location.Extend(Player.ServerPosition.To2D(),Q.Range).To3D(), 65f).First();
            
            if (MinionNear.IsValidTarget(Q.Range) && ToGetQ.Count > 0)
            {
                if (canUseSkill("Q") && Q.IsReady())
                {
                    Q.Cast(MinionNear, UsingPackets());
                }
            }
            if (canUseSkill("W") && ToGetW.Count > 0 && W.IsReady())
            {
                W.Cast(WFarmLocation.Position,UsingPackets());
            }
        }

        Obj_AI_Hero getTargetForR()
        {
            var finalPosition = getEndPosition();
            var checks = Player.ServerPosition.Distance(finalPosition)/150;
            Obj_AI_Hero currentTarget = null;
            for (int i = 0; i < checks; i++)
            {
                var Position = Player.ServerPosition.To2D().Extend(finalPosition.To2D(), 100);
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy && !h.IsDead && h.Distance(Position)<=450))
                {
                    if (hero.Health < currentTarget.Health)
                    {
                        currentTarget = hero;
                    }
                }
            }
            return currentTarget;
        }
        Vector3 getEndPosition()
        {
            var EndPositionStart = REndPosition;
            var FirstDifference = EndPositionStart - RStartPos;
            var Perp = FirstDifference.To2D().Perpendicular();
            var EndPosCurrent = Player.ServerPosition.To2D().Extend(Perp, 650);
            return EndPosCurrent.To3D();
        }
        //Probably not right lol
        Vector3 getV3ForR(Obj_AI_Hero target)
        {
            var RToMeVector3 = getEndPosition() - Player.ServerPosition;
            var PerpendicularV2 = RToMeVector3.To2D().Perpendicular();
            var TargetToMeV3 = target.ServerPosition - Player.ServerPosition;
            var AngleBetween = Geometry.DegreeToRadian(Geometry.AngleBetween(TargetToMeV3.To2D(),PerpendicularV2));
            //var DistanceTargetMe = Player.ServerPosition.Distance(target.ServerPosition);
            //var unitsToWalk = DistanceTargetMe * (float)Math.Cos(AngleBetween);
            var FinalPositon = TargetToMeV3*(float) Math.Cos(AngleBetween);
            return FinalPositon;
        }

        bool UsingPackets()
        {
            return boolLinks["Packets"].Value;
        }
        void passiveCheck()
        {
            if (Environment.TickCount - LastPassiveCheck < 50f) return;
            ShouldHavePassive = hasPassive();
            LastPassiveCheck = Environment.TickCount;
        }
        bool hasPassive()
        {
            return Player.HasBuff("lucianpassivebuff",true);
        }
        float getManaPercentage()
        {
            return (Player.Mana / Player.MaxMana) * 100;
        }
        void CreateMenu()
        {
            Menu = new MenuWrapper("KawaiiLucian");

            var comboMenu = Menu.MainMenu.AddSubMenu("[KL] Combo");

            boolLinks.Add("UseQC", comboMenu.AddLinkedBool("Use Q Combo"));
            boolLinks.Add("UseEQC", comboMenu.AddLinkedBool("Use Extended Q Combo"));
            boolLinks.Add("UseWC", comboMenu.AddLinkedBool("Use W Combo"));
            boolLinks.Add("UseEC", comboMenu.AddLinkedBool("Use E Combo"));
            boolLinks.Add("UseRC", comboMenu.AddLinkedBool("Use R Combo"));
            sliderLinks.Add("QManaC", comboMenu.AddLinkedSlider("Q Mana", 35));
            sliderLinks.Add("WManaC", comboMenu.AddLinkedSlider("W Mana", 30));
            sliderLinks.Add("EManaC", comboMenu.AddLinkedSlider("E Mana", 5));
            sliderLinks.Add("RManaC", comboMenu.AddLinkedSlider("R Mana", 25));

            var HarassMenu = Menu.MainMenu.AddSubMenu("[KL] Harass");

            boolLinks.Add("UseQH", HarassMenu.AddLinkedBool("Use Q Harass"));
            boolLinks.Add("UseEQH", HarassMenu.AddLinkedBool("Use Extended Q Harass"));
            boolLinks.Add("UseWH", HarassMenu.AddLinkedBool("Use W Harass"));
            boolLinks.Add("UseEH", HarassMenu.AddLinkedBool("Use E Harass"));
            sliderLinks.Add("QManaH", HarassMenu.AddLinkedSlider("Q Mana", 35));
            sliderLinks.Add("WManaH", HarassMenu.AddLinkedSlider("W Mana", 30));
            sliderLinks.Add("EManaH", HarassMenu.AddLinkedSlider("E Mana", 5));
            sliderLinks.Add("RManaH", HarassMenu.AddLinkedSlider("R Mana", 25));

            var FarmMenu = Menu.MainMenu.AddSubMenu("[KL] Farm");

            boolLinks.Add("UseQLH", FarmMenu.AddLinkedBool("Use Q LastHit"));
            boolLinks.Add("UseQLC", FarmMenu.AddLinkedBool("Use Q Laneclear"));
            boolLinks.Add("UseWLH", FarmMenu.AddLinkedBool("Use W LastHit"));
            boolLinks.Add("UseWLC", FarmMenu.AddLinkedBool("Use W Laneclear"));
            sliderLinks.Add("QManaLH", FarmMenu.AddLinkedSlider("Q Mana Lasthit", 35));
            sliderLinks.Add("QManaLC", FarmMenu.AddLinkedSlider("Q Mana Laneclear", 35));
            sliderLinks.Add("WManaLH", FarmMenu.AddLinkedSlider("W Mana Lasthit", 35));
            sliderLinks.Add("WManaLC", FarmMenu.AddLinkedSlider("W Mana Laneclear", 35));

            var MiscMenu = Menu.MainMenu.AddSubMenu("[KL] Misc");

            boolLinks.Add("Packets", MiscMenu.AddLinkedBool("Use Packets"));
            boolLinks.Add("GapClosers", MiscMenu.AddLinkedBool("E Gapclosers"));
            boolLinks.Add("AutoQ", MiscMenu.AddLinkedBool("Auto Q"));
            boolLinks.Add("AutoEQ", MiscMenu.AddLinkedBool("Auto Extended Q"));
            sliderLinks.Add("QManaAuto", MiscMenu.AddLinkedSlider("Auto Q Mana", 35));
            boolLinks.Add("RLock", MiscMenu.AddLinkedBool("R Lock"));
            //TODO Add Draw Menu
        }
    }
}
