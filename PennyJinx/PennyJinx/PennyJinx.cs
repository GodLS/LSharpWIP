using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Network.Packets;

namespace PennyJinx
{
    internal class PennyJinx
    {
        private const String ChampName = "Jinx";
        public static Obj_AI_Hero Player;
        public static Spell Q, W, E, R;
        public static Menu Menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly StringList QMode = new StringList(new[] {"AOE mode", "Range mode", "Both"}, 2);
        public static Render.Sprite Sprite;
        public static PennyJinx Instance;
        public static float LastCheck;

        public PennyJinx()
        {
            Instance = this;
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static HitChance CustomHitChance
        {
            get { return GetHitchance(); }
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
            Game.PrintChat("<font color='#7A6EFF'>PennyJinx</font> v 1.0.1.9 <font color='#FFFFFF'>Loaded!</font>");


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

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var sender = (Obj_AI_Hero) gapcloser.Sender;
            if (!sender.IsValidTarget() || !IsMenuEnabled("AntiGP") || !E.IsReady())
            {
                return;
            }

            E.Cast(gapcloser.End, Packets());
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var sender = (Obj_AI_Hero) unit;
            if (!sender.IsValidTarget() || !IsMenuEnabled("Interrupter") || !E.IsReady())
            {
                return;
            }

            E.CastIfHitchanceEquals(sender, CustomHitChance, Packets());
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var target = args.Target;
            if (!target.IsValidTarget())
            {
                return;
            }

            if (!(target is Obj_AI_Minion) ||
                (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear &&
                 _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit))
            {
                return;
            }

            var t2 = (Obj_AI_Minion) target;
            QSwitchLc(t2);
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
            {
                return;
            }

            if (!(target is Obj_AI_Hero))
            {
                return;
            }

            var tar = (Obj_AI_Hero) target;
            UseItems(tar);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Auto();

            if (Menu.Item("ManualR").GetValue<KeyBind>().Active)
            {
                ManualR();
            }

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
            if (Menu.Item("ThreshLantern").GetValue<KeyBind>().Active)
            {
                TakeLantern();
            }

            UseSpellOnTeleport(E);
            AutoPot();

            // Cleanser.cleanserByBuffType();
            //Cleanser.cleanserBySpell();
        }

        #region Drawing

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = Menu.Item("DrawQ").GetValue<Circle>();
            var drawW = Menu.Item("DrawW").GetValue<Circle>();
            var drawE = Menu.Item("DrawE").GetValue<Circle>();
            var drawR = Menu.Item("DrawR").GetValue<Circle>();
            var qRange = IsFishBone()
                ? 525f + ObjectManager.Player.BoundingRadius + 65f
                : 525f + ObjectManager.Player.BoundingRadius + 65f + GetFishboneRange() + 20f;
            if (drawQ.Active)
            {
                Utility.DrawCircle(Player.Position, qRange, drawQ.Color);
            }

            if (drawW.Active)
            {
                Utility.DrawCircle(Player.Position, W.Range, drawW.Color);
            }

            if (drawE.Active)
            {
                Utility.DrawCircle(Player.Position, E.Range, drawE.Color);
            }

            if (drawR.Active)
            {
                Utility.DrawCircle(Player.Position, R.Range, drawR.Color);
            }
        }

        #endregion

        #region Items

        private static void UseItems(Obj_AI_Hero tar)
        {
            var ownH = GetPerValue(false);
            if ((Menu.Item("BotrkC").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) &&
                (Menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= ownH) &&
                ((Menu.Item("EnHPercBotrk").GetValue<Slider>().Value <= tar.HealthPercentage())))
            {
                UseItem(3153, tar);
            }

            if ((Menu.Item("BotrkH").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) &&
                (Menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= ownH) &&
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

        #region Various

        public void UseSpellOnTeleport(Spell spell)
        {
            if (!IsMenuEnabled("EOnTP") || (Environment.TickCount - LastCheck) < 1500)
            {
                return;
            }

            LastCheck = Environment.TickCount;
            var player = ObjectManager.Player;
            if (!spell.IsReady())
            {
                return;
            }

            foreach (
                var targetPosition in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            obj =>
                                obj.Distance(player) < spell.Range && obj.Team != player.Team &&
                                obj.HasBuff("teleport_target", true)))
            {
                spell.Cast(targetPosition.ServerPosition);
            }
        }

        private static void AutoPot()
        {
            if (ObjectManager.Player.HasBuff("Recall") || Player.InFountain() && Player.InShop())
            {
                return;
            }

            //Health Pots
            if (IsMenuEnabled("APH") && GetPerValue(false) <= Menu.Item("APH_Slider").GetValue<Slider>().Value &&
                !Player.HasBuff("RegenerationPotion", true))
            {
                UseItem(2003);
            }
            //Mana Pots
            if (IsMenuEnabled("APM") && GetPerValue(true) <= Menu.Item("APM_Slider").GetValue<Slider>().Value &&
                !Player.HasBuff("FlaskOfCrystalWater", true))
            {
                UseItem(2004);
            }

            //Summoner Heal
            if (!IsMenuEnabled("APHeal") || !(GetPerValue(false) <= Menu.Item("APHeal_Slider").GetValue<Slider>().Value))
            {
                return;
            }

            var heal = Player.GetSpellSlot("summonerheal");
            if (heal != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(heal) == SpellState.Ready)
            {
                Player.Spellbook.CastSpell(heal);
            }
        }

        private static void TakeLantern()
        {
            foreach (var interactPkt in from obj in ObjectManager.Get<GameObject>()
                where
                    obj.Name.Contains("ThreshLantern") &&
                    obj.Position.Distance(ObjectManager.Player.ServerPosition) <= 500 && obj.IsAlly
                select new PKT_InteractReq
                {
                    NetworkId = Player.NetworkId,
                    TargetNetworkId = obj.NetworkId
                })
            {
                //Credits to Trees
                Game.SendPacket(interactPkt.Encode(), PacketChannel.C2S, PacketProtocolFlags.Reliable);
                return;
            }
        }

        private static void SwitchLc()
        {
            if (!Q.IsReady())
            {
                return;
            }

            if (IsFishBone())
            {
                Q.Cast();
            }
        }

        private static void SwitchNoEn()
        {
            if (!IsMenuEnabled("SwitchQNoEn"))
            {
                return;
            }

            var range = IsFishBone()
                ? 525f + ObjectManager.Player.BoundingRadius + 65f
                : 525f + ObjectManager.Player.BoundingRadius + 65f + GetFishboneRange() + 20f;
            if (Player.CountEnemysInRange((int) range) != 0)
            {
                return;
            }

            if (IsFishBone())
            {
                Q.Cast();
            }
        }

        #endregion

        #region Combo/Harrass/Auto

        private void Auto()
        {
            if (GetEMode() == 0)
            {
                ECast_DZ();
            }
            else
            {
                ECast();
            }
            SwitchNoEn();
            AutoWHarass();
            AutoWEmpaired();
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
            if (GetEMode() == 0)
            {
                ECast_DZ();
            }
            else
            {
                ECast();
            }
        }

        #endregion

        #region Farm

        private static void QSwitchLc(Obj_AI_Minion t2)
        {
            if (!IsMenuEnabled("UseQLC") || !Q.IsReady() || GetPerValue(true) < GetSliderValue("QManaLC"))
            {
                return;
            }

            if (CountEnemyMinions(t2, 150) < GetSliderValue("MinQMinions"))
            {
                SwitchLc();
            }
            else
            {
                if (!IsFishBone() && GetPerValue(true) >= GetSliderValue("QManaLC"))
                {
                    Q.Cast();
                }
            }
        }

        private static void WUsageFarm()
        {
            var mode = _orbwalker.ActiveMode;
            var wMana = mode == Orbwalking.OrbwalkingMode.LaneClear
                ? GetSliderValue("WManaLC")
                : GetSliderValue("WManaLH");
            var wEnabled = mode == Orbwalking.OrbwalkingMode.LaneClear
                ? IsMenuEnabled("UseWLC")
                : IsMenuEnabled("UseWLH");
            var mList = MinionManager.GetMinions(Player.Position, W.Range);
            var location = W.GetLineFarmLocation(mList);
            if (GetPerValue(true) >= wMana && wEnabled)
            {
                W.Cast(location.Position);
            }
        }

        #endregion

        #region Spell Casting

        private static void QManager(String mode)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var aaRange = GetMinigunRange(null) + GetFishboneRange() + 25f;
            var target = TargetSelector.GetTarget(aaRange, TargetSelector.DamageType.Physical);
            var jinxBaseRange = GetMinigunRange(target);

            if (!target.IsValidTarget(aaRange + GetFishboneRange() + 25f))
            {
                return;
            }

            switch (Menu.Item("QMode").GetValue<StringList>().SelectedIndex)
            {
                //AOE Mode
                case 0:
                    if (IsFishBone() && GetPerValue(true) <= GetSliderValue("QMana" + mode))
                    {
                        Q.Cast();
                        return;
                    }
                    if (target.CountEnemysInRange(150) > 1)
                    {
                        if (!IsFishBone())
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        if (IsFishBone())
                        {
                            Q.Cast();
                        }
                    }
                    break;
                //Range Mode
                case 1:
                    if (IsFishBone())
                    {
                        //Switching to Minigun
                        if (Player.Distance(target) < jinxBaseRange ||
                            GetPerValue(true) <= GetSliderValue("QMana" + mode))
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > jinxBaseRange &&
                            GetPerValue(true) >= GetSliderValue("QMana" + mode))
                        {
                            Q.Cast();
                        }
                    }
                    break;
                //Both
                case 2:
                    if (IsFishBone())
                    {
                        //Switching to Minigun
                        if (Player.Distance(target) < jinxBaseRange ||
                            GetPerValue(true) <= GetSliderValue("QMana" + mode))
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        //Switching to rockets
                        if (Player.Distance(target) > jinxBaseRange &&
                            GetPerValue(true) >= GetSliderValue("QMana" + mode) ||
                            target.CountEnemysInRange(150) > 1)
                        {
                            Q.Cast();
                        }
                    }
                    break;
            }
        }

        private static void WCast(Orbwalking.OrbwalkingMode mode)
        {
            if (mode != Orbwalking.OrbwalkingMode.Combo && mode != Orbwalking.OrbwalkingMode.Mixed || !W.IsReady())
            {
                return;
            }

            if (Player.CountEnemysInRange((int) Player.AttackRange) != 0)
            {
                return;
            }

            //If the mode is combo then we use the WManaC, if the mode is Harrass we use the WManaH
            var str = (mode == Orbwalking.OrbwalkingMode.Combo) ? "C" : "H";
            //Get a target in W range
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (!wTarget.IsValidTarget(W.Range))
            {
                return;
            }

            var wMana = GetSliderValue("WMana" + str);
            if (GetPerValue(true) >= wMana && IsMenuEnabled("UseW"+str))
            {
                W.CastIfHitchanceEquals(wTarget, CustomHitChance, Packets());
            }
        }

        private static void ECast()
        {
            //Credits to Marksman
            //http://github.com/Esk0r/Leaguesharp/

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range - 150)))
            {
                if (!IsMenuEnabled("AutoE") || !E.IsReady() || !enemy.HasBuffOfType(BuffType.Slow))
                {
                    return;
                }

                var castPosition =
                    Prediction.GetPrediction(
                        new PredictionInput
                        {
                            Unit = enemy,
                            Delay = 0.7f,
                            Radius = 120f,
                            Speed = 1750f,
                            Range = 900f,
                            Type = SkillshotType.SkillshotCircle
                        }).CastPosition;
                if (GetSlowEndTime(enemy) >= (Game.Time + E.Delay + 0.5f))
                {
                    E.Cast(castPosition);
                }

                if (IsMenuEnabled("AutoE") && E.IsReady() &&
                    (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                     enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                     enemy.HasBuffOfType(BuffType.Taunt)))
                {
                    E.CastIfHitchanceEquals(enemy, HitChance.High);
                }
            }
        }

        private static void ECast_DZ()
        {
            if (!E.IsReady())
            {
                return;
            }

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range - 140f) && (IsEmpaired(h))))
            {
                //E necessary mana. If the mode is combo: Combo mana, if not AutoE mana
                var eMana = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo
                    ? GetSliderValue("EManaC")
                    : GetSliderValue("AutoE_Mana");

                if ((!IsMenuEnabled("UseEC") && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) || (!IsMenuEnabled("AutoE") && _orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo))
                {
                    return;
                }


                //If it is slowed & moving
                if (IsEmpairedLight(enemy) && IsMoving(enemy))
                {
                    //Has enough E Mana ?
                    if (GetPerValue(true) >= eMana)
                    {
                        //Casting using predictions
                        E.CastIfHitchanceEquals(enemy, CustomHitChance, Packets());
                        return;
                    }
                }
                //If the empairement ends later, cast the E
                if (GetPerValue(true) >= eMana)
                {
                    //Casting using predictions
                    E.CastIfHitchanceEquals(enemy, CustomHitChance, Packets());
                }
            }
        }
        private static void RCast()
        {
            //TODO R Collision
            if (!R.IsReady())
            {
                return;
            }

            var rTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (!rTarget.IsValidTarget(R.Range))
            {
                return;
            }
            //If is killable with W and AA
            //Or the ally players in there are > 0
            if (IsKillableWaa(rTarget) ||
                CountAllyPlayers(rTarget, 500) > 0 || Player.Distance(rTarget) < (W.Range/2))
            {
                return;
            }

            //Check for Mana && for target Killable. Also check for hitchance
            if (GetPerValue(true) >= GetSliderValue("RManaC") && IsMenuEnabled("UseRC") &&
                R.GetDamage(rTarget) >=
                HealthPrediction.GetHealthPrediction(rTarget, (int) (Player.Distance(rTarget)/2000f)*1000))
            {
                R.CastIfHitchanceEquals(rTarget, CustomHitChance, Packets());
            }
        }
        private static void ManualR()
        {
            //TODO R Collision
            if (!R.IsReady())
            {
                return;
            }

            var rTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (!rTarget.IsValidTarget(R.Range))
            {
                return;
            }


            //Check for Mana && for target Killable. Also check for hitchance
            if (R.GetDamage(rTarget) >=
                HealthPrediction.GetHealthPrediction(rTarget, (int)(Player.Distance(rTarget) / 2000f) * 1000))
            {
                R.CastIfHitchanceEquals(rTarget, CustomHitChance, Packets());
            }
        }
        #endregion

        #region AutoSpells

        private static void AutoWHarass()
        {
            //Uses W in Harrass, factoring hitchance
            if (!IsMenuEnabled("AutoW") || Player.IsRecalling())
            {
                return;
            }

            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            var autoWMana = GetSliderValue("AutoW_Mana");
            if (!wTarget.IsValidTarget())
            {
                return;
            }

            if (GetPerValue(true) >= autoWMana || IsKillableWaa(wTarget))
            {
                W.CastIfHitchanceEquals(wTarget, CustomHitChance, Packets());
            }
        }

        private void AutoWEmpaired()
        {
            if (!IsMenuEnabled("AutoWEmp") || Player.IsRecalling())
            {
                return;
            }

            //Uses W on whoever is empaired
            foreach (
                var enemy in
                    (from enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range))
                        let autoWMana = GetSliderValue("AutoWEmp_Mana")
                        where GetPerValue(true) >= autoWMana
                        select enemy).Where(enemy => IsEmpaired(enemy) || IsEmpairedLight(enemy)))
            {
                W.CastIfHitchanceEquals(enemy, CustomHitChance, Packets());
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

        private static bool Packets()
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

        private static HitChance GetHitchance()
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

        private static bool IsKillableWaa(Obj_AI_Hero wTarget)
        {
            if (Player.Distance(wTarget) > W.Range)
            {
                return false;
            }

            return (Player.GetAutoAttackDamage(wTarget) + W.GetDamage(wTarget) >
                    HealthPrediction.GetHealthPrediction(
                        wTarget,
                        (int)
                            ((Player.Distance(wTarget)/W.Speed)*1000 +
                             (Player.Distance(wTarget)/Orbwalking.GetMyProjectileSpeed())*1000) + (Game.Ping/2)) &&
                    Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(null));
        }


        private static int CountAllyPlayers(Obj_AI_Hero from, float distance)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(h => h.IsAlly && !h.IsMe && h.Distance(from) <= distance)
                    .ToList()
                    .Count;
        }

        private static int CountEnemyMinions(Obj_AI_Base from, float distance)
        {
            return MinionManager.GetMinions(from.Position, distance).ToList().Count;
        }

        private static bool IsFishBone()
        {
            return Player.AttackRange > 565f;
        }

        private static int GetEMode()
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
                    .Where(buff => GetEmpairedBuffs().Contains(buff.Type))
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

        private static bool IsMoving(Obj_AI_Base obj)
        {
            return obj.Path.Count() > 1;
        }

        private static List<BuffType> GetEmpairedBuffs()
        {
            return new List<BuffType>
            {
                BuffType.Stun,
                BuffType.Snare,
                BuffType.Charm,
                BuffType.Fear,
                BuffType.Taunt,
                BuffType.Slow
            };
        }

        #endregion

        #region Menu and spells setup

        private static void SetUpSpells()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2000f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.1f, 120f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);
        }

        private static void SetUpMenu()
        {
            Cleanser.CreateQssSpellList();

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
                comboMenu.AddItem(
                    new MenuItem("EMode", "E Mode").SetValue(new StringList(new[] {"PennyJinx", "Marksman"})));
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

            var farmMenu = new Menu("[PJ] Farm", "Farm");
            {
                farmMenu.AddItem(new MenuItem("UseQLC", "Use Q Laneclear").SetValue(true));
                farmMenu.AddItem(new MenuItem("UseWLH", "Use W Lasthit").SetValue(false));
                farmMenu.AddItem(new MenuItem("UseWLC", "Use W Laneclear").SetValue(false));
                farmMenu.AddItem(new MenuItem("MinQMinions", "Min Minions for Q").SetValue(new Slider(0, 4, 6)));
            }
            var manaManagerFarm = new Menu("Mana Manager", "mm_Farm");
            {
                manaManagerFarm.AddItem(new MenuItem("QManaLC", "Q Mana Laneclear").SetValue(new Slider(15)));
                manaManagerFarm.AddItem(new MenuItem("WManaLH", "W Mana Lasthit").SetValue(new Slider(35)));
                manaManagerFarm.AddItem(new MenuItem("WManaLC", "W Mana Laneclear").SetValue(new Slider(35)));
            }

            farmMenu.AddSubMenu(manaManagerFarm);
            Menu.AddSubMenu(farmMenu);

            var miscMenu = new Menu("[PJ] Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("Packets", "Use Packets").SetValue(true));
                miscMenu.AddItem(new MenuItem("AntiGP", "Anti Gapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("EOnTP", "E On Teleport Location").SetValue(true));
                miscMenu.AddItem(new MenuItem("Interrupter", "Use Interrupter").SetValue(true));
                miscMenu.AddItem(new MenuItem("SwitchQNoEn", "Switch to Minigun when no enemies").SetValue(true));
                miscMenu.AddItem(
                    new MenuItem("C_Hit", "Hitchance").SetValue(
                        new StringList(new[] {"Low", "Medium", "High", "Very High"}, 2)));
                miscMenu.AddItem(
                    new MenuItem("ManualR", "Manual R").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
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

            var itemsMenu = new Menu("[PJ] Items", "Items");
            {
                itemsMenu.AddItem(new MenuItem("BotrkC", "Botrk Combo").SetValue(true));
                itemsMenu.AddItem(new MenuItem("BotrkH", "Botrk Harrass").SetValue(false));
                itemsMenu.AddItem(new MenuItem("YoumuuC", "Youmuu Combo").SetValue(true));
                itemsMenu.AddItem(new MenuItem("YoumuuH", "Youmuu Harrass").SetValue(false));
                itemsMenu.AddItem(new MenuItem("BilgeC", "Cutlass Combo").SetValue(true));
                itemsMenu.AddItem(new MenuItem("BilgeH", "Cutlass Harrass").SetValue(false));
                itemsMenu.AddItem(new MenuItem("OwnHPercBotrk", "Min Own H. % Botrk").SetValue(new Slider(50, 1)));
                itemsMenu.AddItem(new MenuItem("EnHPercBotrk", "Min Enemy H. % Botrk").SetValue(new Slider(20, 1)));
            }
            Menu.AddSubMenu(itemsMenu);

            Menu.AddSubMenu(new Menu("[PJ] QSS Buff Types", "QSST"));
            Cleanser.CreateTypeQssMenu();
            Menu.AddSubMenu(new Menu("[PJ] QSS Spells", "QSSSpell"));
            Cleanser.CreateQssSpellMenu();

            Menu.AddSubMenu(new Menu("[PJ] AutoPot", "AutoPot"));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APH", "Health Pot").SetValue(true));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APM", "Mana Pot").SetValue(true));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APH_Slider", "Health Pot %").SetValue(new Slider(35, 1)));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APM_Slider", "Mana Pot %").SetValue(new Slider(35, 1)));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APHeal", "Use Heal").SetValue(true));
            Menu.SubMenu("AutoPot").AddItem(new MenuItem("APHeal_Slider", "Heal %").SetValue(new Slider(35, 1)));

            var drawMenu = new Menu("[PJ] Drawings", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("DrawQ", "Draw Q").SetValue(new Circle(true, Color.Red)));
                drawMenu.AddItem(
                    new MenuItem("DrawW", "Draw W").SetValue(new Circle(true, Color.MediumPurple)));
                drawMenu.AddItem(
                    new MenuItem("DrawE", "Draw E").SetValue(new Circle(true, Color.MediumPurple)));
                drawMenu.AddItem(new MenuItem("DrawR", "Draw R").SetValue(new Circle(true, Color.MediumPurple)));
                miscMenu.AddItem(new MenuItem("SpriteDraw", "Draw Sprite for R Killable").SetValue(false));
            }
            Menu.AddSubMenu(drawMenu);

            Menu.AddToMainMenu();
        }

        #endregion
    }
}
