using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

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
        }

        void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (!unit.IsMe) return;
            if (!hasPassive())
            {
                justCastedPassive = true;
            }
            switch (Menu.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (boolLinks["UseEC"].Value && E.IsReady() && justCastedPassive && canUseSkill("E")) { E.Cast(Game.CursorPos); }
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (boolLinks["UseEM"].Value && E.IsReady() && justCastedPassive && canUseSkill("E")) { E.Cast(Game.CursorPos); }
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
            }
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            passiveCheck();
            if (!target.IsValidTarget()) return;
            switch (Menu.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    doThings("C",target);
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    doThings("H", target);
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
                default:
                    break;
            }
        }

        void doThings(String Mode, Obj_AI_Hero target)
        {
            if (ShouldHavePassive) return;
            if (boolLinks["UseW"+Mode].Value && W.IsReady() && canUseSkill("W")) { W.Cast(target.Position); }
            if (boolLinks["UseQ"+Mode].Value && Q.IsReady() && canUseSkill("Q")) { Q.CastOnUnit(target);}
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

        float getManaPercentage()
        {
            return (Player.Mana/Player.MaxMana)*100;
        }
        void passiveCheck()
        {
            if (Environment.TickCount - LastPassiveCheck < 100f) return;
            ShouldHavePassive = hasPassive();
            LastPassiveCheck = Environment.TickCount;
        }
        bool hasPassive()
        {
            return Player.HasBuff("lucianpassivebuff",true);
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
            
            //TODO Add Draw Menu
        }
    }
}
