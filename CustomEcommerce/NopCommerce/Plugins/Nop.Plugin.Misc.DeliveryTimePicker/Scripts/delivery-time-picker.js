/**
 * Delivery Time Picker - Main JavaScript
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
        
        // Setup event listeners
        setupEventListeners();
        
        // Load available slots
        loadAvailableSlots();
        
        // Restore saved data if available
        if (config.savedDate && config.savedMinTime && config.savedMaxTime) {
            console.log('DeliveryTimePicker: Restoring saved data', {
                date: config.savedDate,
                minTime: config.savedMinTime,
                maxTime: config.savedMaxTime
            });
            
            // Parse and set the saved date
            var dateParts = config.savedDate.split('-');
            if (dateParts.length === 3) {
                var day = parseInt(dateParts[0], 10);
                var month = parseInt(dateParts[1], 10) - 1; // JavaScript months are 0-indexed
                var year = parseInt(dateParts[2], 10);
                state.selectedDate = new Date(year, month, day);
                
                // Update the date picker display
                $('#deliveryDatePicker').val(config.savedDate);
                
                // Set the times
                state.selectedMinTime = config.savedMinTime;
                state.selectedMaxTime = config.savedMaxTime;
                
                // Update the dropdowns
                $('#minDeliveryTime').val(config.savedMinTime);
                $('#maxDeliveryTime').val(config.savedMaxTime);
                
                // Enable the time selects
                $('#minDeliveryTime, #maxDeliveryTime').prop('disabled', false);
                
                console.log('DeliveryTimePicker: Data restored successfully');
            }
        }
    }

    /**
     * Setup event listeners
     */
    function setupEventListeners() {
        // Date picker click
        $('#deliveryDatePicker').on('click', function () {
            $('#datePickerCalendar').toggle();
            if ($('#datePickerCalendar').is(':visible')) {
                renderCalendar();
            }
        });

        // Time selects
        $('#minDeliveryTime, #maxDeliveryTime').on('click', function () {
            if (state.selectedDate) {
                $('#timeSlotOptions').show();
                renderTimeSlots();
            } else {
                alert('Por favor, seleccione primero una fecha de entrega');
            }
        });

        // Close calendar when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('.delivery-date-input, .calendar-wrapper').length) {
                $('#datePickerCalendar').hide();
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
    function loadAvailableSlots() {
        $.ajax({
            url: config.baseUrl + '/GetAvailableSlots',
            type: 'GET',
            data: {
                daysToShow: 60
            },
            success: function (response) {
                if (response.success) {
                    state.availableSlots = response.data;
                    console.log('Available slots loaded:', state.availableSlots);
                } else {
                    console.error('Error loading slots:', response.message);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading available slots:', error);
            }
        });
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
        html += '<button class="calendar-nav" data-nav="prev">&lt;</button>';
        html += '<span>' + monthNames[month] + ' ' + year + '</span>';
        html += '<button class="calendar-nav" data-nav="next">&gt;</button>';
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
            const dateStr = formatDate(date);
            // Handle both camelCase and PascalCase property names
            const slotData = state.availableSlots.find(s => 
                (s.dateFormatted && s.dateFormatted === dateStr) || 
                (s.DateFormatted && s.DateFormatted === dateStr)
            );
            
            let classes = 'calendar-day';
            
            // Check if today
            const today = new Date();
            if (date.toDateString() === today.toDateString()) {
                classes += ' today';
            }
            
            // Check if selected
            if (state.selectedDate && formatDate(state.selectedDate) === dateStr) {
                classes += ' selected';
            }
            
            // Check if available - Handle both camelCase and PascalCase properties
            const isAvailable = slotData ? (slotData.isAvailable || slotData.IsAvailable) : false;
            if (!slotData || !isAvailable) {
                classes += ' disabled';
            }
            
            html += '<div class="' + classes + '" data-date="' + dateStr + '">' + day + '</div>';
        }
        
        // Next month days
        const lastDayOfWeek = lastDay.getDay();
        for (let i = 1; i < 7 - lastDayOfWeek; i++) {
            html += '<div class="calendar-day other-month">' + i + '</div>';
        }
        
        html += '</div>';
        html += '<button class="calendar-accept-btn">Aceptar</button>';
        
        $('#datePickerCalendar').html(html);
        
        // Attach event listeners
        $('.calendar-nav').on('click', function (e) {
            e.stopPropagation();
            const nav = $(this).data('nav');
            if (nav === 'prev') {
                state.currentMonth = new Date(year, month - 1, 1);
            } else {
                state.currentMonth = new Date(year, month + 1, 1);
            }
            renderCalendar();
        });
        
        $('.calendar-day:not(.disabled):not(.other-month)').on('click', function () {
            $('.calendar-day').removeClass('selected');
            $(this).addClass('selected');
            const dateStr = $(this).data('date');
            state.selectedDate = parseDate(dateStr);
        });
        
        $('.calendar-accept-btn').on('click', function (e) {
            e.preventDefault(); // Prevent form submission
            e.stopPropagation(); // Stop event bubbling
            
            if (state.selectedDate) {
                $('#deliveryDatePicker').val(formatDateDisplay(state.selectedDate));
                $('#selectedDeliveryDate').val(formatDate(state.selectedDate));
                $('#datePickerCalendar').hide();
                
                // Clear time selection
                state.selectedMinTime = null;
                state.selectedMaxTime = null;
                
                // Reset select dropdowns to default
                $('#minDeliveryTime').empty().append(
                    $('<option>', {
                        value: '',
                        text: 'Seleccione horario',
                        selected: true
                    })
                );
                
                $('#maxDeliveryTime').empty().append(
                    $('<option>', {
                        value: '',
                        text: 'Seleccione horario',
                        selected: true
                    })
                );
                
                // Clear hidden fields
                $('#selectedMinTime').val('');
                $('#selectedMaxTime').val('');
                
                // Hide and clear time slots
                $('#timeSlotOptions').html('').hide();
                
                // Enable time selects (though we're using the grid now)
                $('#minDeliveryTime, #maxDeliveryTime').prop('disabled', false);
                
                // Show time slots immediately
                renderTimeSlots();
                $('#timeSlotOptions').show();
                
                console.log('Date selected:', formatDate(state.selectedDate));
            }
        });
    }

    /**
     * Render time slots for selected date
     */
    function renderTimeSlots() {
        if (!state.selectedDate) return;
        
        const dateStr = formatDate(state.selectedDate);
        // Handle both camelCase and PascalCase property names
        const slotData = state.availableSlots.find(s => 
            (s.dateFormatted && s.dateFormatted === dateStr) || 
            (s.DateFormatted && s.DateFormatted === dateStr)
        );
        
        // Get time slots array (handle both camelCase and PascalCase)
        const timeSlots = slotData ? (slotData.timeSlots || slotData.TimeSlots || []) : [];
        
        if (!slotData || timeSlots.length === 0) {
            $('#timeSlotOptions').html('<p class="text-danger">No hay horarios disponibles para esta fecha</p>');
            return;
        }
        
        let html = '<div class="row"><div class="col-12"><h6>Horarios disponibles:</h6></div></div>';
        html += '<div class="time-slot-grid">';
        
        timeSlots.forEach(slot => {
            // Handle both camelCase and PascalCase for all properties
            const isAvailable = slot.isAvailable ?? slot.IsAvailable ?? false;
            const minTimeRaw = slot.minTime || slot.MinTime;
            const maxTimeRaw = slot.maxTime || slot.MaxTime;
            const displayText = slot.displayText || slot.DisplayText;
            const slotId = slot.slotId || slot.SlotId;
            const availableCapacity = slot.availableCapacity ?? slot.AvailableCapacity ?? 0;
            
            // Format TimeSpan values to strings
            const minTime = formatTimeSpan(minTimeRaw);
            const maxTime = formatTimeSpan(maxTimeRaw);
            
            console.log('Time slot:', { displayText, minTime, maxTime, minTimeRaw, maxTimeRaw });
            
            let classes = 'time-slot-option';
            if (!isAvailable) {
                classes += ' disabled';
            }
            if (state.selectedMinTime === minTime && state.selectedMaxTime === maxTime) {
                classes += ' selected';
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
        
        html += '</div>';
        
        $('#timeSlotOptions').html(html);
        
        // Attach click handlers
        $('.time-slot-option:not(.disabled)').on('click', function () {
            $('.time-slot-option').removeClass('selected');
            $(this).addClass('selected');
            
            const minTime = $(this).data('min-time');
            const maxTime = $(this).data('max-time');
            const slotId = $(this).data('slot-id');
            const displayText = $(this).text().split('\n')[0].trim(); // Get just the time text
            
            state.selectedMinTime = minTime;
            state.selectedMaxTime = maxTime;
            
            console.log('Selected time slot:', { minTime, maxTime, slotId, displayText });
            
            // Update the select elements to show selected time
            $('#minDeliveryTime').empty().append(
                $('<option>', {
                    value: minTime,
                    text: minTime,
                    selected: true
                })
            );
            
            $('#maxDeliveryTime').empty().append(
                $('<option>', {
                    value: maxTime,
                    text: maxTime,
                    selected: true
                })
            );
            
            // Update hidden fields
            $('#selectedMinTime').val(minTime);
            $('#selectedMaxTime').val(maxTime);
            
            // Debug: verify values were set
            console.log('Hidden fields updated:');
            console.log('  selectedDeliveryDate:', $('#selectedDeliveryDate').val());
            console.log('  selectedMinTime:', $('#selectedMinTime').val());
            console.log('  selectedMaxTime:', $('#selectedMaxTime').val());
            console.log('  selectedReservationId:', $('#selectedReservationId').val());
            
            // Reserve the slot
            reserveSlot(state.selectedDate, minTime, maxTime, slotId);
        });
    }

    /**
     * Reserve a time slot
     */
    function reserveSlot(date, minTime, maxTime, slotId) {
        // Release previous reservation if exists
        if (state.reservationId) {
            releaseReservation(state.reservationId);
        }
        
        // Ensure times are formatted as strings
        const minTimeStr = formatTimeSpan(minTime);
        const maxTimeStr = formatTimeSpan(maxTime);
        
        const data = {
            deliveryDate: formatDate(date),
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
                console.log('Reserve slot response:', response);
                
                if (response.success || response.Success) {
                    const resId = response.reservationId || response.ReservationId;
                    state.reservationId = resId;
                    $('#selectedReservationId').val(resId);
                    console.log('Slot reserved:', resId);
                } else {
                    const errorMsg = response.message || response.Message || 'Error desconocido al reservar el horario';
                    alert(errorMsg);
                    console.error('Failed to reserve slot:', response);
                    // Reload available slots
                    loadAvailableSlots();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error reserving slot:', error);
                console.error('XHR:', xhr);
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
            async: false, // Synchronous for beforeunload
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
     * Format date as YYYY-MM-DD
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
     * Handles TimeSpan objects from C# which can be strings like "09:00:00" or objects
     */
    function formatTimeSpan(timeSpan) {
        if (!timeSpan) return '';
        
        // If it's already a string in HH:MM:SS format, return it
        if (typeof timeSpan === 'string') {
            return timeSpan;
        }
        
        // If it's an object with hours, minutes, seconds
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
