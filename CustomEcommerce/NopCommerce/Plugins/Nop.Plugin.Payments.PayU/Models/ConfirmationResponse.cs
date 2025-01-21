
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Nop.Plugin.Payments.PayU.Models
{
public class ConfirmationResponse
{
    public int ResponseCodePol { get; set; }
    public string Phone { get; set; }
    public decimal AdditionalValue { get; set; }
    public bool Test { get; set; }
    public DateTime TransactionDate { get; set; }
    public string CcNumber { get; set; }
    public string CcHolder { get; set; }
    public string ErrorCodeBank { get; set; }
    public string BillingCountry { get; set; }
    public string BankReferencedName { get; set; }
    public string Description { get; set; }
    public decimal AdministrativeFeeTax { get; set; }
    public decimal Value { get; set; }
    public decimal AdministrativeFee { get; set; }
    public int PaymentMethodType { get; set; }
    public string OfficePhone { get; set; }
    public string EmailBuyer { get; set; }
    public string ResponseMessagePol { get; set; }
    public string ErrorMessageBank { get; set; }
    public string ShippingCity { get; set; }
    public string TransactionId { get; set; }
    public string Sign { get; set; }
    public decimal Tax { get; set; }
    public int PaymentMethod { get; set; }
    public string BillingAddress { get; set; }
    public string PaymentMethodName { get; set; }
    public string PseBank { get; set; }
    public int StatePol { get; set; }
    public DateTime Date { get; set; }
    public string NicknameBuyer { get; set; }
    public string ReferencePol { get; set; }
    public string Currency { get; set; }
    public decimal Risk { get; set; }
    public string ShippingAddress { get; set; }
    public string BankId { get; set; }
    public string PaymentRequestState { get; set; }
    public string CustomerNumber { get; set; }
    public decimal AdministrativeFeeBase { get; set; }
    public int Attempts { get; set; }
    public string MerchantId { get; set; }
    public decimal ExchangeRate { get; set; }
    public string ShippingCountry { get; set; }
    public int InstallmentsNumber { get; set; }
    public string Franchise { get; set; }
    public int PaymentMethodId { get; set; }
    public string Extra1 { get; set; }
    public string Extra2 { get; set; }
    public string AntifraudMerchantId { get; set; }
    public string Extra3 { get; set; }
    public string NicknameSeller { get; set; }
    public string Ip { get; set; }
    public string AirlineCode { get; set; }
    public string BillingCity { get; set; }
    public string PseReference1 { get; set; }
    public string ReferenceSale { get; set; }
    public string PseReference3 { get; set; }
    public string PseReference2 { get; set; }

    public static ConfirmationResponse FromHttpRequest(HttpRequest request)
    {
        return new ConfirmationResponse
        {
            ResponseCodePol = int.TryParse(request.Form["response_code_pol"].ToString(), out var responseCodePol) ? responseCodePol : 0,
            Phone = request.Form["phone"].ToString(),
            AdditionalValue = decimal.TryParse(request.Form["additional_value"].ToString(), out var additionalValue) ? additionalValue : 0,
            Test = request.Form["test"].ToString() == "1",
            TransactionDate = DateTime.TryParse(request.Form["transaction_date"].ToString(), out var transactionDate) ? transactionDate : default,
            CcNumber = request.Form["cc_number"].ToString(),
            CcHolder = request.Form["cc_holder"].ToString(),
            ErrorCodeBank = request.Form["error_code_bank"].ToString(),
            BillingCountry = request.Form["billing_country"].ToString(),
            BankReferencedName = request.Form["bank_referenced_name"].ToString(),
            Description = request.Form["description"].ToString(),
            AdministrativeFeeTax = decimal.TryParse(request.Form["administrative_fee_tax"].ToString(), out var administrativeFeeTax) ? administrativeFeeTax : 0,
            Value = decimal.TryParse( request.Form["value"].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var txValue) ? AdjustValueFormat(txValue) : 0,            AdministrativeFee = decimal.TryParse(request.Form["administrative_fee"].ToString(), out var administrativeFee) ? administrativeFee : 0,
            PaymentMethodType = int.TryParse(request.Form["payment_method_type"].ToString(), out var paymentMethodType) ? paymentMethodType : 0,
            OfficePhone = request.Form["office_phone"].ToString(),
            EmailBuyer = request.Form["email_buyer"].ToString(),
            ResponseMessagePol = request.Form["response_message_pol"].ToString(),
            ErrorMessageBank = request.Form["error_message_bank"].ToString(),
            ShippingCity = request.Form["shipping_city"].ToString(),
            TransactionId = request.Form["transaction_id"].ToString(),
            Sign = request.Form["sign"].ToString(),
            Tax = decimal.TryParse(request.Form["tax"].ToString(), out var tax) ? tax : 0,
            PaymentMethod = int.TryParse(request.Form["payment_method"].ToString(), out var paymentMethod) ? paymentMethod : 0,
            BillingAddress = request.Form["billing_address"].ToString(),
            PaymentMethodName = request.Form["payment_method_name"].ToString(),
            PseBank = request.Form["pse_bank"].ToString(),
            StatePol = int.TryParse(request.Form["state_pol"].ToString(), out var statePol) ? statePol : 0,
            Date = DateTime.TryParse(request.Form["date"].ToString(), out var date) ? date : default,
            NicknameBuyer = request.Form["nickname_buyer"].ToString(),
            ReferencePol = request.Form["reference_pol"].ToString(),
            Currency = request.Form["currency"].ToString(),
            Risk = decimal.TryParse(request.Form["risk"].ToString(), out var risk) ? risk : 0,
            ShippingAddress = request.Form["shipping_address"].ToString(),
            BankId = request.Form["bank_id"].ToString(),
            PaymentRequestState = request.Form["payment_request_state"].ToString(),
            CustomerNumber = request.Form["customer_number"].ToString(),
            AdministrativeFeeBase = decimal.TryParse(request.Form["administrative_fee_base"].ToString(), out var administrativeFeeBase) ? administrativeFeeBase : 0,
            Attempts = int.TryParse(request.Form["attempts"].ToString(), out var attempts) ? attempts : 0,
            MerchantId = request.Form["merchant_id"].ToString(),
            ExchangeRate = decimal.TryParse(request.Form["exchange_rate"].ToString(), out var exchangeRate) ? exchangeRate : 0,
            ShippingCountry = request.Form["shipping_country"].ToString(),
            InstallmentsNumber = int.TryParse(request.Form["installments_number"].ToString(), out var installmentsNumber) ? installmentsNumber : 0,
            Franchise = request.Form["franchise"].ToString(),
            PaymentMethodId = int.TryParse(request.Form["payment_method_id"].ToString(), out var paymentMethodId) ? paymentMethodId : 0,
            Extra1 = request.Form["extra1"].ToString(),
            Extra2 = request.Form["extra2"].ToString(),
            AntifraudMerchantId = request.Form["antifraudMerchantId"].ToString(),
            Extra3 = request.Form["extra3"].ToString(),
            NicknameSeller = request.Form["nickname_seller"].ToString(),
            Ip = request.Form["ip"].ToString(),
            AirlineCode = request.Form["airline_code"].ToString(),
            BillingCity = request.Form["billing_city"].ToString(),
            PseReference1 = request.Form["pse_reference1"].ToString(),
            ReferenceSale = request.Form["reference_sale"].ToString(),
            PseReference3 = request.Form["pse_reference3"].ToString(),
            PseReference2 = request.Form["pse_reference2"].ToString()
        };
    }

    private static decimal AdjustValueFormat(decimal value)
    {
        // Verifica si el segundo decimal es cero
        if ((value * 100) % 10 == 0)
        {
            // Redondea a un decimal
            return Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }

        // Mantiene los dos decimales
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}


}