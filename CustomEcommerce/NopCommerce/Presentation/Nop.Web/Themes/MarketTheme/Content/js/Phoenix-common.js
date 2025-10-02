/* Left Sidebar  */

$(window).load(function () {
  $(function () {
    var hideFiltersText =
      window.FilterTranslations?.hideFilters || "Ocultar Filtros";
    var showFiltersText =
      window.FilterTranslations?.showFilters || "Mostrar Filtros";

    if ($(this).width() <= 991) {
      var text = $("#sidebar-button").text();
      if (text.trim() === hideFiltersText) {
        $("#sidebar-button").html(showFiltersText);
      }
    }

    $(".sidebar-button").click(function () {
      $(".generalLeftSide").toggleClass("col-sidebar");
      $(".generalSideRight").toggleClass("col-full");
      $(".generalSideRight .product-grid .item-grid").toggleClass(
        "px-full-width-grid"
      );
      var text = $("#sidebar-button").text();
      if (text.trim() === hideFiltersText) {
        $("#sidebar-button").html(showFiltersText);
      } else {
        $("#sidebar-button").html(hideFiltersText);
      }
    });

    $(window).resize(function () {
      if ($(this).width() <= 991) {
        var text = $("#sidebar-button").text();
        if (text.trim() === hideFiltersText) {
          $("#sidebar-button").html(showFiltersText);
        }
      } else {
        var text = $("#sidebar-button").text();
        if (text.trim() === showFiltersText) {
          $("#sidebar-button").html(hideFiltersText);
        }
      }
    });
  });
});

$(document).ready(function () {
  $("#topcartlink").click(function () {
    $(".flyout-cart").addClass("slideright active");
    $(".px_cart_overlay").addClass("overlayadded");
    $("body").addClass("overflowhidden");
  });

  $(".px_mini_shopping_cart_title .pi-cart-cancel").click(function () {
    $(".flyout-cart").removeClass("slideright active");
    $(".px_cart_overlay").removeClass("overlayadded");
    $("body").removeClass("overflowhidden");
  });

  $(document).on("click", function (event) {
    if ($(window).width() >= 992) {
      var $flyoutCart = $(".flyout-cart");
      var $target = $(event.target);

      if (
        $flyoutCart.hasClass("active") &&
        !$target.closest(".flyout-cart").length &&
        !$target.closest("#topcartlink").length
      ) {
        $flyoutCart.removeClass("slideright active");
        $(".px_cart_overlay").removeClass("overlayadded");
        $("body").removeClass("overflowhidden");
      }
    }
  });
});

function initCarousel(config) {
    const { 
        carouselId, 
        trackId, 
        dotsId, 
        slidesToShow, 
        isMobile = false,
        hoursId = 'hours',
        minutesId = 'minutes',
        secondsId = 'seconds'
    } = config;

    let currentSlide = 0;
    let autoPlayInterval = null;
    const autoPlayDelay = 40000;

    const carousel = document.getElementById(carouselId);
    const track = document.getElementById(trackId);
    const dotsContainer = dotsId ? document.getElementById(dotsId) : null;

    if (!track || !carousel) {
        console.warn(`Carousel ${carouselId}: Elementos no encontrados`);
        return;
    }

    const slides = track.children;
    const totalSlides = slides.length;
    const maxSlideIndex = isMobile ? totalSlides - 1 : Math.max(0, totalSlides - slidesToShow);

    if (isMobile && dotsContainer) {
        dotsContainer.innerHTML = '';
        for (let i = 0; i < totalSlides; i++) {
            const dot = document.createElement('div');
            dot.style.cssText = 'width: 8px; height: 8px; border-radius: 50%; background-color: #d1d5db; cursor: pointer; transition: all 0.3s;';
            dot.dataset.slide = i;
            dot.className = 'carousel-dot';
            dotsContainer.appendChild(dot);
        }
    }

    function updateCarousel() {
        if (isMobile) {
            const slideWidth = 80; // 80% de ancho
            const gap = 15; // gap en px
            const containerWidth = track.parentElement.offsetWidth - 30; // menos padding
            const slideWidthPx = (containerWidth * slideWidth) / 100;
            const gapPercent = (gap / containerWidth) * 100;
            const offset = 10; // 15px de padding inicial convertido a %
            
            const translateX = -(currentSlide * (slideWidth + gapPercent)) + offset;
            track.style.transform = `translateX(${translateX}%)`;
        } else {
            // Desktop: original
            const slideWidth = 100 / slidesToShow;
            const translateX = -(currentSlide * slideWidth);
            track.style.transform = `translateX(${translateX}%)`;
        }
        updateDots();
    }

    function updateDots() {
        if (!dotsContainer) return;
        const dots = dotsContainer.querySelectorAll('.carousel-dot');
        dots.forEach((dot, index) => {
            if (index === currentSlide) {
                dot.style.backgroundColor = '#667eea';
                dot.style.width = '24px';
                dot.style.borderRadius = '4px';
            } else {
                dot.style.backgroundColor = '#d1d5db';
                dot.style.width = '8px';
                dot.style.borderRadius = '50%';
            }
        });
    }

    function moveSlide(direction) {
        currentSlide += direction;

        if (currentSlide < 0) {
            currentSlide = maxSlideIndex;
        } else if (currentSlide > maxSlideIndex) {
            currentSlide = 0;
        }

        updateCarousel();
        restartAutoPlay();
    }

    function goToSlide(slideIndex) {
        currentSlide = Math.min(slideIndex, maxSlideIndex);
        updateCarousel();
        restartAutoPlay();
    }

    function startAutoPlay() {
        if (autoPlayInterval) clearInterval(autoPlayInterval);
        autoPlayInterval = setInterval(() => {
            moveSlide(1);
        }, autoPlayDelay);
    }

    function stopAutoPlay() {
        if (autoPlayInterval) {
            clearInterval(autoPlayInterval);
            autoPlayInterval = null;
        }
    }

    function restartAutoPlay() {
        stopAutoPlay();
        startAutoPlay();
    }

    // Event listeners
    carousel.addEventListener('click', (e) => {
        if (e.target.classList.contains('carousel-prev') && 
            e.target.dataset.carousel === (isMobile ? 'mobile' : 'desktop')) {
            moveSlide(-1);
        } else if (e.target.classList.contains('carousel-next') && 
                   e.target.dataset.carousel === (isMobile ? 'mobile' : 'desktop')) {
            moveSlide(1);
        } else if (e.target.classList.contains('carousel-dot')) {
            const slideIndex = parseInt(e.target.dataset.slide) || 0;
            goToSlide(slideIndex);
        }
    });

    carousel.addEventListener('mouseenter', stopAutoPlay);
    carousel.addEventListener('mouseleave', startAutoPlay);

    // Touch events
    let touchStartX = 0;
    let touchEndX = 0;

    carousel.addEventListener('touchstart', (e) => {
        touchStartX = e.changedTouches[0].screenX;
        stopAutoPlay();
    }, { passive: true });

    carousel.addEventListener('touchend', (e) => {
        touchEndX = e.changedTouches[0].screenX;
        const diff = touchStartX - touchEndX;
        const swipeThreshold = 50;

        if (Math.abs(diff) > swipeThreshold) {
            if (diff > 0) {
                moveSlide(1);
            } else {
                moveSlide(-1);
            }
        }
        startAutoPlay();
    }, { passive: true });

    updateCarousel();
    startAutoPlay();
}

// Función para el countdown
function initCountdown(hoursId, minutesId, secondsId) {
    const hoursEl = document.getElementById(hoursId);
    const minutesEl = document.getElementById(minutesId);
    const secondsEl = document.getElementById(secondsId);

    if (!hoursEl || !minutesEl || !secondsEl) {
        console.warn('Countdown: Elementos no encontrados', hoursId);
        return;
    }

    const deadline = new Date(Date.now() + 45 * 60 * 1000);

    function updateCountdown() {
        const now = Date.now();
        const distance = deadline.getTime() - now;

        if (distance < 0) {
            hoursEl.textContent = '00';
            minutesEl.textContent = '00';
            secondsEl.textContent = '00';
            return;
        }

        const hours = Math.floor(distance / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        hoursEl.textContent = String(hours).padStart(2, '0');
        minutesEl.textContent = String(minutes).padStart(2, '0');
        secondsEl.textContent = String(seconds).padStart(2, '0');
    }

    updateCountdown();
    setInterval(updateCountdown, 1000);
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}

function init() {
    // Carousel desktop
    initCarousel({
        carouselId: 'flashCarouselDesktop',
        trackId: 'carouselTrackDesktop',
        dotsId: null,
        slidesToShow: 4,
        isMobile: false
    });

    // Carousel mobile
    initCarousel({
        carouselId: 'flashCarouselMobile',
        trackId: 'carouselTrackMobile',
        dotsId: 'carouselDotsMobile',
        slidesToShow: 1,
        isMobile: true
    });

    // Countdowns
    initCountdown('hours', 'minutes', 'seconds');
    initCountdown('hours-mobile', 'minutes-mobile', 'seconds-mobile');
}