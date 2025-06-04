using LinqToDB;
using LinqToDB.Mapping;
using Nop.Core;

namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    [Table("RecipeSuggestion_AiRecipeSuggestion")]
    public partial class AiRecipeSuggestion : BaseEntity
    {
        [Column(DataType = DataType.Int32), NotNull]
        public int ProductId { get; set; }

        [Column(Length = 400), NotNull]
        public string RecipeTitle { get; set; }

        [Column(DataType = DataType.NText), NotNull]
        public string Description { get; set; }

        [Column(DataType = DataType.NText), Nullable] 
        public string ImageBase64 { get; set; }

        [Column(DataType = DataType.DateTime2), NotNull]
        public DateTime CreatedOnUtc { get; set; }
    }
    [Table("RecipeSuggestion_AiRecipeIngredient")] // Nombre de la tabla
    public partial class AiRecipeIngredient : BaseEntity
    {
        [Column(Length = 255), NotNull]
        public string Name { get; set; }

        [Column(Length = 1000), Nullable]
        public string ImageUrl { get; set; }

        [Column(DataType = DataType.Boolean), NotNull]
        public bool IsNewIngredient { get; set; }

        [Column(DataType = DataType.Int32), Nullable]
        public int? NopCommerceProductId { get; set; }

        [Column(Length = 400), Nullable]
        public string NopCommerceProductSeName { get; set; }

        [Column(DataType = DataType.NText), Nullable]
        public string Base64Image { get; set; }

        [Column(DataType = DataType.Int32), NotNull]
        public int AiRecipeSuggestionId { get; set; } // Clave foránea
    }
}