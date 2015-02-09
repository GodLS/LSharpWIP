using System;
using System.Collections.Generic;
using System.Reflection;
using DZAIO;
using DZAIO.Champions;
using DZAIO.Utility;
using DZAIO.Utility.Helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO
{
    class DZAIO
    {
        /**
         ____ _____   _    ___ ___  
        |  _ \__  /  / \  |_ _/ _ \ 
        | | | |/ /  / _ \  | | | | |
        | |_| / /_ / ___ \ | | |_| |
        |____/____/_/   \_\___\___/ 
         */
                            
        public static Dictionary<String, Func<IChampion>> ChampList = new Dictionary<string, Func<IChampion>>
        { 
           {"Jinx",() => new Jinx()},
           {"Graves",() => new Graves()},
           {"Zilean",() => new Zilean()},
           {"Lux",() => new Lux()},
           {"Cassiopeia",() => new Cassiopeia()}
        };
        public static Menu Config { get; set; }
        public static Orbwalking.Orbwalker Orbwalker { get; set; }
        public static Obj_AI_Hero Player { get; set; }
        public static IChampion CurrentChampion { get; set; }

        public static bool IsDebug = true;
        public static int Revision = 7;

        public DZAIO()
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        public static void OnLoad(EventArgs args)
        {
            Console.Clear();
            Player = ObjectManager.Player;
            Config = new Menu("DZAIO", "AsunaAIO", true);
            TargetSelector.AddToMenu(Config.SubMenu("Target selector"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Game.PrintChat("<b><font color='#FF0000'>[DZAIO]</font></b><font color='#FFFFFF'> DZAIO Loaded!</font> v{0}", Assembly.GetExecutingAssembly().GetName().Version);
            if (ChampList.ContainsKey(Player.ChampionName))
            {
                CurrentChampion = ChampList[Player.ChampionName].Invoke();
                CurrentChampion.OnLoad(Config);
                CurrentChampion.SetUpSpells();
                CurrentChampion.RegisterEvents();
                ItemManager.OnLoad(Config);
                Game.PrintChat("Loaded <b><font color='#FF0000'>{0}</font></b> plugin! <font color='#FFFFFF'> Have fun! </font>", Player.ChampionName);
            }
            Game.PrintChat("Special credits to:  <b><font color='#FF0000'>Hellsing</font></b> - Damage Indicator");

            Cleanser.OnLoad();
            ChatHook.OnLoad();
            DebugHelper.OnLoad();
            PotionManager.OnLoad(Config);
            Config.AddToMainMenu();
        }
    }
}
