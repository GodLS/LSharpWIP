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
            switch (args.Input)
            {
                case ".debug":
                    DZAIO.IsDebug = !DZAIO.IsDebug;
                    Game.PrintChat("Debug status: "+DZAIO.IsDebug);
                    args.Process = false;
                    break;
                default:
                    return;
            }
        }
    }
}
