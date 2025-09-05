var DiscountGiftCard = {
  init: function () {
    $(document).ready(function () {
      $("#discountcouponcode").on("keydown", function (event) {
        if (event.keyCode == 13) {
          $("#applydiscountcouponcode").trigger("click");
          return false;
        }
      });

      $("#giftcardcouponcode").on("keydown", function (event) {
        if (event.keyCode == 13) {
          $("#applygiftcardcouponcode").trigger("click");
          return false;
        }
      });

      $(document).on("click", "#applydiscountcouponcode", function (e) {
        e.preventDefault();
        DiscountGiftCard.applyDiscount();
      });

      $(document).on("click", "#applygiftcardcouponcode", function (e) {
        e.preventDefault();
        DiscountGiftCard.applyGiftCard();
      });

      $(document).on("click", ".remove-discount", function (e) {
        e.preventDefault();
        DiscountGiftCard.removeDiscount($(this).attr("name"));
      });

      $(document).on("click", ".remove-gift-card-button", function (e) {
        e.preventDefault();
        DiscountGiftCard.removeGiftCard($(this).attr("name"));
      });
    });
  },

  getAntiForgeryToken: function () {
    return $("input[name=__RequestVerificationToken]").val();
  },

  showLoadingAnimation: function (buttonId) {
    if (buttonId) {
      const button = $(buttonId);
      button.prop("disabled", true);
      button.data("original-text", button.text());
      button.text("Aplicando...");
    }
  },

  hideLoadingAnimation: function (buttonId) {
    if (buttonId) {
      const button = $(buttonId);
      button.prop("disabled", false);
      button.text(
        button.data("original-text") ||
          button.text().replace("Aplicando...", "Aplicar")
      );
    }
  },

  applyDiscount: function () {
    var discountCode = $("#discountcouponcode").val();
    if (discountCode) {
      $("#applydiscountcouponcode").prop("disabled", true);

      $("#applydiscountcouponcode").text("Aplicando...");

      $.ajax({
        cache: false,
        url: "/onepagecheckoutaction/applydiscount",
        data: {
          discountcouponcode: discountCode,
          applydiscountcouponcode: "true",
          __RequestVerificationToken: this.getAntiForgeryToken(),
        },
        type: "POST",
        success: function (response) {
          if (response.success === true || response.success === false) {
            if (response.success) {
              window.location.reload();
            } else {
              console.error("Error al aplicar descuento:", response.message);

              Swal.fire({
                title: "Error",
                text:
                  response.message || "Error al aplicar el código de descuento",
                icon: "error",
                confirmButtonColor: "#7A37F0",
              });
            }
          } else {
            window.location.reload();
          }
        },
        error: function (xhr, status, error) {
          console.error("Error al aplicar descuento:", status, error);
          console.error("Respuesta del servidor:", xhr.responseText);

          Swal.fire({
            title: "Error",
            text: "Ocurrió un error al aplicar el código de descuento",
            icon: "error",
            confirmButtonColor: "#7A37F0",
          });
        },
        complete: function () {
          $("#applydiscountcouponcode").prop("disabled", false);
          $("#applydiscountcouponcode").text("Aplicar cupón");
        },
      });
    }
    return false;
  },

  applyGiftCard: function () {
    var giftCardCode = $("#giftcardcouponcode").val();
    if (giftCardCode) {
      $("#applygiftcardcouponcode").prop("disabled", true);
      $("#applygiftcardcouponcode").text("Aplicando...");

      $.ajax({
        cache: false,
        url: "/onepagecheckoutaction/applygiftcard",
        data: {
          giftcardcouponcode: giftCardCode,
          applygiftcardcouponcode: "true",
          __RequestVerificationToken: this.getAntiForgeryToken(),
        },
        type: "POST",
        success: function (response) {
          if (response.success === true || response.success === false) {
            if (response.success) {
              window.location.reload();
            } else {
              console.error(
                "Error al aplicar tarjeta regalo:",
                response.message
              );

              Swal.fire({
                title: "Error",
                text: response.message || "Error al aplicar la tarjeta regalo",
                icon: "error",
                confirmButtonColor: "#7A37F0",
              });
            }
          } else {
            window.location.reload();
          }
        },
        error: function (xhr, status, error) {
          console.error("Error al aplicar tarjeta regalo:", status, error);
          console.error("Respuesta del servidor:", xhr.responseText);

          Swal.fire({
            title: "Error",
            text: "Ocurrió un error al aplicar la tarjeta regalo",
            icon: "error",
            confirmButtonColor: "#7A37F0",
          });
        },
        complete: function () {
          $("#applygiftcardcouponcode").prop("disabled", false);
          $("#applygiftcardcouponcode").text("Aplicar tarjeta");
        },
      });
    }
    return false;
  },

  removeDiscount: function (buttonName) {
    $(".remove-discount").prop("disabled", true);

    $.ajax({
      cache: false,
      url: "/onepagecheckoutaction/removediscount",
      data: {
        [buttonName]: "",
        __RequestVerificationToken: this.getAntiForgeryToken(),
      },
      type: "POST",
      success: function (response) {
        if (response.success === true || response.success === false) {
          if (response.success) {
            $("#discount-container").html(
              $(response.html).find("#discount-container").html()
            );
            window.location.reload();
          } else {
            console.error("Error al eliminar descuento:", response.message);

            Swal.fire({
              title: "Error",
              text:
                response.message || "Error al eliminar el código de descuento",
              icon: "error",
              confirmButtonColor: "#7A37F0",
            });
          }
        } else {
          $("#discount-container").html(
            $(response).find("#discount-container").html()
          );
          window.location.reload();
        }
      },
      error: function (xhr, status, error) {
        console.error("Error al eliminar descuento:", status, error);
        console.error("Respuesta del servidor:", xhr.responseText);

        Swal.fire({
          title: "Error",
          text: "Ocurrió un error al eliminar el código de descuento",
          icon: "error",
          confirmButtonColor: "#7A37F0",
        });
      },
      complete: function () {
        $(".remove-discount").prop("disabled", false);
      },
    });
    return false;
  },

  removeGiftCard: function (buttonName) {
    $(".remove-gift-card-button").prop("disabled", true);

    $.ajax({
      cache: false,
      url: "/onepagecheckoutaction/removegiftcard",
      data: {
        [buttonName]: "",
        __RequestVerificationToken: this.getAntiForgeryToken(),
      },
      type: "POST",
      success: function (response) {
        if (response.success === true || response.success === false) {
          if (response.success) {
            $("#giftcard-container").html(
              $(response.html).find("#giftcard-container").html()
            );
            window.location.reload();
          } else {
            console.error(
              "Error al eliminar tarjeta regalo:",
              response.message
            );

            Swal.fire({
              title: "Error",
              text: response.message || "Error al eliminar la tarjeta regalo",
              icon: "error",
              confirmButtonColor: "#7A37F0",
            });
          }
        } else {
          $("#giftcard-container").html(
            $(response).find("#giftcard-container").html()
          );
          window.location.reload();
        }
      },
      error: function (xhr, status, error) {
        console.error("Error al eliminar tarjeta regalo:", status, error);
        console.error("Respuesta del servidor:", xhr.responseText);

        Swal.fire({
          title: "Error",
          text: "Ocurrió un error al eliminar la tarjeta regalo",
          icon: "error",
          confirmButtonColor: "#7A37F0",
        });
      },
      complete: function () {
        $(".remove-gift-card-button").prop("disabled", false);
      },
    });
    return false;
  },
};