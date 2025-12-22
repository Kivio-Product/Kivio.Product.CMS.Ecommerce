using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Plugin.Misc.DeliveryTimePicker;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Services.Rules
{
    /// <summary>
    /// Implementation of the delivery rule service
    /// </summary>
    public class DeliveryRuleService : IDeliveryRuleService
    {
        #region Fields

        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly DeliveryTimePickerSettings _settings;

        #endregion

        #region Ctor

        public DeliveryRuleService(
            IShoppingCartService shoppingCartService,
            IProductService productService,
            IWorkContext workContext,
            DeliveryTimePickerSettings settings)
        {
            _shoppingCartService = shoppingCartService;
            _productService = productService;
            _workContext = workContext;
            _settings = settings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Evaluates a delivery slot against all applicable rules
        /// </summary>
        public async Task<RuleResult> EvaluateSlotAsync(DeliveryRuleContext context, DeliverySlot slot)
        {
            // Get applicable rules for the suppliers in the cart
            var rules = GetApplicableRules(context.SuppliersInCart);

            // If no specific rules apply, allow the slot (default behavior)
            if (rules.Count == 0)
                return RuleResult.Success;

            // Apply all rules - ALL must pass (most restrictive wins)
            foreach (var rule in rules)
            {
                var result = rule(context, slot);
                if (!result.IsAllowed)
                {
                    // Return the first failing rule's message
                    return result;
                }
            }

            return RuleResult.Success;
        }

        /// <summary>
        /// Gets the list of supplier identifiers from a customer's shopping cart
        /// </summary>
        public async Task<HashSet<string>> GetSuppliersFromCartAsync(int customerId)
        {
            var suppliers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var customer = await _workContext.GetCurrentCustomerAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart);

            foreach (var item in cart)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                
                // Check if product has a SKU and identify supplier
                if (!string.IsNullOrWhiteSpace(product?.Sku))
                {
                    if (product.Sku.Contains("EXITO", StringComparison.CurrentCultureIgnoreCase))
                    {
                        suppliers.Add("EXITO");
                    }
                }
            }

            return suppliers;
        }

        /// <summary>
        /// Builds a rule context for the given customer
        /// </summary>
        public async Task<DeliveryRuleContext> BuildContextAsync(int customerId, DateTime? now = null)
        {
            // Get current time in Colombia timezone
            var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var currentTime = now ?? TimeZoneInfo.ConvertTime(DateTime.Now, colombiaTimeZone);

            // Get suppliers from cart
            var suppliers = await GetSuppliersFromCartAsync(customerId);

            // Build context
            var context = new DeliveryRuleContext
            {
                Now = currentTime,
                SuppliersInCart = suppliers,
                CutoffHour = _settings.CutoffHour,
                DisableWeekends = _settings.DisableWeekends,
                // Placeholder for IsBusinessDay - can be replaced with actual service later
                IsBusinessDay = date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday,
                SameDayDeliveryCutoffHour = _settings.SameDayDeliveryCutoffHour
            };

            return context;
        }

        /// <summary>
        /// Gets all applicable rules for the given suppliers
        /// </summary>
        public List<SlotRule> GetApplicableRules(HashSet<string> suppliers)
        {
            var rules = new List<SlotRule>();

            // If cart contains EXITO products, apply EXITO rules
            if (suppliers.Contains("EXITO"))
            {
                rules.Add(ExitoWeekdayMorningRule);
                rules.Add(ExitoWeekdayAfternoonRule);
                rules.Add(ExitoFridayAfternoonRule);
            }

            // If cart contains regular products (no recognized suppliers), apply regular rules
            if (suppliers.Count == 0)
            {
                rules.Add(RegularSupplierSameDayOnlyRule);
            }

            // Add more supplier rules here as needed
            // Example: if (suppliers.Contains("S3")) rules.Add(S3Rule);

            return rules;
        }

        #endregion

        #region Rules

        /// <summary>
        /// Rule for regular suppliers (products without recognized supplier): Only same-day delivery allowed (before cutoff) or next-day (after cutoff)
        /// </summary>
        private RuleResult RegularSupplierSameDayOnlyRule(DeliveryRuleContext context, DeliverySlot slot)
        {
            var today = context.Now.Date;
            var isBeforeCutoff = context.Now.Hour < context.SameDayDeliveryCutoffHour;

            if (isBeforeCutoff)
            {
                // Before cutoff: only allow same-day delivery
                if (slot.Date.Date != today)
                {
                    return RuleResult.Fail($"Los productos regulares solo permiten entrega el mismo día cuando se ordena antes de las {context.CutoffHour}:00.");
                }
            }
            else
            {
                // After cutoff: only allow next-day delivery
                var nextDay = today.AddDays(1);
                if (slot.Date.Date != nextDay)
                {
                    return RuleResult.Fail($"Los productos regulares solo permiten entrega al día siguiente cuando se ordena después de las {context.CutoffHour}:00.");
                }
            }

            return RuleResult.Success;
        }

        /// <summary>
        /// Rule for EXITO on weekday mornings (Mon-Thu before cutoff): 
        /// Can choose today afternoon (16:00-18:00) OR next business day
        /// </summary>
        private RuleResult ExitoWeekdayMorningRule(DeliveryRuleContext context, DeliverySlot slot)
        {
            // Only apply this rule on Monday-Thursday mornings
            if (context.Now.DayOfWeek >= DayOfWeek.Friday || context.Now.Hour >= context.CutoffHour)
                return RuleResult.Success; // Not applicable

            var today = context.Now.Date;
            var isMorning = context.Now.Hour < context.CutoffHour;

            // Check if it's Monday-Thursday morning
            if (context.Now.DayOfWeek >= DayOfWeek.Monday && context.Now.DayOfWeek <= DayOfWeek.Thursday && isMorning)
            {
                // Allow today afternoon slots (16:00-18:00)
                if (slot.Date.Date == today && slot.StartTime.Hours >= 16 && slot.EndTime.Hours <= 18)
                {
                    return RuleResult.Success;
                }

                // Allow next business day (any slot)
                var nextBusinessDay = GetNextBusinessDay(context, today);
                if (slot.Date.Date == nextBusinessDay)
                {
                    return RuleResult.Success;
                }

                // Also allow any future business days
                if (slot.Date.Date > nextBusinessDay && context.IsBusinessDay(slot.Date))
                {
                    return RuleResult.Success;
                }

                return RuleResult.Fail("Para pedidos ÉXITO realizados en la mañana (lunes-jueves), solo se permiten franjas de la tarde (16:00-18:00) del mismo día o días hábiles posteriores.");
            }

            return RuleResult.Success;
        }

        /// <summary>
        /// Rule for EXITO on weekday afternoons (Mon-Thu after cutoff):
        /// Must choose next business day or later
        /// </summary>
        private RuleResult ExitoWeekdayAfternoonRule(DeliveryRuleContext context, DeliverySlot slot)
        {
            // Only apply this rule on Monday-Thursday afternoons
            if (context.Now.DayOfWeek >= DayOfWeek.Friday || context.Now.Hour < context.CutoffHour)
                return RuleResult.Success; // Not applicable

            var today = context.Now.Date;
            var isAfternoon = context.Now.Hour >= context.CutoffHour;

            // Check if it's Monday-Thursday afternoon
            if (context.Now.DayOfWeek >= DayOfWeek.Monday && context.Now.DayOfWeek <= DayOfWeek.Thursday && isAfternoon)
            {
                var nextBusinessDay = GetNextBusinessDay(context, today);

                // Must be next business day or later
                if (slot.Date.Date < nextBusinessDay)
                {
                    return RuleResult.Fail("Para pedidos ÉXITO realizados en la tarde (lunes-jueves), la entrega debe ser el siguiente día hábil o posterior.");
                }

                // Must be a business day
                if (!context.IsBusinessDay(slot.Date))
                {
                    return RuleResult.Fail("La fecha seleccionada no es un día hábil.");
                }

                return RuleResult.Success;
            }

            return RuleResult.Success;
        }

        /// <summary>
        /// Rule for EXITO on Friday afternoons: Can only choose Friday afternoon slots (16:00-18:00)
        /// </summary>
        private RuleResult ExitoFridayAfternoonRule(DeliveryRuleContext context, DeliverySlot slot)
        {
            // Only apply this rule on Friday afternoons
            if (context.Now.DayOfWeek != DayOfWeek.Friday || context.Now.Hour < context.CutoffHour)
                return RuleResult.Success; // Not applicable

            var today = context.Now.Date;
            var isFriday = context.Now.DayOfWeek == DayOfWeek.Friday;
            var isAfternoon = context.Now.Hour >= context.CutoffHour;

            // Check if it's Friday afternoon
            if (isFriday && isAfternoon)
            {
                // Only allow Friday afternoon slots (16:00-18:00)
                if (slot.Date.Date == today && slot.StartTime.Hours >= 16 && slot.EndTime.Hours <= 18)
                {
                    return RuleResult.Success;
                }

                return RuleResult.Fail("Para pedidos ÉXITO realizados el viernes en la tarde, solo se permiten franjas del mismo viernes entre las 16:00 y las 18:00.");
            }

            return RuleResult.Success;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the next business day after the given date
        /// </summary>
        private DateTime GetNextBusinessDay(DeliveryRuleContext context, DateTime fromDate)
        {
            var nextDay = fromDate.AddDays(1);
            
            // Keep adding days until we find a business day
            while (!context.IsBusinessDay(nextDay))
            {
                nextDay = nextDay.AddDays(1);
            }

            return nextDay;
        }

        #endregion
    }
}
