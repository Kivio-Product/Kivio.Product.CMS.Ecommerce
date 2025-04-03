/* Left Sidebar  */


$(window).load(function () {
    $(function () {
        if ($(this).width() <= 991) {
            var text = $('#sidebar-button').text();
            if (text.trim() === 'Hide Filters') {
                $('#sidebar-button').html('Show Filters');
            }
        }
        $(".sidebar-button").click(function () {
            $(".generalLeftSide").toggleClass("col-sidebar");
            $(".generalSideRight").toggleClass("col-full");
            var text = $('#sidebar-button').text();
            if (text.trim() === 'Hide Filters') {
                $('#sidebar-button').html('Show Filters');
            }
            else {
                $('#sidebar-button').html('Hide Filters');
            }
        });

        $(window).resize(function () {
            if ($(this).width() <= 991) {
                var text = $('#sidebar-button').text();
                if (text.trim() === 'Hide Filters') {
                    $('#sidebar-button').html('Show Filters');
                }
            }
            else {
                var text = $('#sidebar-button').text();
                if (text.trim() === 'Show Filters') {
                    $('#sidebar-button').html('Hide Filters');
                }
            }
        });

    });
});

$('.sidebar-button').click(function () {
    $('.sidebar-button').toggleClass("filtershow");

});


// CART

$(document).ready(function () {
    $('#topcartlink').click(function () {
        $('.flyout-cart').addClass("slideright active");
        $('.vult_cart_overlay').addClass("overlayadded");
        $('body').addClass("overflowhidden");
    });

    $('.vult_mini_shopping_cart_title .Vult-close').click(function () {
        $('.flyout-cart').removeClass("slideright active");
        $('.vult_cart_overlay').removeClass("overlayadded");
        $('body').removeClass("overflowhidden");
    });
});


// SELECT JS

$(document).ready(function () {
    $(".vult-common-select").select2({
        minimumResultsForSearch: -1,
        allowClear: true,
        dropdownCssClass: "vult-dropdown-select-height"
    });
    $(".vult-common-select2").select2({
        minimumResultsForSearch: -1,
        allowClear: true,
        dropdownCssClass: "vult-dropdown-select-height"
    });
});
