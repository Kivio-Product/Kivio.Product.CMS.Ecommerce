// product-validation.js - Versión corregida
var ProductValidation = {
  config: {
    checkChangesUrl: '/shopping-cart/check-cart-changes',
    validateForCheckoutUrl: '/shopping-cart/validate-cart-for-checkout',
    clearSnapshotUrl: '/shopping-cart/clear-cart-snapshot',

    // Estado
    validationInProgress: false,
    bypassValidationOnce: false,
    lastTriggerElement: null,
    currentRetryCount: 0, // Agregar control de retry actual

    // Polling
    pollingEnabled: true,
    pollingDelayMs: 1500,
    maxPollingAttempts: 10
  },

  // Interceptar las funciones nativas de nopCommerce
  interceptNopCommerceFunctions: function() {
    var self = this;
    
    // Backup de funciones originales
    if (typeof ShippingMethod !== 'undefined' && ShippingMethod.save && !ShippingMethod._originalSave) {
      ShippingMethod._originalSave = ShippingMethod.save;
      ShippingMethod.save = function() {
        var originalArgs = arguments;
        var originalContext = ShippingMethod;
        console.log('ShippingMethod.save intercepted');
        if (self.config.bypassValidationOnce) {
          console.log('Bypassing ShippingMethod validation');
          self.config.bypassValidationOnce = false;
          return ShippingMethod._originalSave.apply(originalContext, originalArgs);
        }
        
        if (self.config.validationInProgress) {
          console.log('ShippingMethod validation in progress, ignoring');
          return false;
        }
        
        var $btn = $('.shipping-method-next-step-button');
        if ($btn.length) {
          self.checkProductChanges($btn, 0, function() {
            ShippingMethod._originalSave.apply(originalContext, originalArgs);
          });
        } else {
          return ShippingMethod._originalSave.apply(originalContext, originalArgs);
        }
        return false;
      };
    }
    
    if (typeof PaymentMethod !== 'undefined' && PaymentMethod.save && !PaymentMethod._originalSave) {
      PaymentMethod._originalSave = PaymentMethod.save;
      PaymentMethod.save = function() {
        var originalArgs = arguments;
        var originalContext = PaymentMethod;
        console.log('PaymentMethod.save intercepted');
        if (self.config.bypassValidationOnce) {
          console.log('Bypassing PaymentMethod validation');
          self.config.bypassValidationOnce = false;
          return PaymentMethod._originalSave.apply(originalContext, originalArgs);
        }
        
        if (self.config.validationInProgress) {
          console.log('PaymentMethod validation in progress, ignoring');
          return false;
        }
        
        var $btn = $('.payment-method-next-step-button');
        if ($btn.length) {
          self.checkProductChanges($btn, 0, function() {
            PaymentMethod._originalSave.apply(originalContext, originalArgs);
          });
        } else {
          return PaymentMethod._originalSave.apply(originalContext, originalArgs);
        }
        return false;
      };
    }
    
    if (typeof BillingAddress !== 'undefined' && BillingAddress.save && !BillingAddress._originalSave) {
      BillingAddress._originalSave = BillingAddress.save;
      BillingAddress.save = function() {
        var originalArgs = arguments;
        var originalContext = BillingAddress;
        console.log('BillingAddress.save intercepted');
        if (self.config.bypassValidationOnce) {
          console.log('Bypassing BillingAddress validation');
          self.config.bypassValidationOnce = false;
          return BillingAddress._originalSave.apply(originalContext, originalArgs);
        }
        
        if (self.config.validationInProgress) {
          console.log('BillingAddress validation in progress, ignoring');
          return false;
        }
        
        var $btn = $('.new-address-next-step-button');
        if ($btn.length) {
          self.checkProductChanges($btn, 0, function() {
            BillingAddress._originalSave.apply(originalContext, originalArgs);
          });
        } else {
          return BillingAddress._originalSave.apply(originalContext, originalArgs);
        }
        return false;
      };
    }
    
    if (typeof ConfirmOrder !== 'undefined' && ConfirmOrder.save && !ConfirmOrder._originalSave) {
      ConfirmOrder._originalSave = ConfirmOrder.save;
      ConfirmOrder.save = function() {
        var originalArgs = arguments;
        var originalContext = ConfirmOrder;
        console.log('ConfirmOrder.save intercepted');
        if (self.config.bypassValidationOnce) {
          console.log('Bypassing ConfirmOrder validation');
          self.config.bypassValidationOnce = false;
          return ConfirmOrder._originalSave.apply(originalContext, originalArgs);
        }
        
        if (self.config.validationInProgress) {
          console.log('ConfirmOrder validation in progress, ignoring');
          return false;
        }
        
        var $btn = $('.confirm-order-next-step-button');
        if ($btn.length) {
          self.validateForFinalCheckout($btn, 0, function() {
            ConfirmOrder._originalSave.apply(originalContext, originalArgs);
          });
        } else {
          return ConfirmOrder._originalSave.apply(originalContext, originalArgs);
        }
        return false;
      };
    }
  },

  selectors: {
    container: '.checkout-page',
    // Botones de "Continuar" en pasos intermedios
    normalContinueButtons: '.new-address-next-step-button, .shipping-method-next-step-button, .payment-method-next-step-button',
    // Botón de Confirmar pedido (último paso)
    finalConfirmButton: '.confirm-order-next-step-button',
    // Form del último paso (cuando aplique)
    confirmForm: '#co-confirm-order-form'
  },

  init: function () {
    this.bindEvents();
    
    // Debug: Verificar qué elementos están presentes
    console.log('ProductValidation initialized');
    console.log('Normal buttons found:', $(this.selectors.normalContinueButtons).length);
    console.log('Final button found:', $(this.selectors.finalConfirmButton).length);
    console.log('Confirm form found:', $(this.selectors.confirmForm).length);
  },

  bindEvents: function () {
    var self = this;

    // Evitar doble binding
    $(document).off('.productValidation');

    // Interceptar funciones nativas de nopCommerce
    self.interceptNopCommerceFunctions();

    // Interceptar "Continuar" en pasos intermedios (como backup)
    $(document).on('click.productValidation', self.selectors.normalContinueButtons, function (e) {
      console.log('Normal continue button clicked:', this.className);
      
      // Verificar bypass
      if (self.config.bypassValidationOnce) { 
        console.log('Bypassing validation');
        self.config.bypassValidationOnce = false; 
        return; 
      }
      
      var $btn = $(this);
      
      // Verificar si ya hay una validación en progreso
      if (self.config.validationInProgress) {
        console.log('Validation already in progress, ignoring click');
        e.preventDefault();
        e.stopImmediatePropagation();
        return false;
      }
      
      e.preventDefault();
      e.stopImmediatePropagation();
      self.checkProductChanges($btn);
      return false;
    });

    // Interceptar botón "Confirmar" (último paso)
    $(document).on('click.productValidation', self.selectors.finalConfirmButton, function (e) {
      console.log('Final confirm button clicked');
      
      if (self.config.bypassValidationOnce) { 
        console.log('Bypassing final validation');
        self.config.bypassValidationOnce = false; 
        return; 
      }
      
      var $btn = $(this);
      
      if (self.config.validationInProgress) {
        console.log('Final validation already in progress, ignoring click');
        e.preventDefault();
        return;
      }
      
      e.preventDefault();
      self.validateForFinalCheckout($btn);
    });

    // Interceptar submit del form del último paso
    $(document).on('submit.productValidation', self.selectors.confirmForm, function (e) {
      console.log('Confirm form submitted');
      
      if (self.config.bypassValidationOnce) { 
        console.log('Bypassing form validation');
        self.config.bypassValidationOnce = false; 
        return; 
      }
      
      var $form = $(this);
      
      if (self.config.validationInProgress) {
        console.log('Form validation already in progress, ignoring submit');
        e.preventDefault();
        return;
      }
      
      e.preventDefault();
      self.validateForFinalCheckout($form);
    });
  },

  // Validación final antes del pago/confirmación
  validateForFinalCheckout: function (triggerElement, retryCount, onSuccess) {
    var self = this;
    retryCount = retryCount || 0;
    
    console.log('validateForFinalCheckout called, retry:', retryCount);
    
    // Prevenir múltiples llamadas simultáneas
    if (self.config.validationInProgress && retryCount === 0) {
      console.log('Final validation already in progress, aborting');
      return;
    }
    
    self.config.validationInProgress = true;
    self.config.lastTriggerElement = triggerElement;
    self.config.currentRetryCount = retryCount;
    self.showLoading(triggerElement);

    var tokenEl = $('input[name="__RequestVerificationToken"]');
    var token = tokenEl.length ? tokenEl.val() : null;

    $.ajax({
      url: self.config.validateForCheckoutUrl,
      type: 'POST',
      dataType: 'json',
      headers: token ? { 'RequestVerificationToken': token } : {},
      timeout: 30000, // Timeout de 30 segundos
      success: function (response) {
        console.log('Final validation response:', response);
        
        // Manejar polling si es necesario
        if (response && response.success && response.jobCompleted === false && self.config.pollingEnabled) {
          if (retryCount < self.config.maxPollingAttempts) {
            self.showWaitingModal(response.message || 'Validación en progreso...');
            self.hideLoading(triggerElement);
            
            // NO resetear validationInProgress aquí durante polling
            setTimeout(function () {
              self.validateForFinalCheckout(triggerElement, retryCount + 1, onSuccess);
            }, self.config.pollingDelayMs);
          } else {
            console.warn('Final validation: tiempo de espera agotado; continuando checkout.');
            self.resetValidationState();
            self.hideLoading(triggerElement);
            self.closeWaitingModal();
            if (onSuccess) {
              onSuccess();
            } else {
              self.continueCheckout(triggerElement);
            }
          }
          return;
        }

        // Resetear estado después de completar
        self.resetValidationState();
        self.hideLoading(triggerElement);
        self.closeWaitingModal();

        if (response && response.success) {
          if (response.canProceed) {
            if (response.hasProductChanges && response.productChanges && response.productChanges.length > 0) {
              self.showChangesModal(response.productChanges, triggerElement, false);
            } else {
              if (onSuccess) {
                onSuccess();
              } else {
                self.continueCheckout(triggerElement);
              }
            }
          } else {
            if (response.shouldRedirectHome) {
              self.showErrorModal(response.message || 'No es posible continuar. Serás redirigido al inicio.');
            } else {
              self.showWaitingModal(response.message || 'Estamos verificando tu pedido. Intenta nuevamente en un momento.');
            }
          }
        } else {
          console.error('Error in final validation:', response && response.message);
          if (onSuccess) {
            onSuccess();
          } else {
            self.continueCheckout(triggerElement);
          }
        }
      },
      error: function (xhr, status, error) {
        console.error('AJAX error in final validation:', error, status);
        self.resetValidationState();
        self.hideLoading(triggerElement);
        self.closeWaitingModal();
        
        // En caso de error, continuar para no bloquear el checkout
        if (onSuccess) {
          onSuccess();
        } else {
          self.continueCheckout(triggerElement);
        }
      }
    });
  },

  // Validación normal (pasos intermedios)
  checkProductChanges: function (triggerElement, retryCount, onSuccess) {
    var self = this;
    retryCount = retryCount || 0;
    
    console.log('checkProductChanges called, retry:', retryCount);
    
    // Prevenir múltiples llamadas simultáneas
    if (self.config.validationInProgress && retryCount === 0) {
      console.log('Validation already in progress, aborting');
      return;
    }
    
    self.config.validationInProgress = true;
    self.config.lastTriggerElement = triggerElement;
    self.config.currentRetryCount = retryCount;
    self.showLoading(triggerElement);

    var tokenEl = $('input[name="__RequestVerificationToken"]');
    var token = tokenEl.length ? tokenEl.val() : null;

    $.ajax({
      url: self.config.checkChangesUrl,
      type: 'POST',
      dataType: 'json',
      headers: token ? { 'RequestVerificationToken': token } : {},
      timeout: 30000, // Timeout de 30 segundos
      success: function (response) {
        console.log('Check changes response:', response);
        
        // Manejar polling si es necesario
        if (response && response.success && response.jobCompleted === false && self.config.pollingEnabled) {
          if (retryCount < self.config.maxPollingAttempts) {
            self.showWaitingModal(response.message || 'Validación en progreso...');
            self.hideLoading(triggerElement);
            
            // NO resetear validationInProgress aquí durante polling
            setTimeout(function () {
              self.checkProductChanges(triggerElement, retryCount + 1, onSuccess);
            }, self.config.pollingDelayMs);
          } else {
            console.warn('Validation: tiempo de espera agotado; continuando checkout.');
            self.resetValidationState();
            self.hideLoading(triggerElement);
            self.closeWaitingModal();
            if (onSuccess) {
              onSuccess();
            } else {
              self.continueCheckout(triggerElement);
            }
          }
          return;
        }

        // Resetear estado después de completar
        self.resetValidationState();
        self.hideLoading(triggerElement);
        self.closeWaitingModal();

        if (response && response.success) {
          var hasChanges = !!(response.hasChanges && response.changes && response.changes.length > 0);
          if (hasChanges) {
            self.showChangesModal(response.changes, triggerElement, true);
          } else {
            if (onSuccess) {
              onSuccess();
            } else {
              self.continueCheckout(triggerElement);
            }
          }
        } else {
          console.error('Error checking product changes:', response && response.message);
          if (onSuccess) {
            onSuccess();
          } else {
            self.continueCheckout(triggerElement);
          }
        }
      },
      error: function (xhr, status, error) {
        console.error('AJAX error checking product changes:', error, status);
        self.resetValidationState();
        self.hideLoading(triggerElement);
        self.closeWaitingModal();
        
        // En caso de error, continuar para no bloquear el checkout
        if (onSuccess) {
          onSuccess();
        } else {
          self.continueCheckout(triggerElement);
        }
      }
    });
  },

  // Nuevo método para resetear el estado de validación
  resetValidationState: function() {
    this.config.validationInProgress = false;
    this.config.currentRetryCount = 0;
    console.log('Validation state reset');
  },

  // Modal de cambios detectados
  showChangesModal: function (changes, triggerElement, allowContinue) {
    var self = this;
    self.config.lastTriggerElement = triggerElement;

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
        <div class="modal-overlay" onclick="ProductValidation.closeModal()"></div>
      </div>`;

    $('#product-changes-modal').remove();
    $('body').append(modalHtml);
    setTimeout(function () { $('#product-changes-modal').addClass('show'); }, 10);

    // Limpiar snapshot una vez mostrado
    this.clearSnapshot();
  },

  // Continuar con el flujo original (evita loops)
  continueCheckout: function (triggerElement) {
    console.log('Continuing checkout');
    
    // Asegurar que el estado esté limpio
    this.resetValidationState();
    this.config.bypassValidationOnce = true;

    try {
      if (triggerElement && triggerElement.length && triggerElement.is('form')) {
        console.log('Submitting form');
        triggerElement[0].submit();
      } else if (triggerElement && triggerElement.length) {
        console.log('Clicking button');
        // Para botones con onclick, ejecutar la función directamente
        var onclickAttr = triggerElement.attr('onclick');
        if (onclickAttr) {
          console.log('Executing onclick:', onclickAttr);
          eval(onclickAttr);
        } else {
          triggerElement[0].click();
        }
      } else if (this.config.lastTriggerElement && this.config.lastTriggerElement.length) {
        console.log('Clicking last trigger element');
        if (this.config.lastTriggerElement.is('form')) {
          this.config.lastTriggerElement[0].submit();
        } else {
          var onclickAttr = this.config.lastTriggerElement.attr('onclick');
          if (onclickAttr) {
            console.log('Executing last trigger onclick:', onclickAttr);
            eval(onclickAttr);
          } else {
            this.config.lastTriggerElement[0].click();
          }
        }
      } else {
        console.warn('No trigger element found to continue checkout');
      }
    } catch (e) {
      console.error('Error in continueCheckout:', e);
      // Resetear bypass en caso de error
      this.config.bypassValidationOnce = false;
    }
  },

  // Navegación y cierre
  reloadPage: function () { 
    this.clearSnapshot(); 
    this.resetValidationState();
    window.location.reload(); 
  },
  
  goToHome: function () { 
    this.clearSnapshot(); 
    this.resetValidationState();
    window.location.href = '/'; 
  },

  continueAnyway: function () {
    try {
      console.log('Continue anyway clicked');
      this.closeModal();
      if (this.config.lastTriggerElement && this.config.lastTriggerElement.length) {
        this.continueCheckout(this.config.lastTriggerElement);
      } else {
        console.warn('No se encontró el elemento original para continuar.');
      }
    } catch (e) {
      console.error('Error continuing anyway:', e);
      this.closeModal();
    }
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
    $('#pv-waiting-modal').remove();
    $('body').append(html);
    setTimeout(function(){ $('#pv-waiting-modal').addClass('show'); }, 10);
  },
  
  closeWaitingModal: function () {
    $('#pv-waiting-modal').removeClass('show'); 
    setTimeout(function(){ $('#pv-waiting-modal').remove(); }, 300);
  },

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
    $('#pv-error-modal').remove();
    $('body').append(html);
    setTimeout(function(){ $('#pv-error-modal').addClass('show'); }, 10);
  },
  
  closeErrorModal: function () {
    $('#pv-error-modal').removeClass('show'); 
    setTimeout(function(){ $('#pv-error-modal').remove(); }, 300);
  },

  closeModal: function () {
    $('#product-changes-modal').removeClass('show'); 
    setTimeout(function(){ $('#product-changes-modal').remove(); }, 300);
  },

  // Clear snapshot (server)
  clearSnapshot: function () {
    var tokenEl = $('input[name="__RequestVerificationToken"]');
    var token = tokenEl.length ? tokenEl.val() : null;

    $.ajax({
      url: this.config.clearSnapshotUrl,
      type: 'POST',
      dataType: 'json',
      headers: token ? { 'RequestVerificationToken': token } : {},
      success: function () { 
        console.log('Snapshot cleared successfully');
      },
      error: function (xhr, status, error) { 
        console.error('Error clearing snapshot:', error); 
      }
    });
  },

  // Loading UX solo para <button>
  showLoading: function (el) {
    if (el && el.length && el.is('button')) {
      el.prop('disabled', true);
      el.data('original-text', el.text());
      el.text('Verificando...');
    }
  },
  
  hideLoading: function (el) {
    if (el && el.length && el.is('button')) {
      el.prop('disabled', false);
      var txt = el.data('original-text');
      if (txt) el.text(txt);
    }
  },

  // Método de debug para verificar el estado
  debugState: function() {
    console.log('ProductValidation Debug State:', {
      validationInProgress: this.config.validationInProgress,
      bypassValidationOnce: this.config.bypassValidationOnce,
      currentRetryCount: this.config.currentRetryCount,
      lastTriggerElement: this.config.lastTriggerElement ? this.config.lastTriggerElement[0] : null
    });
  }
};

// Inicializar
$(document).ready(function () {
  ProductValidation.init();
});

// Agregar al objeto window para debugging
window.ProductValidation = ProductValidation;