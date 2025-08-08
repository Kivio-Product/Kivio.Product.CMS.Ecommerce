using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.PushNotifications.Models;
using Nop.Plugin.Misc.PushNotifications.Services;
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
            await _pushNotificationService.RegisterDeviceAsync(customer.Id, model.Token);
            return Json(new { success = true });
        }
    }
}