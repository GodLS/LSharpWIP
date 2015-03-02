using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace DZAIO.Utility.Helpers
{
    internal class NotificationHelper
    {
        private static float _lastNotificationTime = 0f;

        //I know you'll hate me for this Worstping, but ain't nobody got time to manage notifications Kappa.
        //TODO Better implementation, soon™
        public static void AddNotification(String text, int duration)
        {
            if (Environment.TickCount - _lastNotificationTime <= 650f)
            {
                return;
            }
            _lastNotificationTime = Environment.TickCount;
            Notifications.AddNotification(new Notification(text, duration));
        }
    }
}
