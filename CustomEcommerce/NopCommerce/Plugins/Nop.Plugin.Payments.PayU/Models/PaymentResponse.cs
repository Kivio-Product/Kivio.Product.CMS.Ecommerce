using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Nop.Plugin.Payments.PayU.Models
{
    public class PaymentResponse
    {
        public string MerchantId { get; set; }
        public string MerchantName { get; set; }
        public string MerchantAddress { get; set; }
        public string Telephone { get; set; }
        public string MerchantUrl { get; set; }
        public int TransactionState { get; set; }
        public string LapTransactionState { get; set; }
        public string Message { get; set; }
        public string ReferenceCode { get; set; }
        public string ReferencePol { get; set; }
        public string TransactionId { get; set; }
        public string Description { get; set; }
        public string TrazabilityCode { get; set; }
        public string Cus { get; set; }
        public string OrderLanguage { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public int PolTransactionState { get; set; }
        public string Signature { get; set; }
        public int PolResponseCode { get; set; }
        public string LapResponseCode { get; set; }
        public string Risk { get; set; }
        public int PolPaymentMethod { get; set; }
        public string LapPaymentMethod { get; set; }
        public int PolPaymentMethodType { get; set; }
        public string LapPaymentMethodType { get; set; }
        public int InstallmentsNumber { get; set; }
        public decimal TXValue { get; set; }
        public string TXTax { get; set; }
        public string Currency { get; set; }
        public string Lng { get; set; }
        public string PseCycle { get; set; }
        public string BuyerEmail { get; set; }
        public string PseBank { get; set; }
        public string PseReference1 { get; set; }
        public string PseReference2 { get; set; }
        public string PseReference3 { get; set; }
        public string AuthorizationCode { get; set; }
        public string KhipuBank { get; set; }
        public string TxAdministrativeFee { get; set; }
        public string TxTaxAdministrativeFee { get; set; }
        public string TxTaxAdministrativeFeeReturnBase { get; set; }
        public DateTime ProcessingDate { get; set; }

        public static PaymentResponse FromHttpRequest(HttpRequest request)
        {
            var parameters = request.Query;
            return new PaymentResponse
            {
                MerchantId = parameters["merchantId"],
                MerchantName = parameters["merchant_name"],
                MerchantAddress = parameters["merchant_address"],
                Telephone = parameters["telephone"],
                MerchantUrl = parameters["merchant_url"],
                TransactionState = int.Parse(parameters["transactionState"].ToString().ToString() ?? "0"),
                LapTransactionState = parameters["lapTransactionState"],
                Message = parameters["message"],
                ReferenceCode = parameters["referenceCode"],
                ReferencePol = parameters["reference_pol"],
                TransactionId = parameters["transactionId"],
                Description = parameters["description"],
                TrazabilityCode = parameters["trazabilityCode"],
                Cus = parameters["cus"],
                OrderLanguage = parameters["orderLanguage"],
                Extra1 = parameters["extra1"],
                Extra2 = parameters["extra2"],
                Extra3 = parameters["extra3"],
                PolTransactionState = int.Parse(parameters["polTransactionState"].ToString() ?? "0"),
                Signature = parameters["signature"],
                PolResponseCode = int.Parse(parameters["polResponseCode"].ToString() ?? "0"),
                LapResponseCode = parameters["lapResponseCode"],
                Risk = parameters["risk"],
                PolPaymentMethod = int.Parse(parameters["polPaymentMethod"].ToString() ?? "0"),
                LapPaymentMethod = parameters["lapPaymentMethod"],
                PolPaymentMethodType = int.Parse(parameters["polPaymentMethodType"].ToString() ?? "0"),
                LapPaymentMethodType = parameters["lapPaymentMethodType"],
                InstallmentsNumber = int.Parse(parameters["installmentsNumber"].ToString() ?? "0"),
                TXValue = decimal.TryParse(parameters["TX_VALUE"], NumberStyles.Number, CultureInfo.InvariantCulture, out var txValue) ? txValue : 0,
                TXTax = parameters["TX_TAX"],
                Currency = parameters["currency"],
                Lng = parameters["lng"],
                PseCycle = parameters["pseCycle"],
                BuyerEmail = parameters["buyerEmail"],
                PseBank = parameters["pseBank"],
                PseReference1 = parameters["pseReference1"],
                PseReference2 = parameters["pseReference2"],
                PseReference3 = parameters["pseReference3"],
                AuthorizationCode = parameters["authorizationCode"],
                KhipuBank = parameters["khipuBank"],
                TxAdministrativeFee = parameters["TX_ADMINISTRATIVE_FEE"].ToString(),
                TxTaxAdministrativeFee = parameters["TX_TAX_ADMINISTRATIVE_FEE"].ToString(),
                TxTaxAdministrativeFeeReturnBase = parameters["TX_TAX_ADMINISTRATIVE_FEE_RETURN_BASE"],
                ProcessingDate = DateTime.Parse(parameters["processingDate"].ToString() ?? DateTime.MinValue.ToString())
            };
        }

       
    }
}