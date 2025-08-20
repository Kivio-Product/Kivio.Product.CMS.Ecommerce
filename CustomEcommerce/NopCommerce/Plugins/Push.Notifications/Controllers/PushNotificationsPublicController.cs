using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.PushNotifications.Models;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Plugin.Misc.PushNotifications.Helpers;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Controllers
{
    public class PushNotificationsPublicController : Controller
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IWorkContext _workContext;

        public PushNotificationsPublicController(IPushNotificationService pushNotificationService, IWorkContext workContext)
        { 
            _pushNotificationService = pushNotificationService;
            _workContext = workContext;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterDevice([FromBody] PushSubscriptionModel model)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var userAgent = Request.Headers["User-Agent"].ToString();
            
            // Auto-detect notification type if not specified
            if (string.IsNullOrEmpty(model.Type))
            {
                model.Type = PlatformDetectionHelper.DetectNotificationType(userAgent);
            }
            
            model.UserAgent = userAgent;
            
            await _pushNotificationService.RegisterDeviceAsync(customer.Id, model);
            return Json(new { success = true, type = model.Type });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterDeviceLegacy([FromBody] dynamic data)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var userAgent = Request.Headers["User-Agent"].ToString();
            string token = data.token;
            
            var model = new PushSubscriptionModel
            {
                Token = token,
                Type = PlatformDetectionHelper.DetectNotificationType(userAgent),
                UserAgent = userAgent
            };
            
            await _pushNotificationService.RegisterDeviceAsync(customer.Id, model);
            return Json(new { success = true, type = model.Type });
        }
    }
}