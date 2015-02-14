using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Notifications;
using SharpDX;
using SharpDX.Direct3D9;

namespace DZAIO.Utility.Drawing
{
    class NotificationDrawing
    {
        public static Notification testNotification;
        public static void OnLoad()
        {
            var dzaioNotification = new Notification("[DZAIO]", -1)
            {
                TextColor = Color.Red,
                Font =
                    new Font(
                        LeagueSharp.Drawing.Direct3DDevice, 0xd, 0x0, FontWeight.Bold, 0x0, false,
                        FontCharacterSet.Default, FontPrecision.Default, FontQuality.Antialiased,
                        FontPitchAndFamily.DontCare | FontPitchAndFamily.Decorative, "Tahoma"),
                BorderColor = Color.White
            };
            var CassioNotification = new Notification("Cassiopeia Loaded", -1);
            Notifications.Notifications.AddNotification(dzaioNotification);
            Notifications.Notifications.AddNotification(CassioNotification);
        }
    }
}
