using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAIO.Utility.Helpers
{
    class UpdateHelper
    {
        public static void UpdateCheck()
        {
            var client = new BetterWebClient(new CookieContainer());
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var myThread = new Thread(() =>
                {
                    try
                    {
                        var currentClientData =client.DownloadString("https://github.com/DZ191/LSharpWIP/blob/master/DZAIO/Properties/AssemblyInfo.cs");
                        Match match =new Regex(@"\[assembly\: AssemblyVersion\(""(\d{1,4})\.(\d{1,4})\.(\d{1,4})\.(\d{1,4})""\)\]").Match(currentClientData);
                        if (match.Success)
                        {
                            var myFormat = String.Format("{0}.{1}.{2}.{3}", match.Groups[0], match.Groups[1], match.Groups[2], match.Groups[3]);
                            var remoteVersion = new System.Version(myFormat);
                            if (remoteVersion > currentVersion)
                            {
                                Game.PrintChat("<font color='purple'>[DZAIO]</font><font color='white'> Update found! Update to version: "+remoteVersion+"</font>");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Caught exception {0}",exception);
                    }
                });
            myThread.Start();      
        }
    }
}
