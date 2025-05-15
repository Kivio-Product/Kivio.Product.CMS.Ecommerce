var DiscountGiftCard = {
    init: function() {
        $(document).ready(function() {
            $('#discountcouponcode').on('keydown', function(event) {
                if (event.keyCode == 13) {
                    $('#applydiscountcouponcode').trigger("click");
                    return false;
                }
            });

            $('#giftcardcouponcode').on('keydown', function(event) {
                if (event.keyCode == 13) {
                    $('#applygiftcardcouponcode').trigger("click");
                    return false;
                }
            });

            $(document).on('click', '#applydiscountcouponcode', function(e) {
                e.preventDefault();
                DiscountGiftCard.applyDiscount();
            });

            $(document).on('click', '#applygiftcardcouponcode', function(e) {
                e.preventDefault();
                DiscountGiftCard.applyGiftCard();
            });

            $(document).on('click', '.remove-discount-button', function(e) {
                e.preventDefault();
                DiscountGiftCard.removeDiscount($(this).attr('name'));
            });

            $(document).on('click', '.remove-gift-card-button', function(e) {
                e.preventDefault();
                DiscountGiftCard.removeGiftCard($(this).attr('name'));
            });
        });
    },

    getAntiForgeryToken: function() {
        return $('input[name=__RequestVerificationToken]').val();
    },

    showLoadingAnimation: function() {
        Swal.fire({
            title: '',
            allowEscapeKey: false,
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    },

    applyDiscount: function() {
        var discountCode = $('#discountcouponcode').val();
        if (discountCode) {
            this.showLoadingAnimation();
            
            $.ajax({
                cache: false,
                url: '/onepagecheckoutaction/applydiscount',
                data: {
                    discountcouponcode: discountCode,
                    applydiscountcouponcode: 'true',
                    __RequestVerificationToken: this.getAntiForgeryToken()
                },
                type: 'POST',
                success: function(response) {
                    if (response.success === true || response.success === false) {
                        if (response.success) {
                            window.location.reload();
                        } else {
                            console.error("Error al aplicar descuento:", response.message);
                            
                            Swal.fire({
                                title: 'Error',
                                text: response.message || 'Error al aplicar el código de descuento',
                                icon: 'error',
                                confirmButtonColor: '#102c5a'
                            });
                        }
                    } else {
                        window.location.reload();
                    }
                },
                error: function(xhr, status, error) {
                    console.error("Error al aplicar descuento:", status, error);
                    console.error("Respuesta del servidor:", xhr.responseText);
                    
                    Swal.fire({
                        title: 'Error',
                        text: 'Ocurrió un error al aplicar el código de descuento',
                        icon: 'error',
                        confirmButtonColor: '#102c5a'
                    });
                },
                complete: function() {
                    if (Swal.isVisible()) {
                        Swal.close();
                    }
                }
            });
        }
        return false;
    },

    applyGiftCard: function() {
        var giftCardCode = $('#giftcardcouponcode').val();
        if (giftCardCode) {
            this.showLoadingAnimation();
            
            $.ajax({
                cache: false,
                url: '/onepagecheckoutaction/applygiftcard',
                data: {
                    giftcardcouponcode: giftCardCode,
                    applygiftcardcouponcode: 'true',
                    __RequestVerificationToken: this.getAntiForgeryToken()
                },
                type: 'POST',
                success: function(response) {
                    if (response.success === true || response.success === false) {
                        if (response.success) {
                            window.location.reload();
                        } else {
                            console.error("Error al aplicar tarjeta regalo:", response.message);
                            
                            Swal.fire({
                                title: 'Error',
                                text: response.message || 'Error al aplicar la tarjeta regalo',
                                icon: 'error',
                                confirmButtonColor: '#102c5a'
                            });
                        }
                    } else {
                        window.location.reload();
                    }
                },
                error: function(xhr, status, error) {
                    console.error("Error al aplicar tarjeta regalo:", status, error);
                    console.error("Respuesta del servidor:", xhr.responseText);
                    
                    Swal.fire({
                        title: 'Error',
                        text: 'Ocurrió un error al aplicar la tarjeta regalo',
                        icon: 'error',
                        confirmButtonColor: '#102c5a'
                    });
                },
                complete: function() {
                    if (Swal.isVisible()) {
                        Swal.close();
                    }
                }
            });
        }
        return false;
    },

    removeDiscount: function(buttonName) {
        this.showLoadingAnimation();
        
        $.ajax({
            cache: false,
            url: '/onepagecheckoutaction/removediscount',
            data: {
                [buttonName]: '',
                __RequestVerificationToken: this.getAntiForgeryToken()
            },
            type: 'POST',
            success: function(response) {
                if (response.success === true || response.success === false) {
                    if (response.success) {
                        $('#discount-container').html($(response.html).find('#discount-container').html());
                        window.location.reload();
                    } else {
                        console.error("Error al eliminar descuento:", response.message);
                        
                        Swal.fire({
                            title: 'Error',
                            text: response.message || 'Error al eliminar el código de descuento',
                            icon: 'error',
                            confirmButtonColor: '#102c5a'
                        });
                    }
                } else {
                    $('#discount-container').html($(response).find('#discount-container').html());
                    window.location.reload();
                }
            },
            error: function(xhr, status, error) {
                console.error("Error al eliminar descuento:", status, error);
                console.error("Respuesta del servidor:", xhr.responseText);
                
                Swal.fire({
                    title: 'Error',
                    text: 'Ocurrió un error al eliminar el código de descuento',
                    icon: 'error',
                    confirmButtonColor: '#102c5a'
                });
            },
            complete: function() {
                Swal.close();
            }
        });
        return false;
    },

    removeGiftCard: function(buttonName) {
        this.showLoadingAnimation();
        
        $.ajax({
            cache: false,
            url: '/onepagecheckoutaction/removegiftcard',
            data: {
                [buttonName]: '',
                __RequestVerificationToken: this.getAntiForgeryToken()
            },
            type: 'POST',
            success: function(response) {
                if (response.success === true || response.success === false) {
                    if (response.success) {
                        $('#giftcard-container').html($(response.html).find('#giftcard-container').html());
                        window.location.reload();
                    } else {
                        console.error("Error al eliminar tarjeta regalo:", response.message);
                        
                        Swal.fire({
                            title: 'Error',
                            text: response.message || 'Error al eliminar la tarjeta regalo',
                            icon: 'error',
                            confirmButtonColor: '#102c5a'
                        });
                    }
                } else {
                    $('#giftcard-container').html($(response).find('#giftcard-container').html());
                    window.location.reload();
                }
            },
            error: function(xhr, status, error) {
                console.error("Error al eliminar tarjeta regalo:", status, error);
                console.error("Respuesta del servidor:", xhr.responseText);
                
                Swal.fire({
                    title: 'Error',
                    text: 'Ocurrió un error al eliminar la tarjeta regalo',
                    icon: 'error',
                    confirmButtonColor: '#102c5a'
                });
            },
            complete: function() {
                Swal.close();
            }
        });
        return false;
    }
};