using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nager.Date;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Services
{
    /// <summary>
    /// Delivery time service implementation
    /// </summary>
    public class DeliveryTimeService : IDeliveryTimeService
    {
        #region Fields

        private readonly IRepository<DeliveryTimeSlot> _timeSlotRepository;
        private readonly IRepository<DeliveryTimeReservation> _reservationRepository;
        private readonly IRepository<Holiday> _holidayRepository;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly DeliveryTimePickerSettings _settings;

        #endregion

        #region Ctor

        public DeliveryTimeService(
            IRepository<DeliveryTimeSlot> timeSlotRepository,
            IRepository<DeliveryTimeReservation> reservationRepository,
            IRepository<Holiday> holidayRepository,
            ISettingService settingService,
            IShoppingCartService shoppingCartService,
            IProductService productService,
            IWorkContext workContext,
            DeliveryTimePickerSettings settings)
        {
            _timeSlotRepository = timeSlotRepository;
            _reservationRepository = reservationRepository;
            _holidayRepository = holidayRepository;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _productService = productService;
            _workContext = workContext;
            _settings = settings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the current time in the configured timezone
        /// </summary>
        private DateTime GetCurrentTimeInTimeZone()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }

        /// <summary>
        /// Checks if a date is a weekend
        /// </summary>
        private bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        #endregion

        #region Time Slots

        // function to get a time slot by hours and day
        public virtual async Task<DeliveryTimeSlot> GetTimeSlotByHoursAndDayAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            var slots = await _timeSlotRepository.GetAllAsync(query =>
            {
                return query.Where(x => x.DayOfWeek == dayOfWeek && x.StartTime == startTime && x.EndTime == endTime);
            });

            return slots.FirstOrDefault();
        }

        public virtual async Task<IList<DeliveryTimeSlot>> GetAllTimeSlotsAsync()
        {
            var slots = await _timeSlotRepository.GetAllAsync(query =>
            {
                return query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime);
            });

            return [.. slots];
        }

        public virtual async Task<DeliveryTimeSlot> GetTimeSlotByIdAsync(int id)
        {
            return await _timeSlotRepository.GetByIdAsync(id);
        }

        public virtual async Task InsertTimeSlotAsync(DeliveryTimeSlot timeSlot)
        {
            if (timeSlot == null)
                throw new ArgumentNullException(nameof(timeSlot));

            timeSlot.CreatedOnUtc = DateTime.UtcNow;
            timeSlot.UpdatedOnUtc = DateTime.UtcNow;

            await _timeSlotRepository.InsertAsync(timeSlot);
        }

        public virtual async Task UpdateTimeSlotAsync(DeliveryTimeSlot timeSlot)
        {
            if (timeSlot == null)
                throw new ArgumentNullException(nameof(timeSlot));

            timeSlot.UpdatedOnUtc = DateTime.UtcNow;

            await _timeSlotRepository.UpdateAsync(timeSlot);
        }

        public virtual async Task DeleteTimeSlotAsync(DeliveryTimeSlot timeSlot)
        {
            if (timeSlot == null)
                throw new ArgumentNullException(nameof(timeSlot));

            await _timeSlotRepository.DeleteAsync(timeSlot);
        }

        public virtual async Task<IList<DeliveryTimeSlot>> GetTimeSlotsForDayAsync(int dayOfWeek)
        {
            var slots = await _timeSlotRepository.GetAllAsync(query =>
            {
                return query
                    .Where(x => x.IsEnabled && (x.DayOfWeek == dayOfWeek || x.DayOfWeek == -1))
                    .OrderBy(x => x.StartTime);
            });

            return slots.ToList();
        }

        #endregion

        #region Reservations

        public virtual async Task<DeliveryTimeReservation> GetReservationByIdAsync(int id)
        {
            return await _reservationRepository.GetByIdAsync(id);
        }

        public virtual async Task<DeliveryTimeReservation> GetReservationByOrderIdAsync(int orderId)
        {
            var reservations = await _reservationRepository.GetAllAsync(query =>
            {
                return query.Where(x => x.OrderId == orderId);
            });

            return reservations.FirstOrDefault();
        }

        public virtual async Task<DeliveryTimeReservation> InsertReservationAsync(DeliveryTimeReservation reservation)
        {
            if (reservation == null)
                throw new ArgumentNullException(nameof(reservation));

            reservation.CreatedOnUtc = DateTime.UtcNow;

            // Set reservation timeout for temporary reservations
            if (!reservation.IsConfirmed && reservation.ReservedUntilUtc == null)
            {
                reservation.ReservedUntilUtc = DateTime.UtcNow.AddMinutes(_settings.ReservationTimeoutMinutes);
            }

            await _reservationRepository.InsertAsync(reservation);
            return reservation;
        }

        public virtual async Task UpdateReservationAsync(DeliveryTimeReservation reservation)
        {
            if (reservation == null)
                throw new ArgumentNullException(nameof(reservation));

            await _reservationRepository.UpdateAsync(reservation);
        }

        public virtual async Task DeleteReservationAsync(DeliveryTimeReservation reservation)
        {
            if (reservation == null)
                throw new ArgumentNullException(nameof(reservation));

            await _reservationRepository.DeleteAsync(reservation);
        }

        public virtual async Task<int> GetReservationCountAsync(DateTime date, TimeSpan minTime, TimeSpan maxTime, bool confirmedOnly = true)
        {
            var reservations = await _reservationRepository.GetAllAsync(query =>
            {
                var q = query.Where(x =>
                    x.DeliveryDate.Date == date.Date &&
                    x.MinDeliveryTime == minTime &&
                    x.MaxDeliveryTime == maxTime);

                if (confirmedOnly)
                {
                    q = q.Where(x => x.IsConfirmed);
                }
                else
                {
                    // Include temporary reservations that haven't expired
                    q = q.Where(x => x.IsConfirmed || 
                                    (x.ReservedUntilUtc.HasValue && x.ReservedUntilUtc.Value > DateTime.UtcNow));
                }

                return q;
            });

            return reservations.Count();
        }

        public virtual async Task ConfirmReservationAsync(int reservationId, int orderId)
        {
            var reservation = await GetReservationByIdAsync(reservationId);
            if (reservation == null)
                throw new ArgumentException($"Reservation with ID {reservationId} not found");

            reservation.IsConfirmed = true;
            reservation.OrderId = orderId;
            reservation.ReservedUntilUtc = null;

            await UpdateReservationAsync(reservation);
        }

        public virtual async Task ReleaseExpiredReservationsAsync()
        {
            var expiredReservations = await _reservationRepository.GetAllAsync(query =>
            {
                return query.Where(x =>
                    !x.IsConfirmed &&
                    x.ReservedUntilUtc.HasValue &&
                    x.ReservedUntilUtc.Value < DateTime.UtcNow);
            });

            foreach (var reservation in expiredReservations)
            {
                await DeleteReservationAsync(reservation);
            }
        }

        public virtual async Task<int> GetAvailableCapacityAsync(DateTime date, TimeSpan minTime, TimeSpan maxTime, int maxCapacity)
        {
            var reservedCount = await GetReservationCountAsync(date, minTime, maxTime, confirmedOnly: false);
            return Math.Max(0, maxCapacity - reservedCount);
        }

        #endregion

        #region Holidays

        public virtual async Task<IList<Holiday>> GetAllHolidaysAsync()
        {
            var holidays = await _holidayRepository.GetAllAsync(query =>
            {
                return query.Where(x => x.IsActive).OrderBy(x => x.Date);
            });

            return holidays.ToList();
        }

        public virtual async Task<Holiday> GetHolidayByIdAsync(int id)
        {
            return await _holidayRepository.GetByIdAsync(id);
        }

        public virtual async Task InsertHolidayAsync(Holiday holiday)
        {
            if (holiday == null)
                throw new ArgumentNullException(nameof(holiday));

            holiday.CreatedOnUtc = DateTime.UtcNow;

            await _holidayRepository.InsertAsync(holiday);
        }

        public virtual async Task UpdateHolidayAsync(Holiday holiday)
        {
            if (holiday == null)
                throw new ArgumentNullException(nameof(holiday));

            await _holidayRepository.UpdateAsync(holiday);
        }

        public virtual async Task DeleteHolidayAsync(Holiday holiday)
        {
            if (holiday == null)
                throw new ArgumentNullException(nameof(holiday));

            await _holidayRepository.DeleteAsync(holiday);
        }

        public virtual async Task<bool> IsHolidayAsync(DateTime date)
        {
            var holidays = await _holidayRepository.GetAllAsync(query =>
            {
                return query.Where(x => x.IsActive && x.Date.Date == date.Date);
            });

            if (holidays.Any())
                return true;

            // Check recurring holidays
            var recurringHolidays = await _holidayRepository.GetAllAsync(query =>
            {
                return query.Where(x =>
                    x.IsActive &&
                    x.IsRecurring &&
                    x.Date.Month == date.Month &&
                    x.Date.Day == date.Day);
            });

            return recurringHolidays.Any();
        }

        public virtual async Task ImportHolidaysAsync(string countryCode, int year)
        {
            try
            {
                // Parse country code to CountryCode enum
                if (!Enum.TryParse<CountryCode>(countryCode, out var country))
                    return;

                // Get public holidays from Nager.Date
                var publicHolidays = DateSystem.GetPublicHolidays(year, country);

                foreach (var holiday in publicHolidays)
                {
                    // Check if holiday already exists
                    var existingHolidays = await _holidayRepository.GetAllAsync(query =>
                    {
                        return query.Where(x =>
                            x.Date.Date == holiday.Date.Date &&
                            x.CountryCode == countryCode);
                    });

                    if (!existingHolidays.Any())
                    {
                        var newHoliday = new Holiday
                        {
                            Date = holiday.Date,
                            Name = holiday.LocalName ?? holiday.Name,
                            IsRecurring = false,
                            CountryCode = countryCode,
                            IsAutoImported = true,
                            IsActive = true
                        };

                        await InsertHolidayAsync(newHoliday);
                    }
                }
            }
            catch (Exception)
            {
                // Log error but don't throw
                // This is not critical functionality
            }
        }

        #endregion

        #region Validation

        public virtual async Task<(bool IsValid, string Reason)> ValidateDeliveryDateAsync(DateTime date, bool hasExitoProducts)
        {
            var currentTime = GetCurrentTimeInTimeZone();

            // Check if date is in the past
            if (date.Date < currentTime.Date)
                return (false, "La fecha seleccionada está en el pasado");

            // Check if it's a weekend and weekends are disabled
            if (_settings.DisableWeekends && IsWeekend(date))
                return (false, "Los fines de semana no están disponibles para entrega");

            // Check if it's a holiday
            if (await IsHolidayAsync(date))
                return (false, "La fecha seleccionada es un día festivo");

            // For NON-Éxito products: Maximum delivery is same day
            if (!hasExitoProducts)
            {
                if (date.Date > currentTime.Date)
                    return (false, "Para productos regulares, la entrega máxima es el mismo día");
            }
            // For Éxito products: Special rules
            else
            {
                bool isCurrentDayWeekday = currentTime.DayOfWeek >= DayOfWeek.Monday && 
                                          currentTime.DayOfWeek <= DayOfWeek.Friday;
                bool isFriday = currentTime.DayOfWeek == DayOfWeek.Friday;
                bool isMorning = currentTime.Hour < _settings.CutoffHour;
                bool isAfternoon = currentTime.Hour >= _settings.CutoffHour;

                // Same day delivery
                if (date.Date == currentTime.Date)
                {
                    // Monday to Friday in the morning: can choose afternoon of same day
                    if (isCurrentDayWeekday && isMorning)
                        return (true, null);

                    // Monday to Thursday afternoon: must choose next business day
                    if (isCurrentDayWeekday && !isFriday && isAfternoon)
                        return (false, "En la tarde de lunes a jueves, debe elegir un día posterior");

                    // Friday afternoon: can choose Friday afternoon
                    if (isFriday && isAfternoon)
                        return (true, null);
                }
                // Future dates
                else if (date.Date > currentTime.Date)
                {
                    // Monday to Thursday afternoon: must choose from next business day onwards
                    if (isCurrentDayWeekday && !isFriday && isAfternoon)
                    {
                        // Date must be at least next day
                        if (date.Date < currentTime.Date.AddDays(1))
                            return (false, "Debe elegir a partir del siguiente día hábil");
                    }
                }
            }

            return (true, null);
        }

        public virtual async Task<DateTime> GetNextAvailableDeliveryDateAsync(bool hasExitoProducts)
        {
            var currentTime = GetCurrentTimeInTimeZone();
            var candidateDate = currentTime.Date;

            if (hasExitoProducts)
            {
                // If before cutoff, same day might be available
                if (currentTime.Hour < _settings.CutoffHour)
                {
                    var (isValid, _) = await ValidateDeliveryDateAsync(candidateDate, hasExitoProducts);
                    if (isValid)
                        return candidateDate;
                }

                // Otherwise, start from tomorrow
                candidateDate = candidateDate.AddDays(1);
            }
            else
            {
                // For non-Éxito products, can start from tomorrow
                candidateDate = candidateDate.AddDays(1);
            }

            // Find next available date
            int maxDaysToCheck = 30; // Prevent infinite loop
            for (int i = 0; i < maxDaysToCheck; i++)
            {
                var (isValid, _) = await ValidateDeliveryDateAsync(candidateDate, hasExitoProducts);
                if (isValid)
                    return candidateDate;

                candidateDate = candidateDate.AddDays(1);
            }

            // Fallback: return tomorrow
            return currentTime.Date.AddDays(1);
        }

        public virtual async Task<bool> CartHasExitoProductsAsync(int customerId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer.Id != customerId && customerId > 0)
            {
                // This is a simplified version - you might need to get customer by ID
                // For now, we'll work with current customer
            }

            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart);

            foreach (var item in cart)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product != null && !string.IsNullOrEmpty(product.Sku) &&
                    product.Sku.Contains(_settings.ExitoProductSkuPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
