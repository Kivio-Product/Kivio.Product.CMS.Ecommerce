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


function initFlashCarousel() {
  // Variables del carousel
  let currentSlide = 0;
  let autoPlayInterval = null;
  const autoPlayDelay = 4000; // 4 segundos
  const SLIDES_TO_SHOW = 4;

  // Obtener elementos del DOM
  const track = document.getElementById("carouselTrack");
  const carousel = document.getElementById("flashCarousel");
  const dotsContainer = document.getElementById("carouselDots");

  if (!track || !carousel) {
    console.warn("Flash Carousel: Elementos no encontrados");
    return;
  }

  // Contar slides automáticamente
  const slides = track.children;
  const totalSlides = slides.length;
  const maxSlideIndex = Math.max(0, totalSlides - SLIDES_TO_SHOW);

  // Configurar estilos de los slides
  function setupSlides() {
    const slideWidth = `${100 / SLIDES_TO_SHOW}%`;
    
    for (let slide of slides) {
      slide.style.flex = `0 0 ${slideWidth}`;
    }
  }

  // Función para actualizar el carousel
  function updateCarousel() {
    const slideWidth = 100 / SLIDES_TO_SHOW;
    const translateX = -(currentSlide * slideWidth);
    track.style.transform = `translateX(${translateX}%)`;
    updateDots();
  }

  // Función para actualizar los dots
  function updateDots() {
    if (!dotsContainer) return;

    const dots = dotsContainer.querySelectorAll(".carousel-dot");
    const totalDots = maxSlideIndex + 1;
    
    dots.forEach((dot, index) => {
      if (index === currentSlide) {
        dot.style.backgroundColor = "#8b5cf6";
      } else {
        dot.style.backgroundColor = "#d1d5db";
      }
    });
  }

  // Función para mover el carousel
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

  // Función para ir a slide específico
  function goToSlide(slideIndex) {
    currentSlide = Math.min(slideIndex, maxSlideIndex);
    updateCarousel();
    restartAutoPlay();
  }

  // Auto-play functions
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

  // Event listeners para navegación
  carousel.addEventListener("click", (e) => {
    if (e.target.classList.contains("carousel-prev")) {
      moveSlide(-1);
    } else if (e.target.classList.contains("carousel-next")) {
      moveSlide(1);
    } else if (e.target.classList.contains("carousel-dot")) {
      const slideIndex = parseInt(e.target.dataset.slide) || 0;
      goToSlide(slideIndex);
    }
  });

  // Event listeners para pausar auto-play
  carousel.addEventListener("mouseenter", stopAutoPlay);
  carousel.addEventListener("mouseleave", startAutoPlay);

  // Teclado
  document.addEventListener("keydown", (e) => {
    if (e.key === "ArrowLeft") {
      e.preventDefault();
      moveSlide(-1);
    } else if (e.key === "ArrowRight") {
      e.preventDefault();
      moveSlide(1);
    }
  });

  // Touch events
  let touchStartX = 0;
  let touchEndX = 0;

  carousel.addEventListener(
    "touchstart",
    (e) => {
      touchStartX = e.changedTouches[0].screenX;
      stopAutoPlay();
    },
    { passive: true }
  );

  carousel.addEventListener(
    "touchend",
    (e) => {
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
    },
    { passive: true }
  );

  // Inicializar
  setupSlides();
  updateCarousel();
  startAutoPlay();
}

// Función para el contador regresivo
function initCountdown() {
  const hoursEl = document.getElementById("hours");
  const minutesEl = document.getElementById("minutes");
  const secondsEl = document.getElementById("seconds");

  if (!hoursEl || !minutesEl || !secondsEl) {
    console.warn("Countdown: Elementos no encontrados");
    return;
  }

  // Configurar deadline (45 minutos desde ahora)
  const deadline = new Date(Date.now() + 45 * 60 * 1000);

  function updateCountdown() {
    const now = Date.now();
    const distance = deadline.getTime() - now;

    // Si el tiempo se acabó
    if (distance < 0) {
      hoursEl.textContent = "00";
      minutesEl.textContent = "00";
      secondsEl.textContent = "00";
      return;
    }

    // Calcular tiempo restante
    const hours = Math.floor(distance / (1000 * 60 * 60));
    const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((distance % (1000 * 60)) / 1000);

    // Actualizar display con padding
    hoursEl.textContent = String(hours).padStart(2, "0");
    minutesEl.textContent = String(minutes).padStart(2, "0");
    secondsEl.textContent = String(seconds).padStart(2, "0");
  }

  // Iniciar countdown
  updateCountdown();
  const countdownInterval = setInterval(updateCountdown, 1000);

  // Opcional: detener cuando llegue a 0
  setTimeout(() => {
    clearInterval(countdownInterval);
  }, 45 * 60 * 1000);
}

// Auto-inicialización cuando el DOM esté listo
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", () => {
    initFlashCarousel();
    initCountdown();
  });
} else {
  initFlashCarousel();
  initCountdown();
}

// Exponer funciones globalmente
window.initFlashCarousel = initFlashCarousel;
window.initCountdown = initCountdown;