/*
 ** nopCommerce custom js functions
 */

function OpenWindow(query, w, h, scroll) {
  var l = (screen.width - w) / 2;
  var t = (screen.height - h) / 2;

  winprops =
    "resizable=0, height=" +
    h +
    ",width=" +
    w +
    ",top=" +
    t +
    ",left=" +
    l +
    "w";
  if (scroll) winprops += ",scrollbars=1";
  var f = window.open(query, "_blank", winprops);
}

function setLocation(url) {
  window.location.href = url;
}

function displayAjaxLoading(display) {
  if (display) {
    $(".ajax-loading-block-window").show();
  } else {
    $(".ajax-loading-block-window").hide("slow");
  }
}

function displayPopupNotification(message, messagetype, modal) {
  //types: success, error, warning
  var container;
  if (messagetype == "success") {
    //success
    container = $("#dialog-notifications-success");
  } else if (messagetype == "error") {
    //error
    container = $("#dialog-notifications-error");
  } else if (messagetype == "warning") {
    //warning
    container = $("#dialog-notifications-warning");
  } else {
    //other
    container = $("#dialog-notifications-success");
  }

  //we do not encode displayed message
  var htmlcode = "";
  if (typeof message == "string") {
    htmlcode = "<p>" + message + "</p>";
  } else {
    for (var i = 0; i < message.length; i++) {
      htmlcode = htmlcode + "<p>" + message[i] + "</p>";
    }
  }

  container.html(htmlcode);

  var isModal = modal ? true : false;
  container.dialog({
    modal: isModal,
    width: 350,
  });
}
function displayJoinedPopupNotifications(notes) {
  if (Object.keys(notes).length === 0) return;

  var container = $("#dialog-notifications-success");
  var htmlcode = document.createElement("div");

  for (var note in notes) {
    if (notes.hasOwnProperty(note)) {
      var messages = notes[note];

      for (var i = 0; i < messages.length; ++i) {
        var elem = document.createElement("div");
        elem.innerHTML = messages[i];
        elem.classList.add("popup-notification");
        elem.classList.add(note);

        htmlcode.append(elem);
      }
    }
  }

  container.html(htmlcode);
  container.dialog({
    width: 350,
    modal: true,
  });
}

function displayPopupContentFromUrl(url, title, modal, width) {
  const isModal = modal !== false;
  const targetWidth = width || 550;

  const stickyHeader = document.querySelector(".header.sticky");
  let originalZIndex = null;

  if (stickyHeader) {
    const computedStyle = window.getComputedStyle(stickyHeader);
    originalZIndex = computedStyle.zIndex;
    stickyHeader.style.zIndex = "auto";
  }

  const overlay = document.createElement("div");
  overlay.className = "modern-modal-overlay";
  overlay.innerHTML = `
        <div class="modern-modal-container" style="max-width: ${targetWidth}px;">
            <div class="modern-modal-header">
                <h2 class="modern-modal-title">${title || "Información"}</h2>
                <button class="modern-modal-close" type="button">×</button>
            </div>
            <div class="modern-modal-content">
                <div class="modern-modal-loading">
                    <div class="modern-modal-spinner"></div>
                    <p>Cargando contenido...</p>
                </div>
            </div>
        </div>
    `;

  const closeModal = function () {
    if (stickyHeader && originalZIndex !== null) {
      if (originalZIndex === "auto" || originalZIndex === "") {
        stickyHeader.style.zIndex = "";
      } else {
        stickyHeader.style.zIndex = originalZIndex;
      }
    }

    overlay.style.opacity = "0";
    overlay.querySelector(".modern-modal-container").style.transform =
      "translateY(-20px) scale(0.95)";

    setTimeout(() => {
      if (overlay.parentNode) {
        overlay.parentNode.removeChild(overlay);
      }
      document.body.style.overflow = "";
    }, 300);
  };

  overlay
    .querySelector(".modern-modal-close")
    .addEventListener("click", closeModal);

  if (isModal) {
    overlay.addEventListener("click", function (e) {
      if (e.target === overlay) {
        closeModal();
      }
    });
  }

  const escapeHandler = function (e) {
    if (e.key === "Escape") {
      document.removeEventListener("keydown", escapeHandler);
      closeModal();
    }
  };
  document.addEventListener("keydown", escapeHandler);

  document.body.appendChild(overlay);
  document.body.style.overflow = "hidden";

  if (typeof $ !== "undefined" && $.fn.load) {
    $("<div></div>").load(url, function (response, status, xhr) {
      const contentDiv = overlay.querySelector(".modern-modal-content");

      if (status === "error") {
        contentDiv.innerHTML = `
                    <div class="modern-modal-error">
                        <p>Error al cargar el contenido</p>
                        <p class="error-details">${xhr.status} ${xhr.statusText}</p>
                    </div>
                `;
      } else {
        contentDiv.innerHTML = response;
      }
    });
  } else {
    fetch(url)
      .then((response) => {
        if (!response.ok) {
          throw new Error(`${response.status} ${response.statusText}`);
        }
        return response.text();
      })
      .then((data) => {
        overlay.querySelector(".modern-modal-content").innerHTML = data;
      })
      .catch((error) => {
        overlay.querySelector(".modern-modal-content").innerHTML = `
                    <div class="modern-modal-error">
                        <p>Error al cargar el contenido</p>
                        <p class="error-details">${error.message}</p>
                    </div>
                `;
      });
  }

  return overlay;
}

function displayPopupContentFromUrlLegacy(url, title, modal, width) {
  return displayPopupContentFromUrl(url, title, modal, width);
}

function displayBarNotification(message, messagetype, timeout) {
  var notificationTimeout;

  var messages = typeof message === "string" ? [message] : message;
  if (messages.length === 0) return;

  //types: success, error, warning
  var cssclass =
    ["success", "error", "warning"].indexOf(messagetype) !== -1
      ? messagetype
      : "success";

  $("#bar-notification").children(".generalnote-main").show();
  //remove previous CSS classes and notifications
  $("#bar-notification")
    .removeClass("success")
    .removeClass("error")
    .removeClass("warning");
  $(".bar-notification").remove();

  //add new notifications
  var htmlcode = document.createElement("div");

  //IE11 Does not support miltiple parameters for the add() & remove() methods
  htmlcode.classList.add("bar-notification", cssclass);
  htmlcode.classList.add(cssclass);

  //add close button for notification
  var close = document.createElement("span");
  close.classList.add("close");
  close.setAttribute(
    "title",
    document.getElementById("bar-notification").dataset.close
  );

  for (var i = 0; i < messages.length; i++) {
    var content = document.createElement("p");
    content.classList.add("content");
    content.innerHTML = messages[i];

    htmlcode.appendChild(content);
  }

  htmlcode.appendChild(close);

  $(".generalnote-main").append(htmlcode);

  $(htmlcode)
    .fadeIn("slow")
    .on("mouseenter", function () {
      clearTimeout(notificationTimeout);
    });

  //callback for notification removing
  var removeNoteItem = function () {
    $(htmlcode).remove();
  };

  $(close).on("click", function () {
    $("#bar-notification").children(".generalnote-main").hide();
    $(htmlcode).fadeOut("slow", removeNoteItem);
  });

  //timeout (if set)
  if (timeout > 0) {
    notificationTimeout = setTimeout(function () {
      $("#bar-notification").children(".generalnote-main").hide();
      $(htmlcode).fadeOut("slow", removeNoteItem);
    }, timeout);
  }
}

function htmlEncode(value) {
  return $("<div/>").text(value).html();
}

function htmlDecode(value) {
  return $("<div/>").html(value).text();
}

// CSRF (XSRF) security
function addAntiForgeryToken(data) {
  //if the object is undefined, create a new one.
  if (!data) {
    data = {};
  }
  //add token
  var tokenInput = $("input[name=__RequestVerificationToken]");
  if (tokenInput.length) {
    data.__RequestVerificationToken = tokenInput.val();
  }
  return data;
}

// =======================
// Super Deals & Flash Sale & Pantry Staples Carousel Manager
// =======================

window.ProductDisplayManager = {
  resizeTimer: null,

  setupAllProductDisplays: function () {
    this.setupFlashSaleDisplay();
    this.setupSuperDealsDisplay();
    this.setupPantryStaplesDisplay();
    this.setupFilteredProductsDisplay();
    this.setupSimilarProductsDisplay();
  },

  setupFlashSaleDisplay: function () {
    var highDiscountContainer = $("#high-discount-products");
    var regularDiscountContainer = $("#regular-discount-products");

    if (window.innerWidth <= 767) {
      this.setupMobileCarousel(highDiscountContainer, 2, "high-discount-row", {
        items: 2,
        margin: 8,
        stagePadding: 0,
        center: false,
      });

      this.setupMobileCarousel(
        regularDiscountContainer,
        2,
        "regular-discount-row",
        {
          items: 2,
          center: false,
        }
      );
    } else {
      highDiscountContainer.removeClass("mobile-grid");
      regularDiscountContainer.removeClass("mobile-grid");

      highDiscountContainer.find(".item-box").show();
      regularDiscountContainer.find(".item-box").show();

      if (
        highDiscountContainer.length &&
        highDiscountContainer.find(".item-box").length > 3
      ) {
        this.setupDesktopCarousel(highDiscountContainer, {
          responsive: {
            0: { items: 1 },
            600: { items: 2 },
            1024: { items: 3 },
          },
        });
        highDiscountContainer.css("width", "60%");
        $(".regular-discount-row").css("margin-top", "40px");
      } else {
        this.destroyCarousel(highDiscountContainer);
        highDiscountContainer.css("width", "100%");
        $(".regular-discount-row").css("margin-top", "");
      }

      if (
        regularDiscountContainer.length &&
        regularDiscountContainer.find(".item-box").length > 5
      ) {
        this.setupDesktopCarousel(regularDiscountContainer, {
          responsive: {
            0: { items: 1 },
            600: { items: 2 },
            1024: { items: 5 },
            1366: { items: 5 },
          },
        });
      }
    }
  },

  setupSuperDealsDisplay: function () {
    var activeContainer = $(".category-products-container.active");
    var productContainer = activeContainer.find(".item-grid");

    if (!productContainer.length) return;

    if (window.innerWidth <= 767) {
      this.setupMobileCarousel(productContainer, 2, "super-deal-row", {
        items: 2,
        margin: 8,
        stagePadding: 0,
        center: false,
      });
    } else {
      productContainer.removeClass("mobile-grid");
      productContainer.addClass("super-deal-row");
      productContainer.find(".item-box").show();

      var productCount = productContainer.find(".item-box").length;

      if (productCount > 5) {
        this.setupDesktopCarousel(productContainer, {
          responsive: {
            0: { items: 1 },
            600: { items: 2 },
            1024: { items: 5 },
            1366: { items: 5 },
          },
        });
      } else {
        this.destroyCarousel(productContainer);
      }
    }
  },

  setupPantryStaplesDisplay: function () {
    var container = $("#pantry-staples-products");

    if (!container.length) return;

    var productCount = container.find(".item-box").length;

    if (window.innerWidth <= 767) {
      this.setupMobileCarousel(container, 2, "super-deal-row", {
        items: 2,
        margin: 8,
        stagePadding: 0,
        center: false,
      });
    } else {
      container.removeClass("mobile-grid");
      container.addClass("super-deal-row");
      container.find(".item-box").show();

      if (productCount > 3) {
        this.setupDesktopCarousel(container, {
          responsive: {
            0: { items: 1 },
            600: { items: 3 },
            1024: { items: 3 },
            1366: { items: 3 },
          },
        });
      } else {
        this.destroyCarousel(container);
      }
    }
  },

  setupFilteredProductsDisplay: function () {
    var activeContainer = $(".filtered-products-row");
    var productContainer = activeContainer.find(".item-grid");

    if (!productContainer.length) return;

    if (window.innerWidth <= 767) {
      this.setupMobileCarousel(productContainer, 2, "filtered-products-row", {
        items: 2,
        margin: 8,
        stagePadding: 0,
        center: false,
      });
    } else {
      productContainer.removeClass("mobile-grid");
      productContainer.addClass("filtered-products-row");
      productContainer.find(".item-box").show();

      var productCount = productContainer.find(".item-box").length;

      if (productCount > 5) {
        this.setupDesktopCarousel(productContainer, {
          responsive: {
            0: { items: 1 },
            600: { items: 2 },
            1024: { items: 5 },
            1366: { items: 5 },
          },
        });
      } else {
        this.destroyCarousel(productContainer);
      }
    }
  },

  setupSimilarProductsDisplay: function () {
    var activeContainer = $(".similar-product-row");
    var productContainer = activeContainer.find(".item-grid");

    if (!productContainer.length) return;

    if (window.innerWidth <= 767) {
      this.setupMobileCarousel(productContainer, 2, "similar-product-row", {
        items: 2,
        margin: 8,
        stagePadding: 0,
        center: false,
      });
    } else {
      productContainer.removeClass("mobile-grid");
      productContainer.addClass("similar-product-row");
      productContainer.find(".item-box").show();

      var productCount = productContainer.find(".item-box").length;

      if (productCount > 5) {
        this.setupDesktopCarousel(productContainer, {
          responsive: {
            0: { items: 1 },
            600: { items: 2 },
            1024: { items: 5 },
            1366: { items: 5 },
          },
        });
      } else {
        this.destroyCarousel(productContainer);
      }
    }
  },

  setupMobileCarousel: function (
    container,
    threshold,
    gridClass,
    customOptions
  ) {
    if (!container.length) return;

    var itemCount = container.find(".item-box").length;

    if (itemCount > threshold) {
      if (!container.hasClass("owl-carousel")) {
        container.removeClass("mobile-grid " + gridClass);
        container.addClass("owl-carousel px_featured");

        var defaultOptions = {
          autoplay: false,
          loop: true,
          rtl: false,
          responsiveClass: true,
          navigation: true,
          dots: false,
          nav: true,
        };

        var options = $.extend({}, defaultOptions, customOptions);
        container.owlCarousel(options);
      }
    } else {
      this.destroyCarousel(container);
      container.addClass("mobile-grid " + gridClass);
      container.find(".item-box").show();
    }
  },

  setupDesktopCarousel: function (container, customOptions) {
    if (!container.hasClass("owl-carousel")) {
      container.addClass("owl-carousel px_featured");

      var defaultOptions = {
        autoplay: false,
        loop: true,
        rtl: false,
        responsiveClass: true,
        autoHeight: true,
        navigation: true,
        dots: false,
        nav: true,
      };

      var options = $.extend(true, {}, defaultOptions, customOptions);
      container.owlCarousel(options);
    }
  },

  destroyCarousel: function (container) {
    if (container.hasClass("owl-carousel")) {
      container.trigger("destroy.owl.carousel");
      container.removeClass("owl-carousel px_featured");
      container
        .find(".owl-stage-outer, .owl-stage, .owl-item")
        .each(function () {
          $(this).removeAttr("style");
        });
    }
  },

  destroyAllCarousels: function () {
    $(".item-grid.owl-carousel").each(function () {
      var $this = $(this);
      $this.trigger("destroy.owl.carousel");
      $this.removeClass("owl-carousel px_featured");
      $this.find(".owl-stage-outer, .owl-stage, .owl-item").each(function () {
        $(this).removeAttr("style");
      });
    });
  },
};

// =======================
// Init events on page load
// =======================
$(document).ready(function () {
  // Inicialización de todos los displays
  ProductDisplayManager.setupAllProductDisplays();

  // Reconfiguración en resize
  $(window).resize(function () {
    clearTimeout(ProductDisplayManager.resizeTimer);
    ProductDisplayManager.resizeTimer = setTimeout(function () {
      ProductDisplayManager.destroyAllCarousels();
      ProductDisplayManager.setupAllProductDisplays();
    }, 250);
  });
});
