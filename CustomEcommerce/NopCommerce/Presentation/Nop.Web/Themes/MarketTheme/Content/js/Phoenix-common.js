/* Left Sidebar  */

$(window).load(function () {
  $(function () {
    var hideFiltersText = window.FilterTranslations?.hideFilters || "Ocultar Filtros";
    var showFiltersText = window.FilterTranslations?.showFilters || "Mostrar Filtros";

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