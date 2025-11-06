using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.DeliveryTimePicker.Models;
using Nop.Plugin.Misc.DeliveryTimePicker.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public class DeliveryTimePickerController(
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IDeliveryTimeService deliveryTimeService) : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService = localizationService;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IPermissionService _permissionService = permissionService;
        private readonly ISettingService _settingService = settingService;
        private readonly IDeliveryTimeService _deliveryTimeService = deliveryTimeService;

        #endregion
        #region Ctor

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            var settings = await _settingService.LoadSettingAsync<DeliveryTimePickerSettings>();

            var model = new ConfigurationModel
            {
                Enabled = settings.Enabled,
                CutoffHour = settings.CutoffHour,
                MaxCapacityPerSlot = settings.MaxCapacityPerSlot,
                DisableWeekends = settings.DisableWeekends,
                TimeZoneId = settings.TimeZoneId,
                ExitoProductSkuPrefix = settings.ExitoProductSkuPrefix,
                AutoFetchHolidays = settings.AutoFetchHolidays,
                HolidayCountryCode = settings.HolidayCountryCode,
                ReservationTimeoutMinutes = settings.ReservationTimeoutMinutes,
                // Populate available time zones
                AvailableTimeZones = [.. TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new SelectListItem
                    {
                        Text = tz.DisplayName,
                        Value = tz.Id,
                        Selected = tz.Id == settings.TimeZoneId
                    })]
            };

            return View("~/Plugins/Misc.DeliveryTimePicker/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var settings = await _settingService.LoadSettingAsync<DeliveryTimePickerSettings>();

            settings.Enabled = model.Enabled;
            settings.CutoffHour = model.CutoffHour;
            settings.MaxCapacityPerSlot = model.MaxCapacityPerSlot;
            settings.DisableWeekends = model.DisableWeekends;
            settings.TimeZoneId = model.TimeZoneId;
            settings.ExitoProductSkuPrefix = model.ExitoProductSkuPrefix;
            settings.AutoFetchHolidays = model.AutoFetchHolidays;
            settings.HolidayCountryCode = model.HolidayCountryCode;
            settings.ReservationTimeoutMinutes = model.ReservationTimeoutMinutes;

            await _settingService.SaveSettingAsync(settings);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        public async Task<IActionResult> TimeSlotList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            return View("~/Plugins/Misc.DeliveryTimePicker/Views/TimeSlotList.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> TimeSlotListData()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            try
            {
                var timeSlots = await _deliveryTimeService.GetAllTimeSlotsAsync();

                var models = timeSlots.Select(slot => new DeliveryTimeSlotModel
                {
                    Id = slot.Id,
                    DayOfWeek = slot.DayOfWeek,
                    DayOfWeekName = slot.DayOfWeek == -1 
                        ? "Todos los días" 
                        : System.Globalization.CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetDayName((DayOfWeek)slot.DayOfWeek), 
                    StartTime = $"{slot.StartTime.Hours:D2}:{slot.StartTime.Minutes:D2}",
                    EndTime = $"{slot.EndTime.Hours:D2}:{slot.EndTime.Minutes:D2}",
                    IsEnabled = slot.IsEnabled,
                    MaxCapacity = slot.MaxCapacity,
                    DisplayOrder = slot.DisplayOrder
                }).ToList();

                return Json(new 
                {
                    Data = models,
                    recordsTotal = models.Count,
                    recordsFiltered = models.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new 
                {
                    Data = new List<DeliveryTimeSlotModel>(),
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    error = ex.Message
                });
            }
        }

        public async Task<IActionResult> CreateTimeSlot()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            var model = new DeliveryTimeSlotModel
            {
                IsEnabled = true,
                DisplayOrder = 0
            };
            PrepareTimeSlotModel(model);

            return View("~/Plugins/Misc.DeliveryTimePicker/Views/TimeSlotCreateEdit.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTimeSlot(DeliveryTimeSlotModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            if (!TimeSpan.TryParse(model.StartTimeString, out var startTime))
            {
                ModelState.AddModelError("StartTimeString", "Formato de hora inválido");
            }

            if (!TimeSpan.TryParse(model.EndTimeString, out var endTime))
            {
                ModelState.AddModelError("EndTimeString", "Formato de hora inválido");
            }

            if (startTime >= endTime)
            {
                ModelState.AddModelError("EndTimeString", "La hora de fin debe ser mayor que la hora de inicio");
            }

            if (ModelState.IsValid)
            {
                var conflictingSlot = await _deliveryTimeService.GetTimeSlotByHoursAndDayAsync(model.DayOfWeek, startTime, endTime);

                if (conflictingSlot != null)
                {
                    var dayName = model.DayOfWeek == -1 
                        ? "todos los días" 
                        : System.Globalization.CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetDayName((DayOfWeek)model.DayOfWeek).ToLower();
                    
                    ModelState.AddModelError("", 
                        $"Ya existe una franja horaria para {dayName} de {model.StartTimeString} a {model.EndTimeString}");
                }
            }

            if (!ModelState.IsValid)
            {
                PrepareTimeSlotModel(model);
                return View("~/Plugins/Misc.DeliveryTimePicker/Views/TimeSlotCreateEdit.cshtml", model);
            }

            var timeSlot = new Domain.DeliveryTimeSlot
            {
                DayOfWeek = model.DayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsEnabled = model.IsEnabled,
                MaxCapacity = model.MaxCapacity,
                DisplayOrder = model.DisplayOrder
            };

            await _deliveryTimeService.InsertTimeSlotAsync(timeSlot);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction("TimeSlotList");
        }

        public async Task<IActionResult> EditTimeSlot(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            var timeSlot = await _deliveryTimeService.GetTimeSlotByIdAsync(id);
            if (timeSlot == null)
                return RedirectToAction("TimeSlotList");

            var model = new DeliveryTimeSlotModel
            {
                Id = timeSlot.Id,
                DayOfWeek = timeSlot.DayOfWeek,
                StartTimeString = timeSlot.StartTime.ToString(@"hh\:mm"),
                EndTimeString = timeSlot.EndTime.ToString(@"hh\:mm"),
                IsEnabled = timeSlot.IsEnabled,
                MaxCapacity = timeSlot.MaxCapacity,
                DisplayOrder = timeSlot.DisplayOrder
            };

            PrepareTimeSlotModel(model);

            return View("~/Plugins/Misc.DeliveryTimePicker/Views/TimeSlotCreateEdit.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditTimeSlot(DeliveryTimeSlotModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            var timeSlot = await _deliveryTimeService.GetTimeSlotByIdAsync(model.Id);
            if (timeSlot == null)
                return RedirectToAction("TimeSlotList");

            // Validación adicional
            if (!TimeSpan.TryParse(model.StartTimeString, out var startTime))
            {
                ModelState.AddModelError("StartTimeString", "Formato de hora inválido");
            }

            if (!TimeSpan.TryParse(model.EndTimeString, out var endTime))
            {
                ModelState.AddModelError("EndTimeString", "Formato de hora inválido");
            }

            if (startTime >= endTime)
            {
                ModelState.AddModelError("EndTimeString", "La hora de fin debe ser mayor que la hora de inicio");
            }

            // Verificar si ya existe una franja con el mismo día y horario (excluyendo el actual)
            if (ModelState.IsValid)
            {
                var allSlots = await _deliveryTimeService.GetAllTimeSlotsAsync();
                var conflictingSlot = allSlots.FirstOrDefault(s => 
                    s.Id != model.Id && // Excluir el slot actual
                    s.DayOfWeek == model.DayOfWeek && 
                    s.StartTime == startTime && 
                    s.EndTime == endTime);

                if (conflictingSlot != null)
                {
                    var dayName = model.DayOfWeek == -1 
                        ? "todos los días" 
                        : System.Globalization.CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetDayName((DayOfWeek)model.DayOfWeek).ToLower();
                    
                    ModelState.AddModelError("", 
                        $"Ya existe una franja horaria para {dayName} de {model.StartTimeString} a {model.EndTimeString}");
                }
            }

            if (!ModelState.IsValid)
            {
                PrepareTimeSlotModel(model);
                return View("~/Plugins/Misc.DeliveryTimePicker/Views/TimeSlotCreateEdit.cshtml", model);
            }

            timeSlot.DayOfWeek = model.DayOfWeek;
            timeSlot.StartTime = startTime;
            timeSlot.EndTime = endTime;
            timeSlot.IsEnabled = model.IsEnabled;
            timeSlot.MaxCapacity = model.MaxCapacity;
            timeSlot.DisplayOrder = model.DisplayOrder;

            await _deliveryTimeService.UpdateTimeSlotAsync(timeSlot);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction("TimeSlotList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return Json(new { success = false, message = "Access denied" });

            var timeSlot = await _deliveryTimeService.GetTimeSlotByIdAsync(id);
            if (timeSlot == null)
                return Json(new { success = false, message = "Time slot not found" });

            await _deliveryTimeService.DeleteTimeSlotAsync(timeSlot);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Common.DataSuccessfullyDeleted"));

            return Json(new { success = true });
        }

        #endregion

        #region Utilities

        private static void PrepareTimeSlotModel(DeliveryTimeSlotModel model)
        {
            model.AvailableDaysOfWeek =
            [
                new() { Text = "Todos los días", Value = "-1" },
                new() { Text = "Lunes", Value = "1" },
                new() { Text = "Martes", Value = "2" },
                new() { Text = "Miércoles", Value = "3" },
                new() { Text = "Jueves", Value = "4" },
                new() { Text = "Viernes", Value = "5" },
                new() { Text = "Sábado", Value = "6" },
                new() { Text = "Domingo", Value = "0" }
            ];
        }

        #endregion
    }
}
