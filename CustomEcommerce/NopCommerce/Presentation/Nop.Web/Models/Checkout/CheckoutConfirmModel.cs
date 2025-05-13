using Nop.Web.Framework.Models;
using static Nop.Web.Models.ShoppingCart.ShoppingCartModel;

namespace Nop.Web.Models.Checkout;

public partial record CheckoutConfirmModel : BaseNopModel
{
    public CheckoutConfirmModel()
    {
        Warnings = new List<string>();
        OrderReviewData = new OrderReviewDataModel();
    }

    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public bool TermsOfServicePopup { get; set; }
    public string MinOrderTotalWarning { get; set; }
    public bool DisplayCaptcha { get; set; }
    public OrderReviewDataModel OrderReviewData { get; set; }

    public IList<string> Warnings { get; set; }
}