using System;
using System.Collections.Generic;
using LeagueSharp;
using Colro = System.Drawing.Color;
namespace DZAIO.Utility.Helpers
{
    class DebugHelper
    {
        public static Dictionary<String,String> DebugDictionary = new Dictionary<string, string>();
        private static float _lastPrint;

        public static void OnLoad()
        {
            LeagueSharp.Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (!DZAIO.IsDebug)
                return;
            var counter = 1;
            foreach (var entry in DebugDictionary)
            {
                LeagueSharp.Drawing.DrawText(25f, 10f + (20f * counter), System.Drawing.Color.White, entry.Key + ": " + entry.Value);
                counter++;
            }
        }

        public static void AddEntry(String key, String value)
        {
            if (DebugDictionary.ContainsKey(key))
            {
                DebugDictionary[key] = value;
            }
            else
            {
                DebugDictionary.Add(key,value); 
            }
        }

        public static void PrintDebug(String message)
        {
            if (!DZAIO.IsDebug)
                return;
            if (Environment.TickCount - _lastPrint > 250)
            {
                _lastPrint = Environment.TickCount;
                Game.PrintChat("<font color='#FF0000'>[DZAIO]</font> <font color='#FFFFFF'>" + message + "</font>");
            }
        }
    }
}
