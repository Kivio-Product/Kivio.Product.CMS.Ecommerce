namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    public class IngredientViewModel
    {
        public string Name { get; set; }
        public string?  ImageUrl { get; set; } // Nullable, used for product images
        public bool IsNewIngredient { get; set; }
        public int? NopCommerceProductId { get; set; } // Nullable if not an existing NopCommerce product
        public string? LinkToProductPage { get; set; } // Nullable
        public string? Base64Image { get; set; } // Nullable, used for inline images
    }
}
