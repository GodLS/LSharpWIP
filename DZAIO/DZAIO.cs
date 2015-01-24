using System;
using System.Collections.Generic;
using DZAIO;
using DZAIO.Champions;
using DZAIO.Utility;
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

        public static void OnLoad()
        {
            Player = ObjectManager.Player;
            Config = new Menu("DZ/Asuna's - AIO", "AsunaAIO", true);
            TargetSelector.AddToMenu(Config.SubMenu("Target selector"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            if (champList.ContainsKey(Player.ChampionName))
            {
                CurrentChampion = champList[Player.ChampionName].Invoke();
                CurrentChampion.OnLoad(Config);
                CurrentChampion.SetUpSpells();
                CurrentChampion.RegisterEvents();
                Cleanser.initList();
            }
            Config.AddToMainMenu();

            Game.PrintChat("<font color='#FF0000'>DZ/Asuna</font><font color='#FFFFFF'> AIO Loaded!</font>");
            Game.PrintChat("Playing as <font color='#FF0000'>" + Player.ChampionName + "</font><font color='#FFFFFF'> Have fun! </font>");
        }
    }
}
