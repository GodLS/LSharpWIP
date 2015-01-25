﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using SharpDX;

using Colro = System.Drawing.Color;
namespace DZAIO.Utility.Helpers
{
    class DebugHelper
    {
        public static Dictionary<String,String> DebugDictionary = new Dictionary<string, string>();
        public static void OnLoad()
        {
            if (!DZAIO.IsDebug)
                return;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            var counter = 1;
            foreach (var entry in DebugDictionary)
            {
                Drawing.DrawText(100f, 100f + (20f * counter), System.Drawing.Color.White, entry.Key + ": " + entry.Value);
                counter++;
            }
        }

        public static void AddEntry(String Key, String Value)
        {
            if (DebugDictionary.ContainsKey(Key))
            {
                DebugDictionary[Key] = Value;
            }
            else
            {
                DebugDictionary.Add(Key,Value);
                
            }
        }
    }
}