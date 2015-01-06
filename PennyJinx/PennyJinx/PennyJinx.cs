using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Network.Packets;
using SharpDX;

namespace PennyJinx
{
    internal class PennyJinx
    {
        private const String ChampName = "Jinx";

        private static HitChance CustomHitChance
        {
            get { return getHitchance(); }
        }

        public static Obj_AI_Hero Player;
        public static Spell _q, _w, _e, _r;
        public static Menu Menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly StringList QMode = new StringList(new[] {"AOE mode", "Range mode", "Both"}, 2);
        public static Render.Sprite Sprite;
        public static PennyJinx instance;
        public static List<SpriteManager.ScopeSprite> _KillableHeroes = new List<SpriteManager.ScopeSprite>(); 
        public PennyJinx()
        {
            instance = this;
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
            Game.PrintChat("<font color='#7A6EFF'>PennyJinx</font> v 1.0.1.2 <font color='#FFFFFF'>Loaded!</font>");

            
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            GameObject.OnCreate += Cleanser.OnCreateObj;
            GameObject.OnDelete += Cleanser.OnDeleteObj;
            new SpriteManager.ScopeSprite();
        }

        void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var Sender = (Obj_AI_Hero)gapcloser.Sender;
            if (!Sender.IsValidTarget() || !IsMenuEnabled("AntiGP") || !_e.IsReady()) return;
            _e.Cast(gapcloser.End, Packets());
        }

        void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var Sender = (Obj_AI_Hero)unit;
            if(!Sender.IsValidTarget() || !IsMenuEnabled("Interrupter") || !_e.IsReady())return;
            _e.CastIfHitchanceEquals(Sender, CustomHitChance, Packets());

        }

        void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var target = args.Target;
            if (!target.IsValidTarget())
                return;
            if (!(target is Obj_AI_Minion) || (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear && _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit))
                return;
            var t2 = target as Obj_AI_Minion;
            QSwitchLC(t2);
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            if (!(target is Obj_AI_Hero)) return;
            var tar = (Obj_AI_Hero)target;
            UseItems(tar);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Cleanser.cleanserBySpell();
            Cleanser.cleanserByBuffType();

            Auto();
            AutoPot();

            if (Menu.Item("ThreshLantern").GetValue<KeyBind>().Active) takeLantern();
            if (Menu.Item("ManualR").GetValue<KeyBind>().Active){RCast();}
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    ComboLogic();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    HarrassLogic();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    WUsageFarm();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    WUsageFarm();
                    break;
            }
        }

        


        #region Various
        private void AutoPot()
        {
            if (ObjectManager.Player.HasBuff("Recall") || Utility.InFountain() && Utility.InShopRange())
                return;

            //Health Pots
            if (IsMenuEnabled("APH") && GetPerValue(false) <= Menu.Item("APH_Slider").GetValue<Slider>().Value && !Player.HasBuff("RegenerationPotion", true))
            {
                UseItem(2003);
            }
            //Mana Pots
            if (IsMenuEnabled("APM") && GetPerValue(true) <= Menu.Item("APM_Slider").GetValue<Slider>().Value && !Player.HasBuff("FlaskOfCrystalWater", true))
            {
                UseItem(2004);
            }
            //Summoner Heal
            if (IsMenuEnabled("APHeal") && GetPerValue(false) <= Menu.Item("APHeal_Slider").GetValue<Slider>().Value)
            {
                var heal = Player.GetSpellSlot("summonerheal");
                if (heal != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(heal) == SpellState.Ready)
                {
                    Player.Spellbook.CastSpell(heal);
                }
            }
        }

        void takeLantern()
        {
            foreach (GameObject obj in ObjectManager.Get<GameObject>())
            {
                if (obj.Name.Contains("ThreshLantern") && obj.Position.Distance(ObjectManager.Player.ServerPosition) <= 500 && obj.IsAlly)
                {
                    var InteractPKT = new PKT_InteractReq
                    {
                        NetworkId = Player.NetworkId,
                        TargetNetworkId = obj.NetworkId
                    };
                    //Credits to Trees
                    Game.SendPacket(InteractPKT.Encode(), PacketChannel.C2S, PacketProtocolFlags.Reliable);
                    return;
                }
            }

        }

        private void SwitchLc()
        {
            if (!_q.IsReady())
                return;

            if (IsFishBone())
            {
                _q.Cast();
            }
        }

        private void SwitchNoEn()
        {
            if (!IsMenuEnabled("SwitchQNoEn"))
                return;
            var Range = IsFishBone() ? Orbwalking.GetRealAutoAttackRange(null) : Player.AttackRange + GetFishboneRange();
            if (Player.CountEnemysInRange((int)Range) == 0)
            {
                if (IsFishBone())
                    _q.Cast();
            }
        }

        
        #endregion

        #region Drawing

        private static void Drawing_OnDraw(EventArgs args)
        {
                var DrawQ = Menu.Item("DrawQ").GetValue<Circle>();
                var DrawW = Menu.Item("DrawW").GetValue<Circle>();
                var DrawE = Menu.Item("DrawE").GetValue<Circle>();
                var DrawR = Menu.Item("DrawR").GetValue<Circle>();
                var QRange = IsFishBone() ? 525f + ObjectManager.Player.BoundingRadius + 65f :525f + ObjectManager.Player.BoundingRadius + 65f + GetFishboneRange() + 20f;
                if (DrawQ.Active) { Utility.DrawCircle(Player.Position,QRange,DrawQ.Color);}
                if (DrawW.Active) { Utility.DrawCircle(Player.Position, _w.Range, DrawW.Color); }
                if (DrawE.Active) { Utility.DrawCircle(Player.Position, _e.Range, DrawE.Color); }
                if (DrawR.Active) { Utility.DrawCircle(Player.Position, _r.Range, DrawR.Color); }
        }

        #endregion

        #region Combo/Harrass/Auto

        private void Auto()
        {
            SwitchNoEn();
            AutoWHarass();
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

        #region Farm
        void QSwitchLC(Obj_AI_Minion t2)
        {
            if (!IsMenuEnabled("UseQLC") ||!_q.IsReady() || GetPerValue(true) < GetSliderValue("QManaLC"))
                return;

            if (CountEnemyMinions(t2, 100) < GetSliderValue("MinQMinions"))
            {
                SwitchLc();
            }
            else
            {
                if (!IsFishBone() && GetPerValue(true) >= GetSliderValue("QManaLC"))
                {
                    _q.Cast();
                }
            }
        }

        void WUsageFarm()
        {
            var mode = _orbwalker.ActiveMode;
            var WMana = mode == Orbwalking.OrbwalkingMode.LaneClear
                ? GetSliderValue("WManaLC")
                : GetSliderValue("WManaLH");
            var WEnabled = mode == Orbwalking.OrbwalkingMode.LaneClear
                ? IsMenuEnabled("UseWLC")
                : IsMenuEnabled("UseWLH");
            var MList = MinionManager.GetMinions(Player.Position, _w.Range);
            var Location = _w.GetLineFarmLocation(MList);
            if (GetPerValue(true) >= WMana && WEnabled)
            {
                _w.Cast(Location.Position);
            }
        }
        #endregion

        #region Spell Casting

        private void QManager(String Mode)
        {
            if (!_q.IsReady())
            {
                return;
            }

            var aaRange = GetMinigunRange(null) + GetFishboneRange() +25f;
            var target = TargetSelector.GetTarget(aaRange, TargetSelector.DamageType.Physical);
            var JinxBaseRange = GetMinigunRange(target);

            if (!target.IsValidTarget(aaRange + GetFishboneRange() + 25f))
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
                        if (Player.Distance(target) < JinxBaseRange || GetPerValue(true) <= GetSliderValue("QMana" + Mode))
                        {
                            _q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > JinxBaseRange && GetPerValue(true) >= GetSliderValue("QMana" + Mode))
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
                        if (Player.Distance(target) < JinxBaseRange|| GetPerValue(true) <= GetSliderValue("QMana" + Mode))
                        {
                            _q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > JinxBaseRange && GetPerValue(true) >= GetSliderValue("QMana" + Mode) ||
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
            //Credits to Marksman
            //http://github.com/Esk0r/Leaguesharp/

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(_e.Range - 150)))
            {
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
            //If is killable with W and AA
            //Or the ally players in there are > 0
            if (isKillableWAA(rTarget) ||
                CountAllyPlayers(rTarget,500) > 0 || Player.Distance(rTarget)<(_w.Range/2))
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
            if (!wTarget.IsValidTarget())
                return;
            if (GetPerValue(true) >= autoWMana || isKillableWAA(wTarget))
            {
                _w.CastIfHitchanceEquals(wTarget, CustomHitChance, Packets());
                
            }
        }

        private void AutoWEmpaired()
        {
            if (!IsMenuEnabled("AutoWEmp"))
            {
                return;
            }

            //Uses W on whoever is empaired
            foreach (
                var enemy in
                    from enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(_w.Range))
                    let autoWMana = GetSliderValue("AutoWEmp_Mana")
                    where GetPerValue(true) >= autoWMana
                    select enemy)
            {
                if (IsEmpaired(enemy) || IsEmpairedLight(enemy))
                {
                    _w.CastIfHitchanceEquals(enemy, CustomHitChance, Packets());
                }
            }
        }

        #endregion

        #region Utility
        public static void UseItem(int id, Obj_AI_Hero target = null)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, target);
            }
        }

        private bool Packets()
        {
            return IsMenuEnabled("Packets");
        }

        private static float GetFishboneRange()
        {
            return 50 + 25*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static float GetMinigunRange(GameObject target)
        {
            return 525f + ObjectManager.Player.BoundingRadius + (target != null ? target.BoundingRadius : 0);
        }

        private static HitChance getHitchance()
        {
            switch (Menu.Item("C_Hit").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.Medium;

            }
        }
        private static bool isKillableWAA(Obj_AI_Hero wTarget)
        {
            if (Player.Distance(wTarget) > _w.Range)
                return false;
            return (Player.GetAutoAttackDamage(wTarget) + _w.GetDamage(wTarget) >
                    HealthPrediction.GetHealthPrediction(
                        wTarget,
                        (int)
                            ((Player.Distance(wTarget) / _w.Speed) * 1000 +
                             (Player.Distance(wTarget) / Orbwalking.GetMyProjectileSpeed()) * 1000) + (Game.Ping / 2)) &&
                    Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(null));

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
            return Player.AttackRange > 565f;
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

        public static bool IsMenuEnabled(String opt)
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

        #region Items
        static void UseItems(Obj_AI_Hero tar)
        {
            var ownH = GetPerValue(false);
            if ((Menu.Item("BotrkC").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && (Menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= ownH) &&
                ((Menu.Item("EnHPercBotrk").GetValue<Slider>().Value <= tar.HealthPercentage())))
            {
                UseItem(3153, tar);
            }
            if ((Menu.Item("BotrkH").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) && (Menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= ownH) &&
               ((Menu.Item("EnHPercBotrk").GetValue<Slider>().Value <= tar.HealthPercentage())))
            {
                UseItem(3153, tar);
            }
            if (Menu.Item("YoumuuC").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                UseItem(3142);
            }
            if (Menu.Item("YoumuuH").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                UseItem(3142);
            }
            if (Menu.Item("BilgeC").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                UseItem(3144, tar);
            }
            if (Menu.Item("BilgeH").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                UseItem(3144, tar);
            }
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
            Cleanser.CreateQSSSpellList();

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

            var FarmMenu = new Menu("[PJ] Farm", "Farm");
            {
                FarmMenu.AddItem(new MenuItem("UseQLC", "Use Q Laneclear").SetValue(true));
                FarmMenu.AddItem(new MenuItem("UseWLH", "Use W Lasthit").SetValue(true));
                FarmMenu.AddItem(new MenuItem("UseWLC", "Use W Laneclear").SetValue(true));
                FarmMenu.AddItem(new MenuItem("MinQMinions", "Min Minions for Q").SetValue(new Slider(0,4,6)));
            }
            var manaManagerFarm = new Menu("Mana Manager", "mm_Farm");
            {
                manaManagerFarm.AddItem(new MenuItem("QManaLC", "Q Mana Laneclear").SetValue(new Slider(15)));
                manaManagerFarm.AddItem(new MenuItem("WManaLH", "W Mana Lasthit").SetValue(new Slider(35)));
                manaManagerFarm.AddItem(new MenuItem("WManaLC", "W Mana Laneclear").SetValue(new Slider(35)));
            }

            FarmMenu.AddSubMenu(manaManagerFarm);
            Menu.AddSubMenu(FarmMenu);

            var miscMenu = new Menu("[PJ] Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("Packets", "Use Packets").SetValue(true));
                miscMenu.AddItem(new MenuItem("AntiGP", "Anti Gapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("Interrupter", "Use Interrupter").SetValue(true));
                miscMenu.AddItem(new MenuItem("SwitchQNoEn", "Switch to Minigun when no enemies").SetValue(true));
                miscMenu.AddItem(new MenuItem("C_Hit", "Hitchance").SetValue(new StringList(new[] {"Low","Medium","High","Very High"},2)));
                miscMenu.AddItem(new MenuItem("SpriteDraw", "Draw Sprite for R Killable").SetValue(true));
                miscMenu.AddItem(new MenuItem("ManualR", "Manual R").SetValue(new KeyBind("T".ToCharArray()[0],KeyBindType.Press)));
                miscMenu
                    .AddItem(
                        new MenuItem("ThreshLantern", "Grab Thresh Lantern").SetValue(new KeyBind("S".ToCharArray()[0],
                            KeyBindType.Press)));
            }
            Menu.AddSubMenu(miscMenu);

            var autoMenu = new Menu("[PJ] Auto Harrass", "Auto");
            {
                autoMenu.AddItem(new MenuItem("AutoE", "Auto E Slow/Immobile").SetValue(true));
                autoMenu.AddItem(new MenuItem("AutoE_Mana", "Auto E Mana").SetValue(new Slider(35)));
                autoMenu.AddItem(new MenuItem("AutoW", "Auto W").SetValue(true));
                autoMenu.AddItem(new MenuItem("AutoW_Mana", "Auto W Mana").SetValue(new Slider(40)));
                autoMenu.AddItem(new MenuItem("AutoWEmp", "Auto W Slow/Immobile").SetValue(true));
                autoMenu.AddItem(new MenuItem("AutoWEmp_Mana", "Auto W Slow/Imm Mana").SetValue(new Slider(40)));
            }
            Menu.AddSubMenu(autoMenu);

            var ItemsMenu = new Menu("[PJ] Items", "Items");
            {
                ItemsMenu.AddItem(new MenuItem("BotrkC", "Botrk Combo").SetValue(true));
                ItemsMenu.AddItem(new MenuItem("BotrkH", "Botrk Harrass").SetValue(false));
                ItemsMenu.AddItem(new MenuItem("YoumuuC", "Youmuu Combo").SetValue(true));
                ItemsMenu.AddItem(new MenuItem("YoumuuH", "Youmuu Harrass").SetValue(false));
                ItemsMenu.AddItem(new MenuItem("BilgeC", "Cutlass Combo").SetValue(true));
                ItemsMenu.AddItem(new MenuItem("BilgeH", "Cutlass Harrass").SetValue(false));
                ItemsMenu.AddItem(new MenuItem("OwnHPercBotrk", "Min Own H. % Botrk").SetValue(new Slider(50, 1, 100)));
                ItemsMenu.AddItem(new MenuItem("EnHPercBotrk", "Min Enemy H. % Botrk").SetValue(new Slider(20, 1, 100)));   
            }
            Menu.AddSubMenu(ItemsMenu);

            Menu.AddSubMenu(new Menu("[PJ] QSS Buff Types", "QSST"));
            Cleanser.CreateTypeQSSMenu();
            Menu.AddSubMenu(new Menu("[PJ] QSS Spells", "QSSSpell"));
            Cleanser.CreateQSSSpellMenu();

            Menu.AddSubMenu(new Menu("[PJ] AutoPot", "AutoPot"));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APH", "Health Pot").SetValue(true));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APM", "Mana Pot").SetValue(true));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APH_Slider", "Health Pot %").SetValue(new Slider(35, 1)));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APM_Slider", "Mana Pot %").SetValue(new Slider(35, 1)));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APHeal", "Use Heal").SetValue(true));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APHeal_Slider", "Heal %").SetValue(new Slider(35, 1)));

            var DrawMenu = new Menu("[PJ] Drawings", "Drawing");
            {
                DrawMenu.AddItem(new MenuItem("DrawQ", "Draw Q").SetValue(new Circle(true, System.Drawing.Color.Red)));
                DrawMenu.AddItem(
                    new MenuItem("DrawW", "Draw W").SetValue(new Circle(true, System.Drawing.Color.MediumPurple)));
                DrawMenu.AddItem(
                    new MenuItem("DrawE", "Draw E").SetValue(new Circle(true, System.Drawing.Color.MediumPurple)));
                DrawMenu.AddItem(new MenuItem("DrawR", "Draw R").SetValue(new Circle(true, System.Drawing.Color.MediumPurple)));
            }
            Menu.AddSubMenu(DrawMenu);

            Menu.AddToMainMenu();
        }

        #endregion
    }
}
