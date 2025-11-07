/**
 * Delivery Time Picker
 */
var DeliveryTimePicker = (function () {
    'use strict';

    // Configuration
    let config = {
        hasExitoProducts: false,
        cutoffHour: 13,
        baseUrl: '/DeliveryTimePublic'
    };

    // State
    let state = {
        availableSlots: [],
        selectedDate: null,
        selectedMinTime: null,
        selectedMaxTime: null,
        currentMonth: new Date(),
        reservationId: null
    };

    /**
     * Initialize the delivery time picker
     */
    function init(options) {
        if (!options) {
            console.error('DeliveryTimePicker: No configuration provided');
            return;
        }
        
        config = { ...config, ...options };
        
        console.log('DeliveryTimePicker: Initializing with config', config);
        console.log('DeliveryTimePicker: Elements check', {
            input: $('#deliveryDatePicker').length,
            calendar: $('#datePickerCalendar').length,
            timeSlots: $('#timeSlotOptions').length
        });
        
        // Setup event listeners
        setupEventListeners();
        
        // Load available slots, then restore saved data
        loadAvailableSlots(function() {
            // Restore saved data if available (after slots are loaded)
            if (config.savedDate && config.savedMinTime && config.savedMaxTime) {
                restoreSavedData();
            }
        });
    }

    /**
     * Setup event listeners
     */
    function setupEventListeners() {
        console.log('DeliveryTimePicker: Setting up event listeners');
        
        // Date picker click - Show calendar dropdown
        $('#deliveryDatePicker').on('click', function (e) {
            console.log('DeliveryTimePicker: Date input clicked');
            e.preventDefault();
            e.stopPropagation();
            
            const $calendar = $('#datePickerCalendar');
            const isVisible = $calendar.hasClass('show');
            
            if (!isVisible) {
                console.log('DeliveryTimePicker: Showing calendar');
                $calendar.addClass('show');
                renderCalendar();
            } else {
                console.log('DeliveryTimePicker: Hiding calendar');
                $calendar.removeClass('show');
            }
        });

        // Close calendar when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('.delivery-date-input, .calendar-wrapper').length) {
                $('#datePickerCalendar').removeClass('show');
            }
        });

        // Release reservation on page unload
        $(window).on('beforeunload', function () {
            if (state.reservationId) {
                releaseReservation(state.reservationId);
            }
        });
    }

    /**
     * Load available slots from server
     */
    function loadAvailableSlots(callback) {
        $.ajax({
            url: config.baseUrl + '/GetAvailableSlots',
            type: 'GET',
            data: {
                daysToShow: 10
            },
            success: function (response) {
                if (response.success || response.Success) {
                    state.availableSlots = response.data || response.Data || [];
                    console.log('Available slots loaded:', state.availableSlots);
                    
                    // Call callback if provided
                    if (callback && typeof callback === 'function') {
                        callback();
                    }
                } else {
                    console.error('Error loading slots:', response.message || response.Message);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading available slots:', error);
                // Call callback anyway to continue initialization
                if (callback && typeof callback === 'function') {
                    callback();
                }
            }
        });
    }

    /**
     * Restore saved data (date and time)
     */
    function restoreSavedData() {
        console.log('DeliveryTimePicker: Restoring saved data', {
            date: config.savedDate,
            minTime: config.savedMinTime,
            maxTime: config.savedMaxTime
        });
        
        // Parse the saved date - FIXED: Handle DD-MM-YYYY format
        var dateParts = config.savedDate.split('-');
        if (dateParts.length === 3) {
            var day = parseInt(dateParts[0], 10);
            var month = parseInt(dateParts[1], 10) - 1;
            var year = parseInt(dateParts[2], 10);
            
            // Validate parsed values
            if (isNaN(day) || isNaN(month) || isNaN(year)) {
                console.warn('DeliveryTimePicker: Invalid saved date format');
                return;
            }
            
            state.selectedDate = new Date(year, month, day);
            
            // Normalize saved times to ensure proper comparison
            state.selectedMinTime = normalizeTimeString(config.savedMinTime);
            state.selectedMaxTime = normalizeTimeString(config.savedMaxTime);
            
            // Update the date picker display - keep DD-MM-YYYY format
            $('#deliveryDatePicker').val(config.savedDate);
            
            // Update the hidden fields
            $('#selectedDeliveryDate').val(config.savedDate);
            $('#selectedMinTime').val(state.selectedMinTime);
            $('#selectedMaxTime').val(state.selectedMaxTime);
            
            // Load time slots for the selected date
            const dateStrYYYYMMDD = formatDate(state.selectedDate);
            const dateStrDDMMYYYY = formatDateDisplay(state.selectedDate);
            
            var selectedSlot = state.availableSlots.find(s => {
                const slotDate = s.dateFormatted || s.DateFormatted || '';
                return slotDate === dateStrYYYYMMDD || slotDate === dateStrDDMMYYYY;
            });
            
            if (selectedSlot) {
                // Render time slots with the saved selection
                renderTimeSlots();
                $('#timeSlotOptions').show();
                
                console.log('DeliveryTimePicker: Data restored successfully', {
                    normalizedMinTime: state.selectedMinTime,
                    normalizedMaxTime: state.selectedMaxTime
                });
            } else {
                console.warn('DeliveryTimePicker: Saved date not found in available slots');
                // Still try to show the date
                $('#timeSlotOptions').html('<p style="text-align: center; color: #dc2626; padding: 20px;">No hay horarios disponibles para esta fecha guardada</p>');
                $('#timeSlotOptions').show();
            }
        }
    }
    
    /**
     * Normalize time string to HH:MM:SS format
     */
    function normalizeTimeString(timeStr) {
        if (!timeStr) return '';
        
        // If already in correct format, return as is
        if (/^\d{2}:\d{2}:\d{2}$/.test(timeStr)) {
            return timeStr;
        }
        
        // If in HH:MM format, add seconds
        if (/^\d{2}:\d{2}$/.test(timeStr)) {
            return timeStr + ':00';
        }
        
        // Try to parse and reformat
        const parts = timeStr.split(':');
        if (parts.length >= 2) {
            const hours = String(parseInt(parts[0]) || 0).padStart(2, '0');
            const minutes = String(parseInt(parts[1]) || 0).padStart(2, '0');
            const seconds = parts.length > 2 ? String(parseInt(parts[2]) || 0).padStart(2, '0') : '00';
            return hours + ':' + minutes + ':' + seconds;
        }
        
        return timeStr;
    }

    /**
     * Render calendar
     */
    function renderCalendar() {
        const month = state.currentMonth.getMonth();
        const year = state.currentMonth.getFullYear();
        
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);
        const prevLastDay = new Date(year, month, 0);
        
        const monthNames = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
                           'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
        const dayNames = ['D', 'L', 'M', 'M', 'J', 'V', 'S'];
        
        let html = '<div class="calendar-header">';
        html += '<button type="button" class="calendar-nav" data-nav="prev">&lt;</button>';
        html += '<span class="calendar-month-year">' + monthNames[month] + ' ' + year + '</span>';
        html += '<button type="button" class="calendar-nav" data-nav="next">&gt;</button>';
        html += '</div>';
        
        html += '<div class="calendar-grid">';
        
        // Day headers
        dayNames.forEach(day => {
            html += '<div class="calendar-day-header">' + day + '</div>';
        });
        
        // Previous month days
        const firstDayOfWeek = firstDay.getDay();
        for (let i = firstDayOfWeek - 1; i >= 0; i--) {
            const day = prevLastDay.getDate() - i;
            html += '<div class="calendar-day other-month">' + day + '</div>';
        }
        
        // Current month days
        for (let day = 1; day <= lastDay.getDate(); day++) {
            const date = new Date(year, month, day);
            const dateStrYYYYMMDD = formatDate(date);
            
            // Try to find slot with both formats
            const slotData = state.availableSlots.find(s => {
                const slotDate = s.dateFormatted || s.DateFormatted || '';
                return slotDate === dateStrYYYYMMDD;
            });
            
            let classes = 'calendar-day';
            
            // Check if today
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            const compareDate = new Date(date);
            compareDate.setHours(0, 0, 0, 0);
            
            if (compareDate.getTime() === today.getTime()) {
                classes += ' today';
            }
            
            // Check if selected
            if (state.selectedDate) {
                const selectedCompare = new Date(state.selectedDate);
                selectedCompare.setHours(0, 0, 0, 0);
                if (compareDate.getTime() === selectedCompare.getTime()) {
                    classes += ' selected';
                }
            }
            
            // Check if available
            const isAvailable = slotData ? (slotData.isAvailable ?? slotData.IsAvailable ?? false) : false;
            if (!slotData || !isAvailable) {
                classes += ' disabled';
            }
            
            html += '<div class="' + classes + '" data-date="' + dateStrYYYYMMDD + '">' + day + '</div>';
        }
        
        // Next month days
        const lastDayOfWeek = lastDay.getDay();
        for (let i = 1; i < 7 - lastDayOfWeek; i++) {
            html += '<div class="calendar-day other-month">' + i + '</div>';
        }
        
        html += '</div>';
        html += '<button type="button" class="calendar-accept-btn">Aceptar</button>';
        
        $('#datePickerCalendar').html(html);
        
        // Attach event listeners
        $('.calendar-nav').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            const nav = $(this).data('nav');
            if (nav === 'prev') {
                state.currentMonth = new Date(year, month - 1, 1);
            } else {
                state.currentMonth = new Date(year, month + 1, 1);
            }
            renderCalendar();
        });
        
        $('.calendar-day:not(.disabled):not(.other-month)').on('click', function (e) {
            e.stopPropagation();
            $('.calendar-day').removeClass('selected');
            $(this).addClass('selected');
            const dateStr = $(this).data('date');
            state.selectedDate = parseDate(dateStr);
        });
        
        $('.calendar-accept-btn').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            
            if (state.selectedDate) {
                // Update display with DD-MM-YYYY format
                const displayDate = formatDateDisplay(state.selectedDate);
                $('#deliveryDatePicker').val(displayDate);
                $('#selectedDeliveryDate').val(displayDate);
                
                $('#datePickerCalendar').removeClass('show');
                
                // Clear time selection
                state.selectedMinTime = null;
                state.selectedMaxTime = null;
                
                // Clear hidden fields
                $('#selectedMinTime').val('');
                $('#selectedMaxTime').val('');
                
                // Show time slots immediately
                renderTimeSlots();
                $('#timeSlotOptions').show();
                
                console.log('Date selected:', displayDate);
            }
        });
    }

    /**
     * Render time slots for selected date
     */
    function renderTimeSlots() {
        if (!state.selectedDate) return;
        
        const dateStrYYYYMMDD = formatDate(state.selectedDate);
        
        const slotData = state.availableSlots.find(s => {
            const slotDate = s.dateFormatted || s.DateFormatted || '';
            return slotDate === dateStrYYYYMMDD;
        });
        
        const timeSlots = slotData ? (slotData.timeSlots || slotData.TimeSlots || []) : [];
        
        if (!slotData || timeSlots.length === 0) {
            $('#timeSlotOptions').html('<p style="text-align: center; color: #dc2626; padding: 20px;">No hay horarios disponibles para esta fecha</p>');
            return;
        }
        
        let html = '';
        let hasSelectedSlot = false;
        
        timeSlots.forEach(slot => {
            const isAvailable = slot.isAvailable ?? slot.IsAvailable ?? false;
            const minTimeRaw = slot.minTime || slot.MinTime;
            const maxTimeRaw = slot.maxTime || slot.MaxTime;
            const displayText = slot.displayText || slot.DisplayText;
            const slotId = slot.slotId || slot.SlotId;
            const availableCapacity = slot.availableCapacity ?? slot.AvailableCapacity ?? 0;
            
            // Normalize times for comparison
            const minTime = normalizeTimeString(formatTimeSpan(minTimeRaw));
            const maxTime = normalizeTimeString(formatTimeSpan(maxTimeRaw));
            
            let classes = 'time-slot-option';
            if (!isAvailable) {
                classes += ' disabled';
            }
            
            // Compare normalized times
            const isSelected = state.selectedMinTime && state.selectedMaxTime &&
                             normalizeTimeString(state.selectedMinTime) === minTime && 
                             normalizeTimeString(state.selectedMaxTime) === maxTime;
            
            if (isSelected) {
                classes += ' selected';
                hasSelectedSlot = true;
            }
            
            html += '<div class="' + classes + '" ';
            html += 'data-min-time="' + minTime + '" ';
            html += 'data-max-time="' + maxTime + '" ';
            html += 'data-slot-id="' + (slotId || '') + '">';
            html += displayText;
            if (!isAvailable) {
                html += '<div class="capacity-info">No disponible</div>';
            } else {
                html += '<div class="capacity-info">' + availableCapacity + ' disponibles</div>';
            }
            html += '</div>';
        });
        
        $('#timeSlotOptions').html(html);
        
        // Log for debugging
        if (state.selectedMinTime && state.selectedMaxTime) {
            console.log('Rendering time slots with saved selection:', {
                savedMin: state.selectedMinTime,
                savedMax: state.selectedMaxTime,
                hasSelectedSlot: hasSelectedSlot
            });
        }
        
        // Attach click handlers
        $('.time-slot-option:not(.disabled)').on('click', function () {
            $('.time-slot-option').removeClass('selected');
            $(this).addClass('selected');
            
            const minTime = $(this).data('min-time');
            const maxTime = $(this).data('max-time');
            const slotId = $(this).data('slot-id');
            
            state.selectedMinTime = minTime;
            state.selectedMaxTime = maxTime;
            
            // Update hidden fields
            $('#selectedMinTime').val(minTime);
            $('#selectedMaxTime').val(maxTime);
            
            console.log('Selected time slot:', { minTime, maxTime, slotId });
            
            // Reserve the slot
            reserveSlot(state.selectedDate, minTime, maxTime, slotId);
        });
    }

    /**
     * Reserve a time slot
     */
    function reserveSlot(date, minTime, maxTime, slotId) {
        if (state.reservationId) {
            releaseReservation(state.reservationId);
        }
        
        const minTimeStr = formatTimeSpan(minTime);
        const maxTimeStr = formatTimeSpan(maxTime);
        
        // Send date in DD-MM-YYYY format to match server expectations
        const data = {
            deliveryDate: formatDateDisplay(date),
            minDeliveryTime: minTimeStr,
            maxDeliveryTime: maxTimeStr,
            timeSlotId: slotId || null,
            isTemporary: true
        };
        
        console.log('Reserving slot with data:', data);
        
        $.ajax({
            url: config.baseUrl + '/ReserveSlot',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function (response) {
                if (response.success || response.Success) {
                    const resId = response.reservationId || response.ReservationId;
                    state.reservationId = resId;
                    $('#selectedReservationId').val(resId);
                    console.log('Slot reserved:', resId);
                } else {
                    const errorMsg = response.message || response.Message || 'Error desconocido al reservar el horario';
                    alert(errorMsg);
                    console.error('Failed to reserve slot:', response);
                    loadAvailableSlots();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error reserving slot:', error);
                let errorMessage = 'Error al reservar el horario. Por favor, intente nuevamente.';
                
                if (xhr.responseJSON) {
                    errorMessage = xhr.responseJSON.message || xhr.responseJSON.Message || errorMessage;
                } else if (xhr.responseText) {
                    try {
                        const parsed = JSON.parse(xhr.responseText);
                        errorMessage = parsed.message || parsed.Message || errorMessage;
                    } catch (e) {
                        // Keep default message
                    }
                }
                
                alert(errorMessage);
            }
        });
    }

    /**
     * Release a reservation
     */
    function releaseReservation(reservationId) {
        $.ajax({
            url: config.baseUrl + '/ReleaseReservation',
            type: 'POST',
            data: { reservationId: reservationId },
            async: false,
            success: function (response) {
                console.log('Reservation released');
                state.reservationId = null;
            },
            error: function (xhr, status, error) {
                console.error('Error releasing reservation:', error);
            }
        });
    }

    /**
     * Format date as YYYY-MM-DD (for internal use and server queries)
     */
    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    }

    /**
     * Format date for display (DD-MM-YYYY)
     */
    function formatDateDisplay(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return day + '-' + month + '-' + year;
    }

    /**
     * Format TimeSpan to HH:MM:SS string
     */
    function formatTimeSpan(timeSpan) {
        if (!timeSpan) return '';
        
        if (typeof timeSpan === 'string') {
            return timeSpan;
        }
        
        if (typeof timeSpan === 'object') {
            const hours = String(timeSpan.hours || timeSpan.Hours || 0).padStart(2, '0');
            const minutes = String(timeSpan.minutes || timeSpan.Minutes || 0).padStart(2, '0');
            const seconds = String(timeSpan.seconds || timeSpan.Seconds || 0).padStart(2, '0');
            return hours + ':' + minutes + ':' + seconds;
        }
        
        return String(timeSpan);
    }

    /**
     * Parse date from YYYY-MM-DD string
     */
    function parseDate(dateStr) {
        const parts = dateStr.split('-');
        return new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
    }

    // Public API
    return {
        init: init
    };
})();