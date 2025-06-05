
/*
** nopCommerce ajax cart implementation
*/


var AjaxCart = {
    loadWaiting: false,
    usepopupnotifications: false,
    topcartselector: '',
    topwishlistselector: '',
    flyoutcartselector: '',
    localized_data: false,

    init: function (usepopupnotifications, topcartselector, topwishlistselector, flyoutcartselector, localized_data) {
        this.loadWaiting = false;
        this.usepopupnotifications = usepopupnotifications;
        this.topcartselector = topcartselector;
        this.topwishlistselector = topwishlistselector;
        this.flyoutcartselector = flyoutcartselector;
        this.localized_data = localized_data;
    },

    setLoadWaiting: function (display) {
        displayAjaxLoading(display);
        this.loadWaiting = display;
    },

    //add a product to the cart/wishlist from the catalog pages
    addproducttocart_catalog: function (urladd) {
        if (this.loadWaiting !== false) {
            return;
        }
        this.setLoadWaiting(true);

        var postData = {};
        addAntiForgeryToken(postData);

        $.ajax({
            cache: false,
            url: urladd,
            type: "POST",
            data: postData,
            success: this.success_process,
            complete: this.resetLoadWaiting,
            error: this.ajaxFailure
        });
    },

    //add a product to the cart/wishlist from the product details page
    addproducttocart_details: function (urladd, formselector) {
        if (this.loadWaiting !== false) {
            return;
        }
        this.setLoadWaiting(true);

        $.ajax({
            cache: false,
            url: urladd,
            data: $(formselector).serialize(),
            type: "POST",
            success: this.success_process,
            complete: this.resetLoadWaiting,
            error: this.ajaxFailure
        });
    },

    //add a product to the cart/wishlist from the product details page
    addproducttocart_details_with_redirect: function (urladd, formselector, redirect) {
        if (this.loadWaiting !== false) {
            return;
        }
        this.setLoadWaiting(true);

        return new Promise((resolve, reject) => {
            $.ajax({
                cache: false,
                url: urladd,
                data: $(formselector).serialize(),
                type: "POST",
                success: (response) => {
                    this.success_process(response);
                    resolve(response);
                    //redirect to the specified URL
                    if (redirect) {
                        window.location.href = redirect;
                    }
                },
                complete: this.resetLoadWaiting,
                error: (xhr, ajaxOptions, thrownError) => {
                    this.ajaxFailure();
                    reject(thrownError);
                }
            });
        });
    },

    //add a product to compare list
    addproducttocomparelist: function (urladd) {
        if (this.loadWaiting !== false) {
            return;
        }
        this.setLoadWaiting(true);

        var postData = {};
        addAntiForgeryToken(postData);

        $.ajax({
            cache: false,
            url: urladd,
            type: "POST",
            data: postData,
            success: this.success_process,
            complete: this.resetLoadWaiting,
            error: this.ajaxFailure
        });
    },

    removeProductFromWishlist_Catalog: function (urlremove) {
        if (this.loadWaiting !== false) {
            return;
        }
        this.setLoadWaiting(true);

        var postData = {};
        addAntiForgeryToken(postData); 

        $.ajax({
            cache: false,
            url: urlremove,
            type: "POST",
            data: postData,
            success: this.success_process,
            complete: this.resetLoadWaiting,
            error: this.ajaxFailure
        });
    },

    success_process: function (response) {
        if (response.updatetopcartsectionhtml) {
            $(AjaxCart.topcartselector).html(response.updatetopcartsectionhtml);
        }
        if (response.updatetopwishlistsectionhtml) {
            $(AjaxCart.topwishlistselector).html(response.updatetopwishlistsectionhtml);
        }
        if (response.updateflyoutcartsectionhtml) {
            $(AjaxCart.flyoutcartselector).replaceWith(response.updateflyoutcartsectionhtml);
        }

        if (typeof response.productId !== 'undefined' && typeof response.isInWishlist !== 'undefined') {
            var productButtonSelector = '.product-item[data-productid="' + response.productId + '"] .add-to-wishlist-button';
            var $button = $(productButtonSelector);
            if ($button.length > 0) {
                if (response.isInWishlist) {
                    $button.addClass('selected'); 
                } else {
                    $button.removeClass('selected');
                }
            }
        }
        if (response.message) {
            //display notification
            if (response.success === true) {
                //success
                if (AjaxCart.usepopupnotifications === true) {
                    displayPopupNotification(response.message, 'success', true);
                }
                else {
                    //specify timeout for success messages
                    displayBarNotification(response.message, 'success', 3500);
                }
            }
            else {
                //error
                if (AjaxCart.usepopupnotifications === true) {
                    displayPopupNotification(response.message, 'error', true);
                }
                else {
                    //no timeout for errors
                    displayBarNotification(response.message, 'error', 0);
                }
            }
            return false;
        }
        if (response.redirect) {
            location.href = response.redirect;
            return true;
        }
        return false;
    },

    resetLoadWaiting: function () {
        AjaxCart.setLoadWaiting(false);
    },

    ajaxFailure: function () {
        alert(this.localized_data.AjaxCartFailure);
    }
};