using Nop.Core;

namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    public partial class RecipeSuggestion : BaseEntity
    {
        public int ProductId { get; set; }
        public string RecipeTitle { get; set; }
        public string Description { get; set; }
        public string ImageBase64 { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }

    public partial class RecipeIngredient : BaseEntity
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public bool IsNewIngredient { get; set; }
        public int? NopCommerceProductId { get; set; }
        public string NopCommerceProductSeName { get; set; }
        public string Base64Image { get; set; }
        public int RecipeSuggestionId { get; set; } 
    }
}