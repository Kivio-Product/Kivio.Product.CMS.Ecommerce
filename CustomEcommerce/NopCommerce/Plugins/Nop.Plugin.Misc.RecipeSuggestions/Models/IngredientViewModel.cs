namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    public class IngredientViewModel
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public bool IsNewIngredient { get; set; }
        public int? NopCommerceProductId { get; set; } // Nullable if not an existing NopCommerce product
        public string? LinkToProductPage { get; set; } // Nullable
    }
}
