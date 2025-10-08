/**
 * Product Box Cart Animation
 * Maneja la animación y funcionalidad del carrito en las cajas de producto
 * @version 2.0.0
 */

(function() {
    'use strict';
    
    // Evitar inicialización múltiple
    if (window.ProductBoxCartAnimation && window.ProductBoxCartAnimation.initialized) {
        console.log('ProductBoxCartAnimation already initialized');
        return;
    }
    
    // Namespace para evitar conflictos
    window.ProductBoxCartAnimation = {
        initialized: true,
        productQuantities: {},
        cartItemIds: {} // Mapear productId -> cartItemId
    };
    
    var PBCart = window.ProductBoxCartAnimation;
    
    console.log('Initializing ProductBoxCartAnimation...');
    
    // Esperar a que jQuery y AjaxCart estén disponibles
    var initWhenReady = function() {
        if (typeof $ === 'undefined' || typeof AjaxCart === 'undefined') {
            console.log('Waiting for jQuery and AjaxCart...');
            setTimeout(initWhenReady, 100);
            return;
        }
        
        init();
    };
    
    var init = function() {
        // Guardar la función original de success_process
        var originalSuccessProcess = AjaxCart.success_process;
        
        // Override de success_process para capturar cuando se agrega exitosamente
        AjaxCart.success_process = function(response) {
            // Llamar a la función original primero
            var result = originalSuccessProcess.call(AjaxCart, response);
            
            // Si fue exitoso
            if (response && response.success && PBCart.lastAddedProductId) {
                var productId = PBCart.lastAddedProductId;
                console.log('Product added successfully:', productId);
                
                // Obtener la cantidad real del carrito después de agregar
                PBCart.fetchCartQuantityForProduct(productId, function(quantity, itemId) {
                    var $wrapper = $('.cart-action-wrapper[data-productid="' + productId + '"]');
                    
                    if ($wrapper.length) {
                        // Guardar itemId para futuras operaciones
                        if (itemId) {
                            PBCart.cartItemIds[productId] = itemId;
                            console.log('Saved itemId for product', productId, ':', itemId);
                        }
                        PBCart.productQuantities[productId] = quantity;
                        
                        // Verificar si ya está visible
                        var $selector = $wrapper.find('.cart-quantity-selector');
                        var isVisible = $selector.hasClass('slide-in');
                        
                        if (isVisible) {
                            // Solo actualizar la cantidad
                            PBCart.updateQuantityDisplay($wrapper, quantity);
                            PBCart.toggleButtons($wrapper, quantity);
                        } else {
                            // Mostrar por primera vez con la cantidad correcta
                            setTimeout(function() {
                                PBCart.showQuantitySelector($wrapper, quantity);
                            }, 300);
                        }
                    }
                    
                    // Limpiar el flag
                    PBCart.lastAddedProductId = null;
                });
            }
            
            return result;
        };
        
        console.log('AjaxCart.success_process overridden successfully');
        
        // Event delegation para los botones - UNA SOLA VEZ
        $(document).off('click.pbcart').on('click.pbcart', '.add-to-cart-initial', function(e) {
            e.preventDefault();
            var $button = $(this);
            var $wrapper = $button.closest('.cart-action-wrapper');
            var addUrl = $wrapper.data('addurl');
            var productId = $wrapper.data('productid');
            
            if (!addUrl) {
                console.error('No add URL found');
                return false;
            }
            
            // Guardar el productId para usarlo después en el callback
            PBCart.lastAddedProductId = productId;
            
            // Construir URL con cantidad 1
            var url = addUrl;
            var urlParts = url.split('/');
            if (urlParts.length >= 5) {
                urlParts[urlParts.length - 1] = '1';
                url = urlParts.join('/');
            }
            
            console.log('Adding to cart with URL:', url);
            
            // Llamar a AjaxCart usando la función original
            if (typeof AjaxCart !== 'undefined' && AjaxCart.addproducttocart_catalog) {
                AjaxCart.addproducttocart_catalog(url);
            }
            
            return false;
        });
        
        // Event delegation para botones de control
        $(document).off('click.pbcart-ctrl').on('click.pbcart-ctrl', '.qty-control-btn', function(e) {
            e.preventDefault();
            
            // Prevenir múltiples clicks
            if ($(this).hasClass('updating')) {
                console.log('Button is updating, ignoring click');
                return false;
            }
            
            var $button = $(this);
            var action = $button.data('action');
            var $wrapper = $button.closest('.cart-action-wrapper');
            var productId = $wrapper.data('productid');
            var $quantityDisplay = $wrapper.find('.quantity-display');
            var currentQty = parseInt($quantityDisplay.text().match(/\d+/)[0]) || 1;
            
            console.log('Control button clicked:', {
                action: action,
                productId: productId,
                currentQty: currentQty,
                hasItemId: !!PBCart.cartItemIds[productId]
            });
            
            // Marcar como actualizando
            $wrapper.find('.qty-control-btn').addClass('updating');
            
            if (action === 'increase') {
                PBCart.handleIncrease($wrapper, productId, currentQty);
            } else if (action === 'decrease') {
                PBCart.handleDecrease($wrapper, productId, currentQty);
            } else if (action === 'remove') {
                PBCart.handleRemove($wrapper, productId);
            }
            
            return false;
        });
        
        console.log('ProductBoxCartAnimation initialized successfully');
    };
    
    // Función para obtener la cantidad real del producto en el carrito
    PBCart.fetchCartQuantityForProduct = function(productId, callback) {
        console.log('Fetching cart quantity for product:', productId);
        
        $.ajax({
            cache: false,
            url: '/shoppingcart/getproductquantity',
            data: { productId: productId },
            type: 'GET',
            success: function(response) {
                console.log('Cart quantity response:', response);
                
                if (response.success) {
                    callback(response.quantity || 1, response.itemId);
                } else {
                    console.error('Failed to get cart quantity:', response.message);
                    callback(1, null);
                }
            },
            error: function(xhr, status, error) {
                console.error('Error fetching cart quantity:', error);
                callback(1, null);
            }
        });
    };
    
    // Funciones del módulo
    PBCart.showQuantitySelector = function($wrapper, quantity) {
        quantity = quantity || 1;
        var $button = $wrapper.find('.add-to-cart-initial');
        var $selector = $wrapper.find('.cart-quantity-selector');
        var productId = $wrapper.data('productid');
        
        PBCart.productQuantities[productId] = quantity;
        
        $button.addClass('slide-out');
        
        setTimeout(function() {
            $selector.addClass('slide-in');
            PBCart.updateQuantityDisplay($wrapper, quantity);
            PBCart.toggleButtons($wrapper, quantity);
        }, 400);
    };
    
    PBCart.handleIncrease = function($wrapper, productId, currentQty) {
        var itemId = PBCart.cartItemIds[productId];
        
        console.log('handleIncrease called:', {
            productId: productId,
            itemId: itemId,
            currentQty: currentQty,
            allItemIds: PBCart.cartItemIds
        });
        
        if (!itemId) {
            console.error('No itemId found for product:', productId);
            console.log('Attempting to fetch from server...');
            
            // Intentar obtener el itemId del servidor
            PBCart.fetchCartQuantityForProduct(productId, function(quantity, fetchedItemId) {
                if (fetchedItemId) {
                    console.log('ItemId fetched successfully:', fetchedItemId);
                    PBCart.cartItemIds[productId] = fetchedItemId;
                    // Reintentar la operación
                    PBCart.handleIncrease($wrapper, productId, currentQty);
                } else {
                    console.error('Could not fetch itemId from server');
                    $wrapper.find('.qty-control-btn').removeClass('updating');
                }
            });
            return;
        }
        
        var newQty = currentQty + 1;
        
        console.log('Updating quantity to:', newQty);
        
        // Actualizar UI optimísticamente
        PBCart.productQuantities[productId] = newQty;
        PBCart.updateQuantityDisplay($wrapper, newQty);
        PBCart.toggleButtons($wrapper, newQty);
        
        // Llamar al endpoint para actualizar
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
                console.log('Update response:', response);
                
                if (response.success) {
                    console.log('Quantity increased successfully');
                    
                    // Actualizar contador del carrito
                    if (response.updatetopcartsectionhtml) {
                        $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml);
                        console.log('Updated top cart section with:', response.updatetopcartsectionhtml);
                    }
                    
                    // Actualizar flyout cart
                    if (response.updateflyoutcartsectionhtml) {
                        $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml);
                        console.log('Updated flyout cart section');
                    }
                    
                    // Si no vienen en la respuesta, refrescar manualmente el mini cart
                    if (!response.updatetopcartsectionhtml || !response.updateflyoutcartsectionhtml) {
                        console.log('Missing cart HTML in response, fetching manually...');
                        PBCart.refreshMiniCart();
                    }
                } else {
                    // Revertir cambio
                    console.error('Failed to increase quantity:', response.message);
                    PBCart.productQuantities[productId] = currentQty;
                    PBCart.updateQuantityDisplay($wrapper, currentQty);
                    PBCart.toggleButtons($wrapper, currentQty);
                }
            },
            error: function(xhr, status, error) {
                // Revertir cambio
                console.error('Error increasing quantity:', error);
                PBCart.productQuantities[productId] = currentQty;
                PBCart.updateQuantityDisplay($wrapper, currentQty);
                PBCart.toggleButtons($wrapper, currentQty);
            },
            complete: function() {
                $wrapper.find('.qty-control-btn').removeClass('updating');
            }
        });
    };
    
    PBCart.handleDecrease = function($wrapper, productId, currentQty) {
        var itemId = PBCart.cartItemIds[productId];
        
        if (!itemId) {
            console.error('No itemId found for product:', productId);
            $wrapper.find('.qty-control-btn').removeClass('updating');
            return;
        }
        
        if (currentQty <= 1) {
            $wrapper.find('.qty-control-btn').removeClass('updating');
            return;
        }
        
        var newQty = currentQty - 1;
        
        // Actualizar UI optimísticamente
        PBCart.productQuantities[productId] = newQty;
        PBCart.updateQuantityDisplay($wrapper, newQty);
        PBCart.toggleButtons($wrapper, newQty);
        
        // Llamar al endpoint para actualizar
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
                    console.log('Quantity decreased successfully');
                    // Actualizar mini cart
                    if (response.updatetopcartsectionhtml) {
                        $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml);
                    }
                    if (response.updateflyoutcartsectionhtml) {
                        $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml);
                    }
                } else {
                    // Revertir cambio
                    PBCart.productQuantities[productId] = currentQty;
                    PBCart.updateQuantityDisplay($wrapper, currentQty);
                    PBCart.toggleButtons($wrapper, currentQty);
                    console.error('Failed to decrease quantity:', response.message);
                }
            },
            error: function() {
                // Revertir cambio
                PBCart.productQuantities[productId] = currentQty;
                PBCart.updateQuantityDisplay($wrapper, currentQty);
                PBCart.toggleButtons($wrapper, currentQty);
                console.error('Error decreasing quantity');
            },
            complete: function() {
                $wrapper.find('.qty-control-btn').removeClass('updating');
            }
        });
    };
    
    PBCart.handleRemove = function($wrapper, productId) {
        var itemId = PBCart.cartItemIds[productId];
        
        if (!itemId) {
            console.error('No itemId found for product:', productId);
            $wrapper.find('.qty-control-btn').removeClass('updating');
            return;
        }
        
        // Animar salida
        var $selector = $wrapper.find('.cart-quantity-selector');
        var $button = $wrapper.find('.add-to-cart-initial');
        
        $selector.removeClass('slide-in');
        
        setTimeout(function() {
            $button.removeClass('slide-out');
        }, 400);
        
        // Llamar al endpoint para eliminar
        $.ajax({
            cache: false,
            url: '/shoppingcart/removecartitem',
            data: {
                itemId: itemId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            type: 'POST',
            success: function(response) {
                if (response.success) {
                    console.log('Item removed successfully');
                    delete PBCart.productQuantities[productId];
                    delete PBCart.cartItemIds[productId];
                    
                    // Actualizar mini cart
                    if (response.updatetopcartsectionhtml) {
                        $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml);
                    }
                    if (response.updateflyoutcartsectionhtml) {
                        $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml);
                    }
                } else {
                    // Revertir animación
                    $button.addClass('slide-out');
                    setTimeout(function() {
                        $selector.addClass('slide-in');
                    }, 50);
                    console.error('Failed to remove item:', response.message);
                }
            },
            error: function() {
                // Revertir animación
                $button.addClass('slide-out');
                setTimeout(function() {
                    $selector.addClass('slide-in');
                }, 50);
                console.error('Error removing item');
            },
            complete: function() {
                $wrapper.find('.qty-control-btn').removeClass('updating');
            }
        });
    };
    
    PBCart.updateQuantityDisplay = function($wrapper, quantity) {
        var $quantityDisplay = $wrapper.find('.quantity-display');
        $quantityDisplay.text(quantity + ' und.');
    };
    
    PBCart.toggleButtons = function($wrapper, quantity) {
        var $removeBtn = $wrapper.find('.remove-btn');
        var $decreaseBtn = $wrapper.find('.decrease-btn');
        
        if (quantity === 1) {
            $removeBtn.removeClass('hidden');
            $decreaseBtn.addClass('hidden');
        } else {
            $removeBtn.addClass('hidden');
            $decreaseBtn.removeClass('hidden');
        }
    };
    
    // Función para refrescar el mini cart manualmente
    PBCart.refreshMiniCart = function() {
        $.ajax({
            cache: false,
            url: '/shoppingcart/GetMiniShoppingCart',
            type: 'GET',
            success: function(data) {
                console.log('Mini cart refreshed');
                
                // Actualizar el contador del carrito
                if (data.TotalProducts !== undefined) {
                    $(AjaxCart.topcartselector).html(data.TotalProducts.toString());
                }
                
                // Actualizar el flyout del carrito si está disponible
                if (data.Html) {
                    $(AjaxCart.flyoutcartselector).replaceWith(data.Html);
                }
            },
            error: function() {
                console.error('Failed to refresh mini cart');
            }
        });
    };
    
    // Iniciar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initWhenReady);
    } else {
        initWhenReady();
    }
    
})();