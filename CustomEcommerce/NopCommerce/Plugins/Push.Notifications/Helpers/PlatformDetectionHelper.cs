using Nop.Plugin.Misc.PushNotifications.Constants;

namespace Nop.Plugin.Misc.PushNotifications.Helpers
{
    public static class PlatformDetectionHelper
    {
        public static string DetectNotificationType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return NotificationTypes.FCM;

            var lowerUserAgent = userAgent.ToLower();
            
            // Detectar iOS (iPhone, iPad, iPod)
            if (lowerUserAgent.Contains("iphone") || 
                lowerUserAgent.Contains("ipad") || 
                lowerUserAgent.Contains("ipod") ||
                lowerUserAgent.Contains("ios"))
            {
                return NotificationTypes.WebPush;
            }

            // Para Safari en macOS también usar Web Push
            if (lowerUserAgent.Contains("safari") && lowerUserAgent.Contains("macintosh"))
            {
                return NotificationTypes.WebPush;
            }

            // Para otros navegadores usar FCM por defecto
            return NotificationTypes.FCM;
        }

        public static bool IsIOSDevice(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            var lowerUserAgent = userAgent.ToLower();
            return lowerUserAgent.Contains("iphone") || 
                   lowerUserAgent.Contains("ipad") || 
                   lowerUserAgent.Contains("ipod") ||
                   lowerUserAgent.Contains("ios");
        }

        public static bool IsSafariMacOS(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            var lowerUserAgent = userAgent.ToLower();
            return lowerUserAgent.Contains("safari") && 
                   lowerUserAgent.Contains("macintosh") && 
                   !lowerUserAgent.Contains("chrome");
        }
    }
}
