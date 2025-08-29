// product-validation.js
var ProductValidation = {
  config: {
    checkChangesUrl: '/shopping-cart/check-cart-changes',
    validateForCheckoutUrl: '/shopping-cart/validate-cart-for-checkout',
    clearSnapshotUrl: '/shopping-cart/clear-cart-snapshot',

    // Estado
    validationInProgress: false,
    bypassValidationOnce: false,
    lastTriggerElement: null,
    currentRetryCount: 0,
    validationCompleted: false,
    finalizingCheckout: false, // evita reentradas en el paso final cuando ya vamos a continuar

    // Polling
    pollingEnabled: true,
    pollingDelayMs: 1500,
    maxPollingAttempts: 10,
    pendingTimer: null // para cancelar timeouts pendientes
  },

  // === Helpers de validación completada ===
  isValidationCompleted: function () {
    if (typeof this.config.validationCompleted === 'boolean') {
      return this.config.validationCompleted;
    }
    try { return sessionStorage.getItem('pvValidationCompleted') === '1'; } catch (e) { return false; }
  },
  markValidationCompleted: function () {
    this.config.validationCompleted = true;
    try { sessionStorage.setItem('pvValidationCompleted', '1'); } catch (e) {}
  },
  clearValidationCompleted: function () {
    this.config.validationCompleted = false;
    try { sessionStorage.removeItem('pvValidationCompleted'); } catch (e) {}
  },

  // === Normalizador de payload del backend ===
  normalizeResponse: function (resp) {
    var isJobCompleted = (typeof resp.isJobCompleted !== 'undefined') ? !!resp.isJobCompleted : !!resp.jobCompleted;
    var hasChanges = !!(resp.hasChanges || resp.hasProductChanges);
    var changes = resp.changes || resp.productChanges || [];
    var canProceed = (typeof resp.canProceed === 'boolean') ? resp.canProceed : true; // en checks intermedios suele no venir
    return { raw: resp, isJobCompleted: isJobCompleted, hasChanges: hasChanges, changes: changes, canProceed: canProceed, message: resp.message };
  },

  // === Utilidades de timers ===
  _clearPendingTimer: function () {
    if (this.config.pendingTimer) {
      clearTimeout(this.config.pendingTimer);
      this.config.pendingTimer = null;
    }
  },

  // Interceptar las funciones nativas de nopCommerce
  interceptNopCommerceFunctions: function() {
    var self = this;
    
    // ShippingMethod
    if (typeof ShippingMethod !== 'undefined' && ShippingMethod.save && !ShippingMethod._originalSave) {
      ShippingMethod._originalSave = ShippingMethod.save;
      ShippingMethod.save = function() {
        var originalArgs = arguments, originalContext = ShippingMethod;
        console.log('ShippingMethod.save intercepted');
        if (self.config.bypassValidationOnce) { console.log('Bypassing ShippingMethod validation'); self.config.bypassValidationOnce = false; return ShippingMethod._originalSave.apply(originalContext, originalArgs); }
        if (self.config.validationInProgress) { console.log('ShippingMethod validation in progress, ignoring'); return false; }
        var $btn = $('.shipping-method-next-step-button');
        if ($btn.length) { self.checkProductChanges($btn, 0, function(){ ShippingMethod._originalSave.apply(originalContext, originalArgs); }); }
        else { return ShippingMethod._originalSave.apply(originalContext, originalArgs); }
        return false;
      };
    }

    // PaymentMethod
    if (typeof PaymentMethod !== 'undefined' && PaymentMethod.save && !PaymentMethod._originalSave) {
      PaymentMethod._originalSave = PaymentMethod.save;
      PaymentMethod.save = function() {
        var originalArgs = arguments, originalContext = PaymentMethod;
        console.log('PaymentMethod.save intercepted');
        if (self.config.bypassValidationOnce) { console.log('Bypassing PaymentMethod validation'); self.config.bypassValidationOnce = false; return PaymentMethod._originalSave.apply(originalContext, originalArgs); }
        if (self.config.validationInProgress) { console.log('PaymentMethod validation in progress, ignoring'); return false; }
        var $btn = $('.payment-method-next-step-button');
        if ($btn.length) { self.checkProductChanges($btn, 0, function(){ PaymentMethod._originalSave.apply(originalContext, originalArgs); }); }
        else { return PaymentMethod._originalSave.apply(originalContext, originalArgs); }
        return false;
      };
    }

    // BillingAddress
    if (typeof BillingAddress !== 'undefined' && BillingAddress.save && !BillingAddress._originalSave) {
      BillingAddress._originalSave = BillingAddress.save;
      BillingAddress.save = function() {
        var originalArgs = arguments, originalContext = BillingAddress;
        console.log('BillingAddress.save intercepted');
        if (self.config.bypassValidationOnce) { console.log('Bypassing BillingAddress validation'); self.config.bypassValidationOnce = false; return BillingAddress._originalSave.apply(originalContext, originalArgs); }
        if (self.config.validationInProgress) { console.log('BillingAddress validation in progress, ignoring'); return false; }
        var $btn = $('.new-address-next-step-button');
        if ($btn.length) { self.checkProductChanges($btn, 0, function(){ BillingAddress._originalSave.apply(originalContext, originalArgs); }); }
        else { return BillingAddress._originalSave.apply(originalContext, originalArgs); }
        return false;
      };
    }

    // ConfirmOrder (último paso)
    if (typeof ConfirmOrder !== 'undefined' && ConfirmOrder.save && !ConfirmOrder._originalSave) {
      ConfirmOrder._originalSave = ConfirmOrder.save;
      ConfirmOrder.save = function() {
        var originalArgs = arguments, originalContext = ConfirmOrder;
        console.log('ConfirmOrder.save intercepted');
        if (self.config.bypassValidationOnce) { console.log('Bypassing ConfirmOrder validation'); self.config.bypassValidationOnce = false; return ConfirmOrder._originalSave.apply(originalContext, originalArgs); }
        if (self.config.finalizingCheckout) { console.log('Already finalizing checkout, ignoring'); return false; }
        if (self.config.validationInProgress) { console.log('ConfirmOrder validation in progress, ignoring'); return false; }
        var $btn = $('.confirm-order-next-step-button');
        if ($btn.length) { self.validateForFinalCheckout($btn, 0, function(){ ConfirmOrder._originalSave.apply(originalContext, originalArgs); }); }
        else { return ConfirmOrder._originalSave.apply(originalContext, originalArgs); }
        return false;
      };
    }
  },

  selectors: {
    container: '.checkout-page',
    normalContinueButtons: '.new-address-next-step-button, .shipping-method-next-step-button, .payment-method-next-step-button',
    finalConfirmButton: '.confirm-order-next-step-button',
    confirmForm: '#co-confirm-order-form'
  },

  init: function () {
    this.bindEvents();
    try { this.config.validationCompleted = sessionStorage.getItem('pvValidationCompleted') === '1'; } catch (e) {}
    console.log('ProductValidation initialized');
    console.log('Normal buttons found:', $(this.selectors.normalContinueButtons).length);
    console.log('Final button found:', $(this.selectors.finalConfirmButton).length);
    console.log('Confirm form found:', $(this.selectors.confirmForm).length);
    console.log('Validation completed:', this.isValidationCompleted());
  },

  bindEvents: function () {
    var self = this;
    $(document).off('.productValidation');
    self.interceptNopCommerceFunctions();

    // Pasos intermedios (backup)
    $(document).on('click.productValidation', self.selectors.normalContinueButtons, function (e) {
      console.log('Normal continue button clicked:', this.className);
      if (self.config.bypassValidationOnce) { console.log('Bypassing validation'); self.config.bypassValidationOnce = false; return; }
      var $btn = $(this);
      if (self.config.validationInProgress) { console.log('Validation already in progress, ignoring click'); e.preventDefault(); e.stopImmediatePropagation(); return false; }
      e.preventDefault(); e.stopImmediatePropagation(); self.checkProductChanges($btn); return false;
    });

    // Último paso - botón confirmar
    $(document).on('click.productValidation', self.selectors.finalConfirmButton, function (e) {
      console.log('Final confirm button clicked');
      if (self.config.bypassValidationOnce) { console.log('Bypassing final validation'); self.config.bypassValidationOnce = false; return; }
      if (self.config.finalizingCheckout) { console.log('Already finalizing (button), ignoring'); e.preventDefault(); return; }
      var $btn = $(this);
      if (self.config.validationInProgress) { console.log('Final validation already in progress, ignoring click'); e.preventDefault(); return; }
      e.preventDefault(); self.validateForFinalCheckout($btn);
    });

    // Último paso - submit del form
    $(document).on('submit.productValidation', self.selectors.confirmForm, function (e) {
      console.log('Confirm form submitted');
      if (self.config.bypassValidationOnce) { console.log('Bypassing form validation'); self.config.bypassValidationOnce = false; return; }
      if (self.config.finalizingCheckout) { console.log('Already finalizing (form), ignoring'); e.preventDefault(); return; }
      var $form = $(this);
      if (self.config.validationInProgress) { console.log('Form validation already in progress, ignoring submit'); e.preventDefault(); return; }
      e.preventDefault(); self.validateForFinalCheckout($form);
    });
  },

  // Validación final antes del pago/confirmación
  // REGLA: si hay cambios -> mostrar modal; si NO hay cambios -> continuar sin mostrar nada
  validateForFinalCheckout: function (triggerElement, retryCount, onSuccess) {
    var self = this; retryCount = retryCount || 0;
    console.log('validateForFinalCheckout called, retry:', retryCount);

    if (self.config.finalizingCheckout) { console.log('Finalizing flag set, aborting'); return; }
    if (self.config.validationInProgress && retryCount === 0) { console.log('Final validation already in progress, aborting'); return; }

    self._clearPendingTimer();

    self.config.validationInProgress = true;
    self.config.lastTriggerElement = triggerElement;
    self.config.currentRetryCount = retryCount;
    self.showLoading(triggerElement);

    var tokenEl = $('input[name="__RequestVerificationToken"]');
    var token = tokenEl.length ? tokenEl.val() : null;

    $.ajax({
      url: self.config.validateForCheckoutUrl,
      type: 'POST', dataType: 'json', headers: token ? { 'RequestVerificationToken': token } : {}, timeout: 30000,
      success: function (resp) {
        var n = self.normalizeResponse(resp);
        console.log('Final validation response (normalized):', n);

        // Polling si job aún no termina
        if (resp && resp.success && !n.isJobCompleted && self.config.pollingEnabled) {
          if (retryCount < self.config.maxPollingAttempts) {
            self.hideLoading(triggerElement);
            self.config.pendingTimer = setTimeout(function () {
              self.validateForFinalCheckout(triggerElement, retryCount + 1, onSuccess);
            }, self.config.pollingDelayMs);
          } else {
            console.warn('Final validation: timeout; continuing checkout.');
            self.resetValidationState(); self.hideLoading(triggerElement);
            self._proceedFinal(triggerElement, onSuccess);
          }
          return;
        }

        // Job completo o error controlado
        self.resetValidationState(); self.hideLoading(triggerElement);

        if (resp && resp.success && n.canProceed) {
          if (n.isJobCompleted && n.hasChanges && n.changes.length > 0) {
            // *** MOSTRAR MODAL EN FINAL SOLO SI HAY CAMBIOS ***
            console.log('Final step: changes detected, showing modal');
            self.showChangesModal(n.changes, triggerElement, false);
            self.markValidationCompleted();
            // No activamos finalizingCheckout aún: el usuario decidirá (ver productos / ir al inicio)
          } else {
            // Sin cambios -> continuar silenciosamente
            console.log('Final step: no changes, proceeding silently');
            self._proceedFinal(triggerElement, onSuccess);
          }
        } else if (resp && resp.shouldRedirectHome) {
          self.showErrorModal(n.message || 'No es posible continuar. Serás redirigido al inicio.');
        } else if (resp && !resp.success) {
          console.error('Final validation reported failure; proceeding for UX continuity. Message:', n.message);
          self._proceedFinal(triggerElement, onSuccess);
        } else {
          // Caso ambiguo
          self._proceedFinal(triggerElement, onSuccess);
        }
      },
      error: function (xhr, status, error) {
        console.error('AJAX error in final validation:', error, status);
        self.resetValidationState(); self.hideLoading(triggerElement);
        self._proceedFinal(triggerElement, onSuccess);
      }
    });
  },

  // Encapsula la continuación final con anti-loop
  _proceedFinal: function (triggerElement, onSuccess) {
    this.config.finalizingCheckout = true; // desde aquí bloqueamos reentradas
    this._clearPendingTimer();

    if (onSuccess) {
      try { onSuccess(); } catch (e) { console.error('onSuccess error:', e); }
      return;
    }
    this.continueCheckout(triggerElement);
  },

  // Validación normal (pasos intermedios)
  checkProductChanges: function (triggerElement, retryCount, onSuccess) {
    var self = this; retryCount = retryCount || 0;
    console.log('checkProductChanges called, retry:', retryCount);

    if (self.isValidationCompleted()) {
      console.log('Validation already completed, continuing normally');
      if (onSuccess) { onSuccess(); } else { self.continueCheckout(triggerElement); }
      return;
    }
    if (self.config.validationInProgress && retryCount === 0) { console.log('Validation already in progress, aborting'); return; }

    self._clearPendingTimer();

    self.config.validationInProgress = true; self.config.lastTriggerElement = triggerElement; self.config.currentRetryCount = retryCount; self.showLoading(triggerElement);

    var tokenEl = $('input[name="__RequestVerificationToken"]');
    var token = tokenEl.length ? tokenEl.val() : null;

    $.ajax({
      url: self.config.checkChangesUrl,
      type: 'POST', dataType: 'json', headers: token ? { 'RequestVerificationToken': token } : {}, timeout: 30000,
      success: function (resp) {
        var n = self.normalizeResponse(resp);
        console.log('Check changes response (normalized):', n);

        if (resp && resp.success && !n.isJobCompleted && self.config.pollingEnabled) {
          if (retryCount < self.config.maxPollingAttempts) {
            self.hideLoading(triggerElement);
            self.config.pendingTimer = setTimeout(function () {
              self.checkProductChanges(triggerElement, retryCount + 1, onSuccess);
            }, self.config.pollingDelayMs);
          } else {
            console.warn('Validation: timeout; continuing checkout.');
            self.resetValidationState(); self.hideLoading(triggerElement);
            if (onSuccess) { onSuccess(); } else { self.continueCheckout(triggerElement); }
          }
          return;
        }

        self.resetValidationState(); self.hideLoading(triggerElement);

        if (resp && resp.success) {
          if (n.isJobCompleted && n.hasChanges && n.changes.length > 0) {
            console.log('Intermediate step: changes detected, showing modal');
            self.showChangesModal(n.changes, triggerElement, true);
            self.markValidationCompleted();
          } else {
            console.log('Intermediate step: no changes, continue');
            if (onSuccess) { onSuccess(); } else { self.continueCheckout(triggerElement); }
          }
        } else {
          console.error('Error checking product changes:', resp && resp.message);
          if (onSuccess) { onSuccess(); } else { self.continueCheckout(triggerElement); }
        }
      },
      error: function (xhr, status, error) {
        console.error('AJAX error checking product changes:', error, status);
        self.resetValidationState(); self.hideLoading(triggerElement);
        if (onSuccess) { onSuccess(); } else { self.continueCheckout(triggerElement); }
      }
    });
  },

  // Reset de estado
  resetValidationState: function() {
    this.config.validationInProgress = false;
    this.config.currentRetryCount = 0;
    this._clearPendingTimer();
    console.log('Validation state reset');
  },

  // Modal de cambios detectados
  showChangesModal: function (changes, triggerElement, allowContinue) {
    var self = this; self.config.lastTriggerElement = triggerElement;

    var changesHtml = '';
    (changes || []).forEach(function (change) {
      var changeIcon = 'ℹ️', changeClass = 'product-info';
      switch (change.changeType) {
        case 'price_changed': changeIcon = '💰'; changeClass = 'price-change'; break;
        case 'unpublished':  changeIcon = '⚠️'; changeClass = 'product-unavailable'; break;
        case 'deleted':      changeIcon = '❌'; changeClass = 'product-deleted'; break;
      }
      changesHtml += `
        <div class="product-change-item ${changeClass}">
          <div class="change-icon">${changeIcon}</div>
          <div class="change-details">
            <strong>${(change.productName || '').toString()}</strong><br>
            <span class="change-message">${(change.message || '').toString()}</span>
          </div>
        </div>`;
    });

    var continueBtnHtml = allowContinue ? `
      <button type="button" class="btn btn-outline" onclick="ProductValidation.continueAnyway()">
        ➡️ Continuar de Todas Formas
      </button>` : '';

    var modalHtml = `
      <div id="product-changes-modal" class="validation-modal">
        <div class="modal-content">
          <div class="modal-header"><h3>🔄 Productos Actualizados</h3></div>
          <div class="modal-body">
            <p>Algunos productos en tu carrito han sido actualizados:</p>
            <div class="changes-list">${changesHtml}</div>
            <p class="modal-note">${allowContinue ? '¿Qué deseas hacer?' : 'Debes revisar los cambios antes de continuar.'}</p>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-primary" onclick="ProductValidation.reloadPage()">🔄 Ver Productos Actualizados</button>
            <button type="button" class="btn btn-secondary" onclick="ProductValidation.goToHome()">🏠 Ir al Inicio</button>
            ${continueBtnHtml}
          </div>
        </div>
      </div>`;

    $('#product-changes-modal').remove();
    $('body').append(modalHtml);
    setTimeout(function () { $('#product-changes-modal').addClass('show'); }, 10);

    // Limpiar snapshot una vez mostrado (evita estados en servidor)
    this.clearSnapshot();
  },

  // Continuar con el flujo original (evita loops)
  continueCheckout: function (triggerElement) {
    console.log('Continuing checkout');
    this.resetValidationState(); this.config.bypassValidationOnce = true;

    try {
      if (triggerElement && triggerElement.length && triggerElement.is('form')) {
        console.log('Submitting form'); triggerElement[0].submit();
      } else if (triggerElement && triggerElement.length) {
        console.log('Clicking button');
        var onclickAttr = triggerElement.attr('onclick');
        if (onclickAttr) { console.log('Executing onclick:', onclickAttr); eval(onclickAttr); }
        else { triggerElement[0].click(); }
      } else if (this.config.lastTriggerElement && this.config.lastTriggerElement.length) {
        console.log('Clicking last trigger element');
        if (this.config.lastTriggerElement.is('form')) { this.config.lastTriggerElement[0].submit(); }
        else {
          var onclickAttr2 = this.config.lastTriggerElement.attr('onclick');
          if (onclickAttr2) { console.log('Executing last trigger onclick:', onclickAttr2); eval(onclickAttr2); }
          else { this.config.lastTriggerElement[0].click(); }
        }
      } else {
        console.warn('No trigger element found to continue checkout');
      }
    } catch (e) {
      console.error('Error in continueCheckout:', e);
      this.config.bypassValidationOnce = false;
    }
  },

  // Navegación y cierre
  reloadPage: function () { this.clearSnapshot(); this.resetValidationState(); this.clearValidationCompleted(); window.location.reload(); },
  goToHome: function () { this.clearSnapshot(); this.resetValidationState(); this.clearValidationCompleted(); window.location.href = '/'; },

  continueAnyway: function () {
    try {
      console.log('Continue anyway clicked');
      if (this.config.finalizingCheckout) { console.log('Already finalizing, ignoring continueAnyway'); return; }
      this.closeModal();
      if (this.config.lastTriggerElement && this.config.lastTriggerElement.length) { this.continueCheckout(this.config.lastTriggerElement); }
      else { console.warn('No se encontró el elemento original para continuar.'); }
    } catch (e) { console.error('Error continuing anyway:', e); this.closeModal(); }
  },

  // Modales utilitarios
  showWaitingModal: function (message) {
    var html = `
      <div id="pv-waiting-modal" class="validation-modal">
        <div class="modal-content">
          <div class="modal-header"><h3>⏳ Por favor espera</h3></div>
          <div class="modal-body"><p>${(message || 'Estamos validando tu pedido...').toString()}</p></div>
          <div class="modal-actions">
            <button type="button" class="btn btn-outline" onclick="ProductValidation.closeWaitingModal()">Cerrar</button>
          </div>
        </div>
        <div class="modal-overlay" onclick="ProductValidation.closeWaitingModal()"></div>
      </div>`;
    $('#pv-waiting-modal').remove(); $('body').append(html); setTimeout(function(){ $('#pv-waiting-modal').addClass('show'); }, 10);
  },
  closeWaitingModal: function () { $('#pv-waiting-modal').removeClass('show'); setTimeout(function(){ $('#pv-waiting-modal').remove(); }, 300); },

  showErrorModal: function (message) {
    var html = `
      <div id="pv-error-modal" class="validation-modal">
        <div class="modal-content">
          <div class="modal-header"><h3>❗ No es posible continuar</h3></div>
          <div class="modal-body"><p>${(message || 'Ha ocurrido un problema con tu pedido.').toString()}</p></div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" onclick="ProductValidation.goToHome()">Ir al Inicio</button>
            <button type="button" class="btn btn-outline" onclick="ProductValidation.closeErrorModal()">Cerrar</button>
          </div>
        </div>
        <div class="modal-overlay" onclick="ProductValidation.closeErrorModal()"></div>
      </div>`;
    $('#pv-error-modal').remove(); $('body').append(html); setTimeout(function(){ $('#pv-error-modal').addClass('show'); }, 10);
  },
  closeErrorModal: function () { $('#pv-error-modal').removeClass('show'); setTimeout(function(){ $('#pv-error-modal').remove(); }, 300); },
  closeModal: function () { $('#product-changes-modal').removeClass('show'); setTimeout(function(){ $('#product-changes-modal').remove(); }, 300); },

  // Clear snapshot (server)
  clearSnapshot: function () {
    var self = this; var tokenEl = $('input[name="__RequestVerificationToken"]'); var token = tokenEl.length ? tokenEl.val() : null;
    $.ajax({
      url: this.config.clearSnapshotUrl, type: 'POST', dataType: 'json', headers: token ? { 'RequestVerificationToken': token } : {},
      success: function () { console.log('Snapshot cleared successfully'); self.clearValidationCompleted(); self.resetValidationState(); },
      error: function (xhr, status, error) { console.error('Error clearing snapshot:', error); self.clearValidationCompleted(); self.resetValidationState(); }
    });
  },

  // Loading UX solo para <button>
  showLoading: function (el) { if (el && el.length && el.is('button')) { el.prop('disabled', true); el.data('original-text', el.text()); el.text('Verificando...'); } },
  hideLoading: function (el) { if (el && el.length && el.is('button')) { el.prop('disabled', false); var txt = el.data('original-text'); if (txt) el.text(txt); } },

};

// Inicializar
$(document).ready(function () { ProductValidation.init(); });

// Agregar al objeto window para debugging
window.ProductValidation = ProductValidation;
