using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Services
{
    /// <summary>
    /// Delivery time service interface
    /// </summary>
    public interface IDeliveryTimeService
    {
        #region Time Slots

        /// <summary>
        /// Gets a delivery time slot by day of week and time range
        /// </summary>
        Task<DeliveryTimeSlot> GetTimeSlotByHoursAndDayAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime);

        /// <summary>
        /// Gets all delivery time slots
        /// </summary>
        Task<IList<DeliveryTimeSlot>> GetAllTimeSlotsAsync();

        /// <summary>
        /// Gets a delivery time slot by ID
        /// </summary>
        Task<DeliveryTimeSlot> GetTimeSlotByIdAsync(int id);

        /// <summary>
        /// Inserts a delivery time slot
        /// </summary>
        Task InsertTimeSlotAsync(DeliveryTimeSlot timeSlot);

        /// <summary>
        /// Updates a delivery time slot
        /// </summary>
        Task UpdateTimeSlotAsync(DeliveryTimeSlot timeSlot);

        /// <summary>
        /// Deletes a delivery time slot
        /// </summary>
        Task DeleteTimeSlotAsync(DeliveryTimeSlot timeSlot);

        /// <summary>
        /// Gets time slots for a specific day of week
        /// </summary>
        Task<IList<DeliveryTimeSlot>> GetTimeSlotsForDayAsync(int dayOfWeek);

        #endregion

        #region Reservations

        /// <summary>
        /// Gets a delivery time reservation by ID
        /// </summary>
        Task<DeliveryTimeReservation> GetReservationByIdAsync(int id);

        /// <summary>
        /// Gets a delivery time reservation by order ID
        /// </summary>
        Task<DeliveryTimeReservation> GetReservationByOrderIdAsync(int orderId);

        /// <summary>
        /// Inserts a delivery time reservation
        /// </summary>
        Task<DeliveryTimeReservation> InsertReservationAsync(DeliveryTimeReservation reservation);

        /// <summary>
        /// Updates a delivery time reservation
        /// </summary>
        Task UpdateReservationAsync(DeliveryTimeReservation reservation);

        /// <summary>
        /// Deletes a delivery time reservation
        /// </summary>
        Task DeleteReservationAsync(DeliveryTimeReservation reservation);

        /// <summary>
        /// Gets the number of reservations for a specific date and time range
        /// </summary>
        Task<int> GetReservationCountAsync(DateTime date, TimeSpan minTime, TimeSpan maxTime, bool confirmedOnly = true);

        /// <summary>
        /// Confirms a temporary reservation
        /// </summary>
        Task ConfirmReservationAsync(int reservationId, int orderId);

        /// <summary>
        /// Releases expired temporary reservations
        /// </summary>
        Task ReleaseExpiredReservationsAsync();

        /// <summary>
        /// Gets available capacity for a specific date and time range
        /// </summary>
        Task<int> GetAvailableCapacityAsync(DateTime date, TimeSpan minTime, TimeSpan maxTime, int maxCapacity);

        #endregion

        #region Holidays

        /// <summary>
        /// Gets all holidays
        /// </summary>
        Task<IList<Holiday>> GetAllHolidaysAsync();

        /// <summary>
        /// Gets a holiday by ID
        /// </summary>
        Task<Holiday> GetHolidayByIdAsync(int id);

        /// <summary>
        /// Inserts a holiday
        /// </summary>
        Task InsertHolidayAsync(Holiday holiday);

        /// <summary>
        /// Updates a holiday
        /// </summary>
        Task UpdateHolidayAsync(Holiday holiday);

        /// <summary>
        /// Deletes a holiday
        /// </summary>
        Task DeleteHolidayAsync(Holiday holiday);

        /// <summary>
        /// Checks if a date is a holiday
        /// </summary>
        Task<bool> IsHolidayAsync(DateTime date);

        /// <summary>
        /// Imports holidays from external service
        /// </summary>
        Task ImportHolidaysAsync(string countryCode, int year);

        #endregion

        #region Validation

        /// <summary>
        /// Validates if a date is available for delivery
        /// </summary>
        Task<(bool IsValid, string Reason)> ValidateDeliveryDateAsync(DateTime date, bool hasExitoProducts);

        /// <summary>
        /// Gets the next available delivery date
        /// </summary>
        Task<DateTime> GetNextAvailableDeliveryDateAsync(bool hasExitoProducts);

        /// <summary>
        /// Checks if the cart contains Éxito products
        /// </summary>
        Task<bool> CartHasExitoProductsAsync(int customerId);

        #endregion
    }
}
