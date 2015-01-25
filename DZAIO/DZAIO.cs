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
        public static Dictionary<String, Func<IChampion>> champList = new Dictionary<string, Func<IChampion>>
        { 
           {"Jinx",() => new Jinx()},
           {"Graves",() => new Graves()},
        };
        public static Menu Config { get; set; }
        public static Orbwalking.Orbwalker Orbwalker { get; set; }
        public static Obj_AI_Hero Player { get; set; }
        public static IChampion CurrentChampion { get; set; }

        public static bool IsDebug = false;

        public static void OnLoad()
        {
            Player = ObjectManager.Player;
            Config = new Menu("DZ/Asuna AIO", "AsunaAIO", true);
            TargetSelector.AddToMenu(Config.SubMenu("Target selector"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Game.PrintChat("<font color='#FF0000'>DZ/Asuna</font><font color='#FFFFFF'> AIO Loaded! Version: </font>"+Assembly.GetExecutingAssembly().ImageRuntimeVersion);

            if (champList.ContainsKey(Player.ChampionName))
            {
                CurrentChampion = champList[Player.ChampionName].Invoke();
                CurrentChampion.OnLoad(Config);
                CurrentChampion.SetUpSpells();
                CurrentChampion.RegisterEvents();
                Cleanser.initList();
                Game.PrintChat("Loaded <font color='#FF0000'>" + Player.ChampionName + "</font> plugin! <font color='#FFFFFF'> Have fun! </font>");
            }
            DebugHelper.OnLoad();
            PotionManager.OnLoad(Config);

            Config.AddToMainMenu();
        }
    }
}
