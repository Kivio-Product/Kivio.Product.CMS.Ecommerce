using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Sermepa
{
    public class SermepaPaymentSettings : ISettings
    {
        public string NombreComercio { get; set; }
        public string Titular { get; set; }
        public string Producto { get; set; }
        public virtual string FUC { get; set; }
        public virtual string Terminal { get; set; }
        public virtual string Moneda { get; set; }
        public string ClaveReal { get; set; }
        public virtual string ClavePruebas { get; set; }
        public virtual bool Pruebas { get; set; }
        public virtual decimal AdditionalFee { get; set; }
        public bool AdditionalFeePercentage { get; set; }
    }
}
