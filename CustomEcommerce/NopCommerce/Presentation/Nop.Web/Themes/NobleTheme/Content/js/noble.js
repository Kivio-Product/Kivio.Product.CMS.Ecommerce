/*!
 * nopAccelerate Noble Theme v2.8.0 (http://themes.nopaccelerate.com/themes/nopaccelerate-noble-theme/)
 * Copyright 2025 Xcellence-IT.
 * Licensed under http://www.nopaccelerate.com/terms/
 */

/* Using Noble js for nopAccelerate Noble Theme */

$(function () {
    //Used js for Header Sticky Menu  
    $(window).on('scroll', function () {
        const $header = $('div.header');
        const navHeight = $header.outerHeight(); // Use `outerHeight` for better accuracy
        const navWidth = $header.outerWidth();
        const $mainMenu = $('.main-menu');

        if ($(window).scrollTop() > navHeight) {
            $mainMenu.addClass('goToTop').css('width', navWidth);
        } else {
            $mainMenu.removeClass('goToTop').css('width', '');
        }
    });

    //Used js for Left Sliderbar Collapse(Responsive Devices)
    $('.block .title').on('click', function () {
        const viewportWidth = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;

        if (viewportWidth < 992) {
            $(this).siblings('.listbox').slideToggle('slow');
            $(this).toggleClass('arrow-up-down');
        }
    });

    //Used js for Tooltip 
    $('[data-toggle="tooltip"]').tooltip();
    $('[data-toggle="tooltip"]').on('shown.bs.tooltip', function() {
        $('.tooltip').addClass('animated swing');
    })

    //Used js for Product Box and Product Thumbs Slider
    // Common Slider Settings
    const sliderSettings = {
        loop: false,
        dots: false,
        nav: true,
        navText: ["prev", "next"],
        autoplay: true, // Correct case for Owl Carousel
        lazyLoad: true,
        responsive: {
            0: { items: 1 },
            640: { items: 1 },
            768: { items: 3 },
            980: { items: 3 },
            1199: { items: 3 }
        }
    };

    // Initialize Sliders with Shared Settings
    $('#home-category-slider, #home-bestseller-slider, #home-features-slider, #related-products-slider, #also-purchased-products-slider, #category-featured-slider, #manufacturer-featured-slider').owlCarousel(sliderSettings);

    $('#product-slider').owlCarousel({
        loop: false,
        dots: false,
        nav: true,
        navText: ["prev", "next"],
        autoPlay: true,
        lazyLoad: true,
        responsive: {
            0: {
                items: 1
            },
            640: {
                items: 2
            },
            768: {
                items: 3
            },
            980: {
                items: 3
            },
            1199: {
                items: 3
            }
        }
    });

    $('#crosssell-products-slider').owlCarousel({
        loop: false,
        dots: false,
        nav: true,
        navText: ["prev", "next"],
        autoPlay: true,
        lazyLoad: true,
        responsive: {
            0: {
                items: 1
            },
            640: {
                items: 1
            },
            768: {
                items: 3
            },
            980: {
                items: 3
            },
            1199: {
                items: 4
            }
        }
    });
});
