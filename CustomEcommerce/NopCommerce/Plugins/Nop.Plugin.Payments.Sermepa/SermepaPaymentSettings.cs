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
        // <summary>
        /// Almacena internamente los IDs como string separados por comas
        /// </summary>
        public string SelectedCurrencyIds { get; set; }

        /// <summary>
        /// Propiedad de ayuda para convertir el string a una lista de ints
        /// </summary>
        public IList<int> SelectedCurrencyIdList
        {
            get
            {
                var result = new List<int>();
                if (!string.IsNullOrEmpty(SelectedCurrencyIds))
                {
                    result.AddRange(SelectedCurrencyIds
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.Parse(id.Trim())));
                }
                return result;
            }
            set
            {
                if (value != null && value.Any())
                    SelectedCurrencyIds = string.Join(",", value);
                else
                    SelectedCurrencyIds = string.Empty;
            }
        }
    }
}
