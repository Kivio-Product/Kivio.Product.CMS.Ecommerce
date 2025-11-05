using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;
using Nop.Plugin.Misc.DeliveryTimePicker.Models;
using Nop.Plugin.Misc.DeliveryTimePicker.Services;
using Nop.Plugin.Misc.DeliveryTimePicker.Services.Rules;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Controllers
{
    public class DeliveryTimePublicController(
        IDeliveryTimeService deliveryTimeService,
        IDeliveryRuleService deliveryRuleService,
        IWorkContext workContext,
        IStoreContext storeContext,
        IGenericAttributeService genericAttributeService,
        IShoppingCartService shoppingCartService,
        IOrderProcessingService orderProcessingService,
        DeliveryTimePickerSettings settings) : BasePluginController
    {
        #region Fields

        private readonly IDeliveryTimeService _deliveryTimeService = deliveryTimeService;
        private readonly IDeliveryRuleService _deliveryRuleService = deliveryRuleService;
        private readonly IWorkContext _workContext = workContext;
        private readonly IStoreContext _storeContext = storeContext;
        private readonly IGenericAttributeService _genericAttributeService = genericAttributeService;
        private readonly IShoppingCartService _shoppingCartService = shoppingCartService;
        private readonly IOrderProcessingService _orderProcessingService = orderProcessingService;
        private readonly DeliveryTimePickerSettings _settings = settings;

        #endregion
        #region Ctor

        #endregion

        #region Methods

        /// <summary>
        /// Gets available delivery dates and time slots
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(DateTime? startDate, int daysToShow = 30)
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();

                // Release expired reservations before checking availability
                await _deliveryTimeService.ReleaseExpiredReservationsAsync();

                // Build rule context for the customer
                var context = await _deliveryRuleService.BuildContextAsync(customer.Id);

                var start = startDate ?? context.Now.Date;
                var result = new List<AvailableDeliverySlotModel>();

                for (int i = 0; i < daysToShow; i++)
                {
                    var date = start.AddDays(i);
                    
                    // Skip weekends if configured
                    if (_settings.DisableWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                        continue;

                    var daySlots = new AvailableDeliverySlotModel
                    {
                        Date = date,
                        DateFormatted = date.ToString("yyyy-MM-dd"),
                        IsAvailable = true,
                        UnavailableReason = null
                    };

                    // Get time slots for this day
                    var timeSlots = await _deliveryTimeService.GetTimeSlotsForDayAsync((int)date.DayOfWeek);

                    foreach (var slot in timeSlots)
                    {
                        // Create a delivery slot for rule evaluation
                        var deliverySlot = new DeliverySlot
                        {
                            Date = date,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            SlotId = slot.Id,
                            DayOfWeek = (int)date.DayOfWeek
                        };

                        // Evaluate the slot using the rule engine
                        var ruleResult = await _deliveryRuleService.EvaluateSlotAsync(context, deliverySlot);

                        // Check capacity
                        var maxCapacity = slot.MaxCapacity ?? _settings.MaxCapacityPerSlot;
                        var availableCapacity = await _deliveryTimeService.GetAvailableCapacityAsync(
                            date, slot.StartTime, slot.EndTime, maxCapacity);

                        // Only include slot if rules allow it
                        if (ruleResult.IsAllowed)
                        {
                            daySlots.TimeSlots.Add(new TimeSlotOption
                            {
                                SlotId = slot.Id,
                                MinTime = slot.StartTime,
                                MaxTime = slot.EndTime,
                                DisplayText = $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}",
                                IsAvailable = availableCapacity > 0,
                                AvailableCapacity = availableCapacity,
                                MaxCapacity = maxCapacity
                            });
                        }
                    }

                    // Mark day as unavailable if no slots are available
                    if (daySlots.TimeSlots.Count == 0)
                    {
                        daySlots.IsAvailable = false;
                        daySlots.UnavailableReason = "No hay franjas horarias disponibles para este día.";
                    }

                    result.Add(daySlots);
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Reserves a delivery time slot temporarily
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ReserveSlot([FromBody] ReserveDeliveryTimeRequest request)
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();

                // Build rule context for the customer
                var context = await _deliveryRuleService.BuildContextAsync(customer.Id);

                // Get the time slot to reserve
                DeliveryTimeSlot slot = null;
                if (request.TimeSlotId.HasValue)
                {
                    slot = await _deliveryTimeService.GetTimeSlotByIdAsync(request.TimeSlotId.Value);
                }

                // Create a delivery slot for rule evaluation
                var deliverySlot = new DeliverySlot
                {
                    Date = request.DeliveryDate,
                    StartTime = request.MinDeliveryTime,
                    EndTime = request.MaxDeliveryTime,
                    SlotId = request.TimeSlotId ?? 0,
                    DayOfWeek = (int)request.DeliveryDate.DayOfWeek
                };

                // Evaluate the slot using the rule engine
                var ruleResult = await _deliveryRuleService.EvaluateSlotAsync(context, deliverySlot);
                if (!ruleResult.IsAllowed)
                {
                    return Json(new ReserveDeliveryTimeResponse
                    {
                        Success = false,
                        Message = ruleResult.Message ?? "Este horario no está disponible."
                    });
                }

                // Check capacity
                var maxCapacity = _settings.MaxCapacityPerSlot;
                if (slot?.MaxCapacity.HasValue == true)
                    maxCapacity = slot.MaxCapacity.Value;

                var availableCapacity = await _deliveryTimeService.GetAvailableCapacityAsync(
                    request.DeliveryDate, request.MinDeliveryTime, request.MaxDeliveryTime, maxCapacity);

                if (availableCapacity <= 0)
                {
                    return Json(new ReserveDeliveryTimeResponse
                    {
                        Success = false,
                        Message = "Este horario ya no está disponible. Por favor, seleccione otro."
                    });
                }

                // Check if customer already has a temporary reservation and release it
                var existingReservations = await _deliveryTimeService.GetAllTimeSlotsAsync();
                // TODO: Implement method to get customer's temporary reservations and release them

                // Determine if cart has EXITO products for reservation tracking
                var hasExitoProducts = context.SuppliersInCart.Contains("EXITO");

                // Create reservation
                var reservation = new DeliveryTimeReservation
                {
                    CustomerId = customer.Id,
                    DeliveryDate = request.DeliveryDate,
                    MinDeliveryTime = request.MinDeliveryTime,
                    MaxDeliveryTime = request.MaxDeliveryTime,
                    TimeSlotId = request.TimeSlotId,
                    IsConfirmed = false,
                    HasExitoProducts = hasExitoProducts
                };

                var insertedReservation = await _deliveryTimeService.InsertReservationAsync(reservation);

                return Json(new ReserveDeliveryTimeResponse
                {
                    Success = true,
                    Message = "Horario reservado temporalmente",
                    ReservationId = insertedReservation.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new ReserveDeliveryTimeResponse
                {
                    Success = false,
                    Message = "Error al reservar el horario: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Confirms a reservation (called when order is placed)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmReservation(int reservationId, int orderId)
        {
            try
            {
                await _deliveryTimeService.ConfirmReservationAsync(reservationId, orderId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Releases a temporary reservation (called when user leaves checkout)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ReleaseReservation(int reservationId)
        {
            try
            {
                var reservation = await _deliveryTimeService.GetReservationByIdAsync(reservationId);
                if (reservation != null && !reservation.IsConfirmed)
                {
                    await _deliveryTimeService.DeleteReservationAsync(reservation);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Saves delivery time selection in One Page Checkout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OpcSaveDeliveryTime(DeliveryTimeCheckoutModel model)
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();

                // Validate cart is not empty
                var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (!cart.Any())
                {
                    return Json(new
                    {
                        error = true,
                        message = "El carrito está vacío."
                    });
                }

                // Validate the model
                if (string.IsNullOrEmpty(model.DeliveryDate) || 
                    string.IsNullOrEmpty(model.MinDeliveryTime) || 
                    string.IsNullOrEmpty(model.MaxDeliveryTime))
                {
                    return Json(new
                    {
                        error = true,
                        message = "Por favor seleccione una fecha y hora de entrega"
                    });
                }

                // Parse the delivery date
                if (!DateTime.TryParse(model.DeliveryDate, out DateTime deliveryDate))
                {
                    return Json(new
                    {
                        error = true,
                        message = "Fecha de entrega no válida"
                    });
                }

                // Parse the times
                if (!TimeSpan.TryParse(model.MinDeliveryTime, out TimeSpan minTime) ||
                    !TimeSpan.TryParse(model.MaxDeliveryTime, out TimeSpan maxTime))
                {
                    return Json(new
                    {
                        error = true,
                        message = "Hora de entrega no válida"
                    });
                }

                // Build rule context for validation
                var context = await _deliveryRuleService.BuildContextAsync(customer.Id);

                // Create a delivery slot for rule evaluation
                var deliverySlot = new DeliverySlot
                {
                    Date = deliveryDate,
                    StartTime = minTime,
                    EndTime = maxTime,
                    SlotId = 0,
                    DayOfWeek = (int)deliveryDate.DayOfWeek
                };

                // Evaluate the slot using the rule engine
                var ruleResult = await _deliveryRuleService.EvaluateSlotAsync(context, deliverySlot);
                if (!ruleResult.IsAllowed)
                {
                    return Json(new
                    {
                        error = true,
                        message = ruleResult.Message ?? "Este horario no está disponible."
                    });
                }

                // Persist data in customer's generic attributes
                await _genericAttributeService.SaveAttributeAsync(customer, "Delivery.Date", model.DeliveryDate, store.Id);
                await _genericAttributeService.SaveAttributeAsync(customer, "Delivery.MinTime", model.MinDeliveryTime, store.Id);
                await _genericAttributeService.SaveAttributeAsync(customer, "Delivery.MaxTime", model.MaxDeliveryTime, store.Id);
                
                if (model.ReservationId.HasValue)
                {
                    await _genericAttributeService.SaveAttributeAsync(customer, "Delivery.ReservationId", model.ReservationId.Value.ToString(), store.Id);
                }

                // Check if payment workflow is required
                var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart, false);
                
                if (!isPaymentWorkflowRequired)
                {
                    // Payment is not required, go directly to confirm order
                    return Json(new
                    {
                        update_section = new
                        {
                            name = "confirm-order",
                            html = "" // Will be loaded by the framework
                        },
                        goto_section = "confirm_order"
                    });
                }

                // Payment is required, go to payment method step
                return Json(new
                {
                    update_section = new
                    {
                        name = "payment-method",
                        html = "" // Will be loaded by the framework
                    },
                    goto_section = "payment_method"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    message = "Error al guardar la hora de entrega: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Loads the delivery time picker view for One Page Checkout
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OpcLoadDeliveryTime()
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                
                // Get suppliers from cart using rule service
                var suppliers = await _deliveryRuleService.GetSuppliersFromCartAsync(customer.Id);
                var hasExitoProducts = suppliers.Contains("EXITO");

                // Retrieve previously saved data if user is returning to this step
                var savedDate = await _genericAttributeService.GetAttributeAsync<string>(customer, "Delivery.Date", store.Id);
                var savedMinTime = await _genericAttributeService.GetAttributeAsync<string>(customer, "Delivery.MinTime", store.Id);
                var savedMaxTime = await _genericAttributeService.GetAttributeAsync<string>(customer, "Delivery.MaxTime", store.Id);

                var model = new DeliveryTimePickerPublicModel
                {
                    HasExitoProducts = hasExitoProducts,
                    CutoffHour = _settings.CutoffHour,
                    DisableWeekends = _settings.DisableWeekends,
                    SavedDate = savedDate,
                    SavedMinTime = savedMinTime,
                    SavedMaxTime = savedMaxTime
                };

                return View("~/Plugins/Misc.DeliveryTimePicker/Views/OpcDeliveryTime.cshtml", model);
            }
            catch (Exception ex)
            {
                return Content("Error loading delivery time picker: " + ex.Message);
            }
        }

        #endregion
    }
}
