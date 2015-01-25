using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO
{
    interface IChampion
    {
        void OnLoad(Menu menu);
        void RegisterEvents();
        void SetUpSpells();
    }
}
