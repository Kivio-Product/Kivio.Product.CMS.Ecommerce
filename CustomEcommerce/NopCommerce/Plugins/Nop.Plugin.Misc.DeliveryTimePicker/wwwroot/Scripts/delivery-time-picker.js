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
            const slotData = state.availableSlots.find(s => s.dateFormatted === dateStr);
            
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
            
            // Check if available
            if (!slotData || !slotData.isAvailable) {
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
        
        $('.calendar-accept-btn').on('click', function () {
            if (state.selectedDate) {
                $('#deliveryDatePicker').val(formatDateDisplay(state.selectedDate));
                $('#selectedDeliveryDate').val(formatDate(state.selectedDate));
                $('#datePickerCalendar').hide();
                
                // Clear time selection
                state.selectedMinTime = null;
                state.selectedMaxTime = null;
                $('#minDeliveryTime').val('');
                $('#maxDeliveryTime').val('');
                $('#timeSlotOptions').html('').hide();
            }
        });
    }

    /**
     * Render time slots for selected date
     */
    function renderTimeSlots() {
        if (!state.selectedDate) return;
        
        const dateStr = formatDate(state.selectedDate);
        const slotData = state.availableSlots.find(s => s.dateFormatted === dateStr);
        
        if (!slotData || !slotData.timeSlots || slotData.timeSlots.length === 0) {
            $('#timeSlotOptions').html('<p class="text-danger">No hay horarios disponibles para esta fecha</p>');
            return;
        }
        
        let html = '<div class="row"><div class="col-12"><h6>Horarios disponibles:</h6></div></div>';
        html += '<div class="time-slot-grid">';
        
        slotData.timeSlots.forEach(slot => {
            let classes = 'time-slot-option';
            if (!slot.isAvailable) {
                classes += ' disabled';
            }
            if (state.selectedMinTime === slot.minTime && state.selectedMaxTime === slot.maxTime) {
                classes += ' selected';
            }
            
            html += '<div class="' + classes + '" ';
            html += 'data-min-time="' + slot.minTime + '" ';
            html += 'data-max-time="' + slot.maxTime + '" ';
            html += 'data-slot-id="' + (slot.slotId || '') + '">';
            html += slot.displayText;
            if (!slot.isAvailable) {
                html += '<div class="capacity-info">No disponible</div>';
            } else {
                html += '<div class="capacity-info">' + slot.availableCapacity + ' disponibles</div>';
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
            
            state.selectedMinTime = minTime;
            state.selectedMaxTime = maxTime;
            
            // Update selects
            $('#minDeliveryTime').val(minTime);
            $('#maxDeliveryTime').val(maxTime);
            $('#selectedMinTime').val(minTime);
            $('#selectedMaxTime').val(maxTime);
            
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
        
        const data = {
            deliveryDate: formatDate(date),
            minDeliveryTime: minTime,
            maxDeliveryTime: maxTime,
            timeSlotId: slotId || null,
            isTemporary: true
        };
        
        $.ajax({
            url: config.baseUrl + '/ReserveSlot',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function (response) {
                if (response.success) {
                    state.reservationId = response.reservationId;
                    $('#selectedReservationId').val(response.reservationId);
                    console.log('Slot reserved:', response.reservationId);
                } else {
                    alert(response.message);
                    // Reload available slots
                    loadAvailableSlots();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error reserving slot:', error);
                alert('Error al reservar el horario. Por favor, intente nuevamente.');
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
