/**
 * Product Box Cart Animation
 * Maneja la animación y funcionalidad del carrito en las cajas de producto
 * @version 2.2.2
 * @author Kivio SAS
 */

(function() {
    'use strict';
    
    if (window.ProductBoxCartAnimation && window.ProductBoxCartAnimation.initialized) {
        return;
    }
    
    window.ProductBoxCartAnimation = {
        initialized: true,
        productQuantities: {},
        cartItemIds: {} 
    };
    
    var PBCart = window.ProductBoxCartAnimation;
    
    var DEFAULT_MESSAGES = {
        maxQuantityReached: '⚠️ No puedes agregar más unidades. La cantidad seleccionada supera el stock disponible.',
        maxQuantityTitle: 'Límite de cantidad alcanzado',
        updateError: 'No se pudo actualizar la cantidad',
        stockError: 'Stock insuficiente'
    };
    
    PBCart.getMessage = function(key) {
        if (window.CartMessages && window.CartMessages[key]) {
            return window.CartMessages[key];
        }
        return DEFAULT_MESSAGES[key] || '';
    };
    
    PBCart.showLimitPopup = function(message, title) {
        message = message || PBCart.getMessage('maxQuantityReached');
        title = title || PBCart.getMessage('maxQuantityTitle');
                
        if (typeof displayBarNotification !== 'undefined') {
            try {
                displayBarNotification(message, 'error', 3500);
                setTimeout(function() { PBCart.showCustomPopup(message, title); }, 100);
                return;
            } catch(e) {
            }
        }
        
        PBCart.showCustomPopup(message, title);
    };
    
    PBCart.showCustomPopup = function(message, title) {
        $('.cart-limit-popup, .cart-limit-overlay').remove();
        
        var popupHtml = '<div class="cart-limit-popup" style="'
            + 'position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); '
            + 'background: white; padding: 30px; border-radius: 8px; '
            + 'box-shadow: 0 4px 20px rgba(0,0,0,0.15); z-index: 10000; '
            + 'max-width: 400px; text-align: center; opacity: 0;">'
            + '<h3 style="margin: 0 0 15px 0; color: #7a37f0; font-size: 18px;">' + title + '</h3>'
            + '<p style="margin: 0 0 20px 0; color: #333; font-size: 14px; line-height: 1.5;">' + message + '</p>'
            + '<button class="close-popup" style="background: #7a37f0; color: white; border: none; padding: 10px 30px; border-radius: 4px; cursor: pointer; font-size: 14px; font-weight: 500;">'
            + 'Entendido'
            + '</button>'
            + '</div>';
        
        var overlayHtml = '<div class="cart-limit-overlay" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 9999; opacity: 0;"></div>';
        
        $('body').append(overlayHtml + popupHtml);
        
        setTimeout(function() {
            $('.cart-limit-overlay').css('transition', 'opacity 0.3s ease');
            $('.cart-limit-popup').css('transition', 'opacity 0.3s ease');
            $('.cart-limit-overlay').css('opacity', '1');
            $('.cart-limit-popup').css('opacity', '1');
        }, 10);
        
        $('.close-popup, .cart-limit-overlay').on('click', function() {
            $('.cart-limit-popup, .cart-limit-overlay').css('opacity', '0');
            setTimeout(function() { $('.cart-limit-popup, .cart-limit-overlay').remove(); }, 300);
        });
    };
    
    var initWhenReady = function() {
        if (typeof $ === 'undefined' || typeof AjaxCart === 'undefined') {
            setTimeout(initWhenReady, 100);
            return;
        }
        init();
    };
    
    var init = function() {
        var originalSuccessProcess = AjaxCart.success_process;
        
        AjaxCart.success_process = function(response) {
            var result = originalSuccessProcess.call(AjaxCart, response);
            
            if (response && response.success && PBCart.lastAddedProductId) {
                var productId = PBCart.lastAddedProductId;
                
                PBCart.fetchCartQuantityForProduct(productId, function(quantity, itemId, maxQtyFromServer) {
                    var $wrapper = $('.cart-action-wrapper[data-productid="' + productId + '"]');
                    
                    if ($wrapper.length) {
                        if (itemId) { PBCart.cartItemIds[productId] = itemId; }
                        
                        var maxQuantity = maxQtyFromServer || parseInt($wrapper.attr('data-max-quantity')) || 5;
                        if (maxQtyFromServer) { $wrapper.attr('data-max-quantity', maxQtyFromServer); }
                        
                        PBCart.productQuantities[productId] = quantity;
                        
                        var $selector = $wrapper.find('.cart-quantity-selector');
                        var isVisible = $selector.hasClass('slide-in');
                        
                        if (isVisible) {
                            PBCart.updateQuantityDisplay($wrapper, quantity);
                            PBCart.toggleButtons($wrapper, quantity, maxQuantity);
                        } else {
                            setTimeout(function() { PBCart.showQuantitySelector($wrapper, quantity, maxQuantity); }, 300);
                        }
                    }
                    
                    PBCart.lastAddedProductId = null;
                });
            }
            
            return result;
        };
        
        $(document)
            .off('click.pbcart')
            .on('click.pbcart', '.add-to-cart-initial', function(e) {
                e.preventDefault();
                var $button = $(this);
                var $wrapper = $button.closest('.cart-action-wrapper');
                var addUrl = $wrapper.data('addurl');
                var productId = $wrapper.data('productid');
                
                if (!addUrl) { return false; }
                
                
                PBCart.fetchCartQuantityForProduct(productId, function(currentQty, itemId, maxQtyFromServer) {
                    
                    var maxQuantity = maxQtyFromServer || parseInt($wrapper.attr('data-max-quantity')) || 5;
                    if (maxQtyFromServer) { $wrapper.attr('data-max-quantity', maxQtyFromServer); }
                    if (itemId) { PBCart.cartItemIds[productId] = itemId; }
                    PBCart.productQuantities[productId] = currentQty;
                    
                    if (currentQty >= maxQuantity) {
                        PBCart.showLimitPopup();
                        return;
                    }
                    
                    if (currentQty > 0) {
                        var $selector = $wrapper.find('.cart-quantity-selector');
                        var isVisible = $selector.hasClass('slide-in');
                        if (!isVisible) { PBCart.showQuantitySelector($wrapper, currentQty, maxQuantity); }
                        return;
                    }
                    
                    PBCart.lastAddedProductId = productId;
                    
                    var url = addUrl;
                    var urlParts = url.split('/');
                    if (urlParts.length >= 5) {
                        urlParts[urlParts.length - 1] = '1';
                        url = urlParts.join('/');
                    }
                    
                    if (typeof AjaxCart !== 'undefined' && AjaxCart.addproducttocart_catalog) {
                        AjaxCart.addproducttocart_catalog(url);
                    }
                });
                
                return false;
            })
            
            .off('click.pbcart-disabled')
            .on('click.pbcart-disabled', '.qty-control-btn[data-action="increase"].disabled', function(e) {
                e.preventDefault();
                e.stopPropagation();
                PBCart.showLimitPopup();
                return false;
            })
            
            .off('click.pbcart-ctrl')
            .on('click.pbcart-ctrl', '.qty-control-btn', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                var $button = $(this);
                var action = $button.data('action');
                var $wrapper = $button.closest('.cart-action-wrapper');
                var productId = $wrapper.data('productid');
                var $quantityDisplay = $wrapper.find('.quantity-display');
                var currentQty = parseInt(($quantityDisplay.text().match(/\d+/) || [1])[0]) || 1;
                var maxQuantity = parseInt($wrapper.attr('data-max-quantity')) || 5;
                
                if (action === 'increase' && $button.hasClass('disabled')) {
                    PBCart.showLimitPopup();
                    return false;
                }
                
                if ($button.hasClass('updating')) { return false; }
                $wrapper.find('.qty-control-btn').addClass('updating');
                
                if (action === 'increase') {
                    PBCart.handleIncrease($wrapper, productId, currentQty, maxQuantity);
                } else if (action === 'decrease') {
                    PBCart.handleDecrease($wrapper, productId, currentQty, maxQuantity);
                } else if (action === 'remove') {
                    PBCart.handleRemove($wrapper, productId);
                }
                
                return false;
            });
        
    };
    
    PBCart.fetchCartQuantityForProduct = function(productId, callback) {
        $.ajax({
            cache: false,
            url: '/shoppingcart/getproductquantity',
            data: { productId: productId },
            type: 'GET',
            success: function(response) {
                if (response.success) {
                    callback(response.quantity || 0, response.itemId, response.maxQuantity);
                } else {
                    callback(0, null, null);
                }
            },
            error: function(xhr, status, error) {
                callback(0, null, null);
            }
        });
    };
    
    PBCart.showQuantitySelector = function($wrapper, quantity, maxQuantity) {
        quantity = quantity || 1;
        maxQuantity = maxQuantity || parseInt($wrapper.attr('data-max-quantity')) || 5;
        
        var $button = $wrapper.find('.add-to-cart-initial');
        var $selector = $wrapper.find('.cart-quantity-selector');
        var productId = $wrapper.data('productid');
        
        PBCart.productQuantities[productId] = quantity;
        
        $button.addClass('slide-out');
        
        setTimeout(function() {
            $selector.addClass('slide-in');
            PBCart.updateQuantityDisplay($wrapper, quantity);
            PBCart.toggleButtons($wrapper, quantity, maxQuantity);
        }, 400);
    };
    
    PBCart.handleIncrease = function($wrapper, productId, currentQty, maxQuantity) {
        
        var itemId = PBCart.cartItemIds[productId];
        if (!maxQuantity || isNaN(maxQuantity)) { maxQuantity = parseInt($wrapper.attr('data-max-quantity')) || 5; }
        
        if (currentQty >= maxQuantity) {
            $wrapper.find('.qty-control-btn').removeClass('updating');
            PBCart.showLimitPopup();
            return;
        }
        
        if (!itemId) {
            PBCart.fetchCartQuantityForProduct(productId, function(quantity, fetchedItemId, maxQty) {
                if (fetchedItemId) {
                    PBCart.cartItemIds[productId] = fetchedItemId;
                    if (maxQty) { $wrapper.attr('data-max-quantity', maxQty); maxQuantity = maxQty; }
                    PBCart.handleIncrease($wrapper, productId, currentQty, maxQuantity);
                } else {
                    $wrapper.find('.qty-control-btn').removeClass('updating');
                }
            });
            return;
        }
        
        var newQty = currentQty + 1;
        
        PBCart.productQuantities[productId] = newQty;
        PBCart.updateQuantityDisplay($wrapper, newQty);
        PBCart.toggleButtons($wrapper, newQty, maxQuantity);
        
        $.ajax({
            cache: false,
            url: '/shoppingcart/updatecartitemquantity',
            data: {
                itemId: itemId,
                quantity: newQty,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            type: 'POST',
            success: function(response) {
                if (response.success) {
                    if (response.updatetopcartsectionhtml) { $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml); }
                    if (response.updateflyoutcartsectionhtml) { $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml); }
                    if (!response.updatetopcartsectionhtml || !response.updateflyoutcartsectionhtml) { PBCart.refreshMiniCart(); }
                    setTimeout(function() { PBCart.toggleButtons($wrapper, newQty, maxQuantity); }, 100);
                } else {
                    PBCart.productQuantities[productId] = currentQty;
                    PBCart.updateQuantityDisplay($wrapper, currentQty);
                    PBCart.toggleButtons($wrapper, currentQty, maxQuantity);
                    var errorMsg = response.message || PBCart.getMessage('updateError');
                    var errorTitle = PBCart.getMessage('stockError');
                    PBCart.showLimitPopup(errorMsg, errorTitle);
                }
            },
            error: function() {
                PBCart.productQuantities[productId] = currentQty;
                PBCart.updateQuantityDisplay($wrapper, currentQty);
                PBCart.toggleButtons($wrapper, currentQty, maxQuantity);
            },
            complete: function() {
                $wrapper.find('.qty-control-btn').removeClass('updating');
            }
        });
    };
    
    PBCart.handleDecrease = function($wrapper, productId, currentQty, maxQuantity) {
        var itemId = PBCart.cartItemIds[productId];
        if (!maxQuantity || isNaN(maxQuantity)) { maxQuantity = parseInt($wrapper.attr('data-max-quantity')) || 5; }
        if (!itemId) { $wrapper.find('.qty-control-btn').removeClass('updating'); return; }
        if (currentQty <= 1) { $wrapper.find('.qty-control-btn').removeClass('updating'); return; }
        
        var newQty = currentQty - 1;
        PBCart.productQuantities[productId] = newQty;
        PBCart.updateQuantityDisplay($wrapper, newQty);
        PBCart.toggleButtons($wrapper, newQty, maxQuantity);
        
        $.ajax({
            cache: false,
            url: '/shoppingcart/updatecartitemquantity',
            data: {
                itemId: itemId,
                quantity: newQty,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            type: 'POST',
            success: function(response) {
                if (response.success) {
                    if (response.updatetopcartsectionhtml) { $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml); }
                    if (response.updateflyoutcartsectionhtml) { $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml); }
                    setTimeout(function() { PBCart.toggleButtons($wrapper, newQty, maxQuantity); }, 100);
                } else {
                    PBCart.productQuantities[productId] = currentQty;
                    PBCart.updateQuantityDisplay($wrapper, currentQty);
                    PBCart.toggleButtons($wrapper, currentQty, maxQuantity);
                }
            },
            error: function() {
                PBCart.productQuantities[productId] = currentQty;
                PBCart.updateQuantityDisplay($wrapper, currentQty);
                PBCart.toggleButtons($wrapper, currentQty, maxQuantity);
            },
            complete: function() { $wrapper.find('.qty-control-btn').removeClass('updating'); }
        });
    };
    
        PBCart.handleRemove = function($wrapper, productId) {
        var itemId = PBCart.cartItemIds[productId];
        if (!itemId) { $wrapper.find('.qty-control-btn').removeClass('updating'); return; }
        
        var $selector = $wrapper.find('.cart-quantity-selector');
        var $button = $wrapper.find('.add-to-cart-initial');
        
        $.ajax({
            cache: false,
            url: '/shoppingcart/removecartitem',
            data: { itemId: itemId, __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val() },
            type: 'POST',
            success: function(response) {
                if (response.success) {
                    delete PBCart.productQuantities[productId];
                    delete PBCart.cartItemIds[productId];
                    
                    $selector.removeClass('slide-in');
                    setTimeout(function() { $button.removeClass('slide-out'); }, 400);
                    
                    if (response.updatetopcartsectionhtml) { $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml); }
                    if (response.updateflyoutcartsectionhtml) { $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml); }
                } else {
                    $button.addClass('slide-out');
                    setTimeout(function() { $selector.addClass('slide-in'); }, 50);
                }
            },
            error: function() {
                $button.addClass('slide-out');
                setTimeout(function() { $selector.addClass('slide-in'); }, 50);
            },
            complete: function() { $wrapper.find('.qty-control-btn').removeClass('updating'); }
        });
    };
    
    PBCart.updateQuantityDisplay = function($wrapper, quantity) {
        var $quantityDisplay = $wrapper.find('.quantity-display');
        $quantityDisplay.text(quantity + ' und.');
    };
    
    PBCart.toggleButtons = function($wrapper, quantity, maxQuantity) {
        var $removeBtn = $wrapper.find('.remove-btn');
        var $decreaseBtn = $wrapper.find('.decrease-btn');
        var $increaseBtn = $wrapper.find('.increase-btn');
        
        if (!maxQuantity || isNaN(maxQuantity)) {
            maxQuantity = parseInt($wrapper.attr('data-max-quantity'));
            if (!maxQuantity || isNaN(maxQuantity)) { maxQuantity = 5; }
        }
        
        if (quantity === 1) {
            $removeBtn.removeClass('hidden').show();
            $decreaseBtn.addClass('hidden').hide();
        } else {
            $removeBtn.addClass('hidden').hide();
            $decreaseBtn.removeClass('hidden').show();
        }
        
        var shouldDisable = (quantity >= maxQuantity);
        
        if (shouldDisable) {
            $increaseBtn
                .addClass('disabled')
                .attr('aria-disabled', 'true')
                .css({
                    'opacity': '0.5',
                    'cursor': 'not-allowed',
                    'pointer-events': 'auto' 
                });
        } else {
            $increaseBtn
                .removeClass('disabled')
                .removeAttr('aria-disabled')
                .removeAttr('disabled') 
                .prop('disabled', false) 
                .css({
                    'opacity': '1',
                    'cursor': 'pointer',
                    'pointer-events': 'auto'
                });
        }
        
        setTimeout(function() {
        }, 50);
    };
    
    // Refrescar mini cart
    PBCart.refreshMiniCart = function() {
        $.ajax({
            cache: false,
            url: '/shoppingcart/GetMiniShoppingCart',
            type: 'GET',
            success: function(data) {
                if (data.TotalProducts !== undefined) {
                    $(AjaxCart.topcartselector).html(data.TotalProducts.toString());
                }
                if (data.Html) { $(AjaxCart.flyoutcartselector).replaceWith(data.Html); }
            },
            error: function() { console.error('Failed to refresh mini cart'); }
        });
    };
    
    // Iniciar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initWhenReady);
    } else {
        initWhenReady();
    }
    
})();
