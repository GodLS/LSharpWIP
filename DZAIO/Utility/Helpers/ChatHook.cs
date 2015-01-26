using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;

namespace DZAIO.Utility.Helpers
{
    class ChatHook
    {
        public static void OnLoad()
        {
            Game.OnGameInput += Game_OnGameInput;
        }

        static void Game_OnGameInput(GameInputEventArgs args)
        {
            if (args.Input.StartsWith("."))
            {
                args.Process = false;
            }

            switch (args.Input)
            {
                case ".debug":
                    DZAIO.IsDebug = !DZAIO.IsDebug;
                    Game.PrintChat("[DZAIO] Debug Status: "+DZAIO.IsDebug);
                    break;
                default:
                    return;
            }
        }
    }
}
