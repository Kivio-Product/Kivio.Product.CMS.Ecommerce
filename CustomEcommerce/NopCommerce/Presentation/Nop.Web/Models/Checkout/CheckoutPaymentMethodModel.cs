using Nop.Web.Framework.Models;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Models.Checkout;

public partial record CheckoutPaymentMethodModel : BaseNopModel
{
    public CheckoutPaymentMethodModel()
    {
        PaymentMethods = new List<PaymentMethodModel>();
        OrderTotals = new OrderTotalsModel();
    }

    public IList<PaymentMethodModel> PaymentMethods { get; set; }
    public OrderTotalsModel OrderTotals { get; set; }

    public bool DisplayRewardPoints { get; set; }
    public int RewardPointsBalance { get; set; }
    public int RewardPointsToUse { get; set; }
    public string RewardPointsToUseAmount { get; set; }
    public bool RewardPointsEnoughToPayForOrder { get; set; }
    public bool UseRewardPoints { get; set; }

    #region Nested classes

    public partial record PaymentMethodModel : BaseNopModel
    {
        public string PaymentMethodSystemName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Fee { get; set; }
        public bool Selected { get; set; }
        public string LogoUrl { get; set; }
    }

    #endregion
}