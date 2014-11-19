using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace DZRumble
{
    class Program
    {
        private static String champName = "Rumble";
        private static Menu Menu;
        private static Spell Q, W, E, R;
        private static Orbwalking.Orbwalker commonOrbwalker;
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static HitChance customHitChance = HitChance.Medium;
        private static bool overheat, HQE,HW;
        private static float lastECast;
        private static Vector3 Debug1, Debug2;
        private static Dictionary<Obj_AI_Hero, int> CachedOrient = new Dictionary<Obj_AI_Hero, int>(); 
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != champName) return;
            Menu = new Menu("DangerZone Rumble","DZRumble1",true);
            var lxMenu = new Menu("Orbwalker", "LXOrb");
            commonOrbwalker = new Orbwalking.Orbwalker(lxMenu);
            Menu.AddSubMenu(lxMenu);
            var ts = new Menu("Target Selector", "TargetSelector");
            SimpleTs.AddToMenu(ts);
            Menu.AddSubMenu(ts);

            Menu.AddSubMenu(new Menu("[Rumble]Combo", "Combo"));

            Menu.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseEC", "Use E Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseRC", "Use R Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("AllowOHC", "Allow Smart OH").SetValue(false));

            Menu.AddSubMenu(new Menu("[Rumble]Harrass", "Harrass"));
            Menu.SubMenu("Harrass").AddItem(new MenuItem("UseQH", "Use Q Harrass").SetValue(true));
            Menu.SubMenu("Harrass").AddItem(new MenuItem("UseWH", "Use W Harrass").SetValue(true));
            Menu.SubMenu("Harrass").AddItem(new MenuItem("UseEH", "Use E Harrass").SetValue(true));
            Menu.SubMenu("Harrass").AddItem(new MenuItem("AllowOHH", "Allow Smart OH").SetValue(false));

            Menu.AddSubMenu(new Menu("[Rumble]Farm", "Farm"));
            Menu.SubMenu("Farm").AddItem(new MenuItem("UseQLH", "Use Q LastHit").SetValue(false));
            Menu.SubMenu("Farm").AddItem(new MenuItem("UseELH", "Use E LastHit").SetValue(true));
            Menu.SubMenu("Farm").AddItem(new MenuItem("UseQLC", "Use Q Laneclear").SetValue(true));
            Menu.SubMenu("Farm").AddItem(new MenuItem("UseELC", "Use E Laneclear").SetValue(false));

            Menu.AddSubMenu(new Menu("[Rumble]Misc", "Misc"));
            Menu.SubMenu("Misc").AddItem(new MenuItem("Debug", "Debug[For Dev]").SetValue(true));
            Menu.SubMenu("Misc").AddItem(new MenuItem("AutoR", "Auto R at").SetValue(new Slider(3,1,5)));
            Menu.SubMenu("Misc").AddItem(new MenuItem("RDuration", "R Min Duration").SetValue(new Slider(1, 0, 2)));
            Menu.SubMenu("Misc").AddItem(new MenuItem("AutoE", "Auto E Slowed/Immobile").SetValue(false));
            Menu.SubMenu("Misc").AddItem(new MenuItem("SecondE", "2nd E Delay Melee Range").SetValue(true));
            Menu.SubMenu("Misc").AddItem(new MenuItem("SecondED", "2nd E Delay Melee Range").SetValue(new Slider(3, 1, 5)));
            Menu.SubMenu("Misc").AddItem(new MenuItem("Flee", "Flee Key").SetValue(new KeyBind("S".ToCharArray()[0],KeyBindType.Press)));

            Menu.AddSubMenu(new Menu("[Rumble]Heat Manager", "Heat"));
            Menu.SubMenu("Heat").AddItem(new MenuItem("UseQHe", "Use Q").SetValue(true));
            Menu.SubMenu("Heat").AddItem(new MenuItem("UseWHe", "Use W").SetValue(true));
            Menu.SubMenu("Heat").AddItem(new MenuItem("UseEHe", "Use E").SetValue(true));
            Menu.SubMenu("Heat").AddItem(new MenuItem("DangerZone", "Stay In Danger Zone").SetValue(true));
            Menu.AddSubMenu(new Menu("[Rumble]Drawing", "Drawings"));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q").SetValue(new Circle(true,System.Drawing.Color.Red)));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E").SetValue(new Circle(true, System.Drawing.Color.Red)));
            Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R").SetValue(new Circle(true, System.Drawing.Color.Red)));

            Menu.AddToMainMenu();

            Q=new Spell(SpellSlot.Q,600f);
            W=new Spell(SpellSlot.W,0f);
            E = new Spell(SpellSlot.E, 850f);
            R = new Spell(SpellSlot.R, 1700f);

            E.SetSkillshot(0.25f,70,2000,true,SkillshotType.SkillshotLine);
            R.SetSkillshot(1700, 120, 1400, false, SkillshotType.SkillshotLine);
            Game.PrintChat("DangerZone Rumble By DZ191 Loaded. Special thanks to Dienofail.");
           // CachedOrientInit();
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (isValueEnabled("Debug"))
            {            
                Vector2 p1, p2;
                 var target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(hero => hero.IsEnemy)
                        .OrderBy(hero => hero.Distance(Player))
                        .First();
                Vector2 pos1Mid, pos2Mid, pos1Ext, pos2Ext;
                int nHitMid, nHitExt;
                MidPointSimulation(target, out pos1Mid, out pos2Mid, out nHitMid);
               // ExtremePointSimulation(target, out pos1Ext, out pos2Ext, out nHitExt);
                //IndipendentSimulation(out pos1Mid,out pos2Mid,out nHitMid);
                Utility.DrawCircle(pos1Mid.To3D(),100f,Color.Yellow);Utility.DrawCircle(pos2Mid.To3D(),100f,Color.Orange);
                Utility.DrawCircle(target.Direction,100f,Color.Tomato);
                //Utility.DrawCircle(pos1Ext.To3D(), 100f, Color.Green); Utility.DrawCircle(pos2Ext.To3D(), 100f, Color.Cyan);
                 
                // int nHit;
                // IndipendentSimulation(out p1, out p2, out nHit);
                //Game.PrintChat(p1.ToString());
               // Game.PrintChat(p2.ToString());
                // Utility.DrawCircle(p1.To3D(), 100f, Color.Yellow); Utility.DrawCircle(p2.To3D(), 100f, Color.Orange);
            }
           
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            Print();
            Flee(); 
            CachedOrientUpdate();
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var Rtarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);  
            switch(commonOrbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    HeatCalculation(target);
                    Combo(target, Rtarget);
                    break;
                default:
                    break;
            }
            AutoHeatManager();
        }

        private static void Flee()
        {
            if (!isKeybindEnabled("Flee")) return;
            var closestEnemy = getEnemiesInRange(E.Range).OrderBy(player => player.Distance(Player)).FirstOrDefault();
            sendMovementPacket(Game.CursorPos.To2D());
            if (closestEnemy != null)
            {
                var EPrediction = E.GetPrediction(closestEnemy);
                if (EPrediction.Hitchance >= customHitChance)
                {
                    E.Cast(EPrediction.CastPosition);
                }
            }
            W.Cast();
        }

        private static void Combo(Obj_AI_Hero target,Obj_AI_Hero RTarget)
        {
            if (!Utility.IsValidTarget(target)) return;
            var useEC = isValueEnabled("UseEC");
            
            var useRC = isValueEnabled("UseRC");
            
            var useWC = isValueEnabled("UseWC");
            
            var useQC = isValueEnabled("UseQC");
            var debug = isValueEnabled("Debug");
            var AllowOverHeat = isValueEnabled("AllowOHC");
            HeatCalculation(target);

            if (useQC && Q.IsReady() && HQE && target.Distance(Player)<650f && Utility.IsFacing(Player,target))
            {
                Q.Cast(target);
            }
            if (useEC && E.IsReady() && !hasSecondE() && HQE && target.Distance(Player)<=E.Range)
            {
                if (debug) { Console.WriteLine("Casted First E");}
                castE(target);
                
            }
            if (useEC && E.IsReady() && hasSecondE() && target.Distance(Player) <= E.Range)
            {
                if (Player.Distance(target) <= 350f && isValueEnabled("SecondE"))
                {
                    var SecondED = Menu.Item("SecondED").GetValue<Slider>().Value;
                    if (Game.Time - lastECast >= SecondED)
                    {
                        castE(target);
                    }
                }
                else
                {
                    castE(target);
                }
            }
            if (useWC && W.IsReady() && HW && Player.Distance(target)<850f)
            {
                W.Cast();
            }
            //TODO R Cast here
            if (useRC)
            {
                CastR(target);
            }
            if ( AllowOverHeat && overheat && !isOverHeated())
            {
                if(debug)Console.WriteLine("Should Overheat");
                Q.Cast(target);
                W.Cast();
                castE(target);
            }
        }
        #region Skill Casting Methods
        private void castQAdv()
        {
        }

        private static void castE(Obj_AI_Hero target)
        {
            if (!E.IsReady()) return;
            var EPrediction = E.GetPrediction(target);
            if (EPrediction.Hitchance >= customHitChance)
            {
                E.Cast(EPrediction.CastPosition);
                lastECast = Game.Time;
            }
        }

        private static void CastR(Obj_AI_Hero target)
        {
            if (!R.IsReady() || !target.IsValidTarget()) return;
            if (getEnemiesInRangeOfPos(target.Position, 1000).Count > 1)
            {
                
                Vector2 pos1Mid, pos2Mid,pos1Ext,pos2Ext;
                int nHitMid,nHitExt;
                MidPointSimulation(target,out pos1Mid,out pos2Mid,out nHitMid);
               // ExtremePointSimulation(target,out pos1Ext,out pos2Ext,out nHitExt);
                if (pos1Mid != null && pos2Mid != null)
                {
                    castExtR(pos1Mid.To3D(), pos2Mid.To3D());
                }
                //if (nHitMid >= nHitExt)
               // {
                   // Game.PrintChat("Should Cast");
                    
               // }
               // else
                //{
                 //   if (pos1Ext != null && pos2Ext != null)
                 //   {
                  //      castExtR(pos1Ext.To3D(), pos2Ext.To3D());
                  //  }
                }
                /**
                Vector2 p1, p2;
                int nHit;
                IndipendentSimulation(out p1,out p2,out nHit);
                if (p1 != Vector2.Zero && p2 != Vector2.Zero)
                {
                    castExtR(p1,p2);
                }
                 * 
                 * */
            }
            
            
        

        private static void castExtR(Vector3 point1, Vector3 point2)
        {
            var p1 = point1.To2D();
            var p2 = point2.To2D();

            Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, R.Slot, -1, p1.X, p1.Y, p2.X, p2.Y)).Send();
        }
        #endregion

        #region MyUltLogic  
        public static bool IsWall(Vector3 position)
        {
            CollisionFlags cFlags = NavMesh.GetCollisionFlags(position);
            return (cFlags == CollisionFlags.Wall || cFlags == CollisionFlags.Building);
        }

        private static void Print()
        {
        }
        private static void CachedOrientUpdate()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var orientation = hero.ServerPosition - hero.Position;
                double angleRadians = Math.Atan2(orientation.Y, orientation.X);
                int angle = (int)Math.Ceiling(angleRadians * 180 / Math.PI);
                angle = angle < 0 ? 360 - angle : angle;
                if (angle != 0) CachedOrient[hero] = angle;
            }
        }

        private static void CachedOrientInit()
        {
            foreach (
                var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                CachedOrient.Add(hero,0);
            }
        }

        private static void uberSimulation(out Vector2 Pos1, out Vector2 Pos2, out int Hit)
        {
            var list = ObjectManager.Get<Obj_AI_Hero>().OrderByDescending(h => h.Distance(Player)).ToList();
            var closest = list[0];
            var farthest = list[list.Count()];
           
        }
        private static void IndipendentSimulation(out Vector2 Pos1, out Vector2 Pos2, out int Hit)
        {
            var MaxHit = 0;
            Vector3 MP1 = Vector3.Zero;
            Vector3 MP2 = Vector3.Zero;
            foreach (var Hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.Distance(Player)<1700))
            {
                foreach (var Hero2 in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.Distance(Hero)<1000 && hero != Hero))
                {
                    var PInput = new PredictionInput
                    {
                        Delay = 0.25f,
                        Radius = 1,
                        Type = SkillshotType.SkillshotCircle,
                        Unit = Hero,

                    };
                    var PInput2 = new PredictionInput
                    {
                        Delay = 0.25f,
                        Radius = 1,
                        Type = SkillshotType.SkillshotCircle,
                        Unit = Hero2,

                    };
                    var Pred1 = Prediction.GetPrediction(PInput);
                    var Pred2 = Prediction.GetPrediction(PInput);
                    Vector3 p1 = Pred1.CastPosition;
                    Vector3 p2 = Pred2.CastPosition;
                    var Distance = p1.Distance(p2);
                    var DFrom1 = Player.Distance(p1);
                    var DFrom2 = Player.Distance(p2);
                    var pFinal = Vector2.Zero;
                    var pFInal1 = Vector2.Zero;
                        var V1 = Vector3.Normalize(p1 - p2);
                        var V2 = V1*(900 - Distance);
                        //if (DFrom1 > DFrom2)
                        //{
                            pFinal = p1.To2D().Extend(p2.To2D(), -600);
                            pFInal1 = p1.To2D();
                        //}
                        //else
                        //{
                        //    pFinal = p2.To2D().Extend(p1.To2D(), 900 - Distance);
                         //   pFInal1 = p2.To2D();
                       // }
                    if (!CheckWall(pFinal.To3D(), pFInal1.To3D()))
                    {
                        var NHit = CalculateNumHit(pFinal.To3D(), pFInal1.To3D(), Hero);
                        if (NHit > MaxHit)
                        {
                            MP1 = p1;
                            MP2 = p2;
                            MaxHit = NHit;
                        }
                    }
                }
            }
            Pos1 = MP1.To2D();
            Pos2 = MP2.To2D();
            Hit = MaxHit;
        }

        private static void ExtremePointSimulation(Obj_AI_Hero target, out Vector2 Pos1, out Vector2 pos2, out int Hit)
        {
            var targetPos = target.Position;
            var RLenght = 900;
            var MaxHit2 = 0;
            Vector2 MaxP1 = Vector2.Zero;
            Vector2 MaxP2 = Vector2.Zero;
            var orientation = target.ServerPosition - target.Position;
            double angleRadians = Math.Atan2(orientation.Y, orientation.X);
            int angle = (int)Math.Ceiling(angleRadians * 180 / Math.PI);
            //angle = angle < 0 ? 360 - angle : angle;
            
           // var angle = CachedOrient[target];
           // Game.PrintChat(angle.ToString());
            for (int i = angle; i <= angle + 360; i += 32)
            {
                var cosI = (float)Math.Cos(i * (Math.PI / 180));
                var sinI = (float)Math.Sin(i * (Math.PI / 180));

                Vector2 P1 = new Vector2();
                P1 = new Vector2(targetPos.X + RLenght * cosI, targetPos.Y + RLenght * sinI);
                
                if (i > angle + 90)
                {
                    P1 = new Vector2(targetPos.X + RLenght * cosI, targetPos.Y + RLenght * sinI);
                }
                else if (i > angle + 180)
                {
                    P1 = new Vector2(targetPos.X - RLenght * cosI, targetPos.Y - RLenght * sinI);
                }
                else if (i > angle + 270)
                {
                    P1 = new Vector2(targetPos.X + RLenght * cosI, targetPos.Y - RLenght * sinI);
                }

                if (!CheckWall(targetPos, P1.To3D()))
                {
                    var NHit = CalculateNumHit(targetPos, P1.To3D(), target);
                    if (NHit > MaxHit2)
                    {
                        MaxHit2 = NHit;
                        MaxP1 = P1;
                    }
                }
            }
            Hit = MaxHit2;
            Pos1 = targetPos.To2D();
            pos2 = MaxP1;
        }

        private static void MidPointSimulation(Obj_AI_Hero target, out Vector2 Pos1, out Vector2 pos2, out int Hit)
        {
            var targetPos = target.Position;
            var RLenght = 900;
            var HalfR = RLenght / 2;
            var MaxHit2 = 0;
            Vector2 MaxP1 = Vector2.Zero;
            Vector2 MaxP2 = Vector2.Zero;
            //var angle = CachedOrient[target];
            var orientation = target.ServerPosition - target.Position;
            double angleRadians = Math.Atan2(orientation.Y, orientation.X);
            int angle = (int)Math.Ceiling(angleRadians * 180 / Math.PI);
            //angle = angle < 0 ? 360 - angle : angle;
            
           // Game.PrintChat(target.ChampionName+ " Angle: "+angle.ToString());
            for (int i = angle; i <= angle + 180; i += 31)
            {
                var cosI = (float)Math.Cos(i * (Math.PI / 180));
                var sinI = (float)Math.Sin(i * (Math.PI / 180));

                Vector2 P1, P2 = new Vector2();
                //P1 = new Vector2(targetPos.X - HalfR * cosI, targetPos.Y - HalfR * sinI);
                //P2 = new Vector2(targetPos.X + HalfR * cosI, targetPos.Y + HalfR * sinI);
                
                if (i > angle + 90)
                {
                    P1 = new Vector2(targetPos.X + HalfR * cosI, targetPos.Y - HalfR * sinI);
                    P2 = new Vector2(targetPos.X - HalfR * cosI, targetPos.Y + HalfR * sinI);
                }
                else
                {
                    P1 = new Vector2(targetPos.X - HalfR * cosI, targetPos.Y - HalfR * sinI);
                    P2 = new Vector2(targetPos.X + HalfR * cosI, targetPos.Y + HalfR * sinI);
                }
                 
                if (!CheckWall(P1.To3D(), P2.To3D()))
                {
                    var NHit = CalculateNumHit(P1.To3D(), P2.To3D(), target);
                    if (NHit > MaxHit2)
                    {
                        MaxHit2 = NHit;
                        MaxP1 = P1;
                        MaxP2 = P2;
                    }
                }
            }
            Hit = MaxHit2;
            Pos1 = MaxP1;
            pos2 = MaxP2;

        }
        #endregion


        #region Ultimate Utility Method
      
        private static Vector3 GetMidPoint(Vector3 p1, Vector3 p2)
        {
            return new Vector3((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);
        }

        private static bool CheckWall(Vector3 Position1, Vector3 Position2)
        {
            var Posmidpoint = new Vector3((Position1.X + Position1.X) / 2, (Position1.Y + Position2.Y) / 2, (Position1.Z + Position2.Z) / 2);
            var EndInitialVector = Vector3.Normalize(Position2 - Position1);
            var Multiplier = 30;
            var WallCount = 0;
            for (var i = 1; i <= 20; i++)
            {
                var current_multiplier = 60 * i;
                var CurrentCheckVector = Position1 + EndInitialVector * current_multiplier;
                if (IsWall(CurrentCheckVector))
                {
                    WallCount++;
                }
            }
            return WallCount >= 8;
        }

        private static int CalculateNumHit(Vector3 Position1, Vector3 Position2, Obj_AI_Hero target)
        {
            var Posmidpoint = new Vector3((Position1.X + Position2.X) / 2, (Position1.Y + Position2.Y) / 2,0);
            var EndInitialVector = Vector3.Normalize(Position2 - Position1);
            var ExtensionAmount = 450;
            var ExtPos1 = Posmidpoint + EndInitialVector * ExtensionAmount;
            var ExtPos2 = Posmidpoint - EndInitialVector * ExtensionAmount;
            var Enemies = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy);
            var NumHit = 0;
            var ObjHero = new List<Obj_AI_Hero>();
            foreach (var enemy in Enemies)
            {
                if (enemy.IsValid && !enemy.IsDead && enemy.Distance(target) < 1000)
                {
                    var PInput = new PredictionInput
                    {
                        Delay = 0.25f,
                        Radius = 1,
                        Type = SkillshotType.SkillshotCircle,
                        Unit = enemy,

                    };
                    var EnemyPredictedPos = Prediction.GetPrediction(PInput).CastPosition;
                    var Objects = VectorPointProjectionOnLineSegment(ExtPos1.To2D(), ExtPos2.To2D(), EnemyPredictedPos.To2D());
                    if ((bool)Objects[2] && Vector2.Distance((Vector2)Objects[0],(Vector2)Objects[1])< 90+60)
                    {
                        ObjHero.Add(enemy);
                        NumHit++;
                    }
                }
            }
            //Game.PrintChat(NumHit.ToString());
           // Game.PrintChat("From "+target.ChampionName+" Will Hit: ");
           // Game.PrintChat("===========================");
          //  foreach (var h in ObjHero)
         //   {
          //      Game.PrintChat("WillHit: "+h.ChampionName);
          //  }
           // Game.PrintChat("===========================");
            return NumHit;
        }
        
        public static Object[] VectorPointProjectionOnLineSegment(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float cx = v3.X;
            float cy = v3.Y;
            float ax = v1.X;
            float ay = v1.Y;
            float bx = v2.X;
            float by = v2.Y;
            float rL = ((cx - ax) * (bx - ax) + (cy - ay) * (by - ay)) /
                       ((float)Math.Pow(bx - ax, 2) + (float)Math.Pow(by - ay, 2));
            var pointLine = new Vector2(ax + rL * (bx - ax), ay + rL * (by - ay));
            float rS;
            if (rL < 0)
            {
                rS = 0;
            }
            else if (rL > 1)
            {
                rS = 1;
            }
            else
            {
                rS = rL;
            }
            bool isOnSegment;
            if (rS.CompareTo(rL) == 0)
            {
                isOnSegment = true;
            }
            else
            {
                isOnSegment = false;
            }
            var pointSegment = new Vector2();
            if (isOnSegment)
            {
                pointSegment = pointLine;
            }
            else
            {
                pointSegment = new Vector2(ax + rS * (bx - ax), ay + rS * (by - ay));
            }
            return new object[3] { pointSegment, pointLine, isOnSegment };
        }
        #endregion


        #region Heat utility Methods
        private static float getHeat()
        {
            return Player.Mana;
        }
        private static bool isOverHeated()
        {
            return Player.HasBuff("rumbleoverheat", true);
        }
        //Method inspired by Dieno's rumble.Credits to DienoFail.
        private static void HeatCalculation(Obj_AI_Hero target)
        {
           // if (!Utility.IsValidTarget(target)) return;
            var Heat = getHeat();
            var oH = CalculateHeatTarget(target);
            if (Heat >= 80 && oH)
            {
                overheat = true;
                HQE = false;
                HW = false;
            }
            else if (Heat >= 80 && !oH)
            {
                overheat = false;
                HQE = false;
                HW = false;
            }
            else if (Heat >= 60)
            {
                overheat = false;
                HQE = true;
                HW = false;
            }
            else if (Heat >= 0)
            {
                overheat = false;
                HQE = true;
                HW = true;      
            }
        }
        private static void AutoHeatManager()
        {
            if (isRecalling() || Utility.InFountain()) return;
            if (isValueEnabled("DangerZone") && getEnemiesInRange(1000).Count <2)
            {
                if (isValueEnabled("UseWHe") && W.IsReady() && getHeat()<35)
                {
                    W.Cast();
                }else if(Q.IsReady() && isValueEnabled("UseQHe") &&getHeat()<35)
                {
                    Q.Cast(ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(hero => hero.IsEnemy));
                }
            }
        }
        private static bool CalculateHeatTarget(Obj_AI_Hero target)
        {
            if (target.IsValidTarget() && !Q.IsReady() && !E.IsReady() && !Q.IsReady(3) && !E.IsReady(4) && Player.Distance(target)<=350f)
            {
                var damage = Player.GetAutoAttackDamage(target);
                if (damage * 3 >= target.Health) return true;
            }
            return false;
        }
        #endregion


        #region Various Utility Methods

        private static bool isRecalling()
        {
            return Player.HasBuff("Recall", true);
        }
        private static bool hasSecondE()
        {
            return Player.HasBuff("RumbleGrenade", true);
        }
        private static bool isValueEnabled(String value)
        {
            return Menu.Item(value).GetValue<bool>();
        }
        private static bool isKeybindEnabled(String value)
        {
            return Menu.Item(value).GetValue<KeyBind>().Active;
        }
        private static List<Obj_AI_Hero> getEnemiesInRangeOfPos(Vector3 Pos,float range)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Where(player => player.IsValidTarget(range) && player.Distance(Pos) < range).ToList();
        }
        private static List<Obj_AI_Hero> getEnemiesInRange(float range)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Where(player => player.IsValidTarget(range) && player.Distance(Player.Position)<range).ToList();
        }
        private static void sendMovementPacket(Vector2 position)
        {
            Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(position.X, position.Y)).Send();
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, position.To3D());
        }
        #endregion
    }
}
