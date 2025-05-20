using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Misc.ArtificialIntelligence.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Misc.ArtificialIntelligence.Fields.ApiKey")]
        [DataType(DataType.Password)]
        public string ApiKey { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ArtificialIntelligence.Fields.BasePrompt")]
        public string BasePrompt { get; set; }
        // Default prompt: "Given the product '[ProductName]' (description: '[ProductDescription]'), suggest [NumberOfIngredients] complementary ingredients that can be found in an online grocery store and the name of a popular recipe that can be made with them. Format the response as: INGREDIENTS: [Ingredient1], [Ingredient2], ... RECETA: [RecipeName]."

        [NopResourceDisplayName("Plugins.Misc.ArtificialIntelligence.Fields.NumberOfIngredients")]
        public int NumberOfIngredients { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ArtificialIntelligence.Fields.CacheDurationMinutes")]
        public int CacheDurationMinutes { get; set; }

        [NopResourceDisplayName("Plugins.Misc.ArtificialIntelligence.Fields.EnablePlugin")]
        public bool EnablePlugin { get; set; }

        // TODO: Add property for ProductMappingStrategy if we implement the strategy pattern for mapping
        // [NopResourceDisplayName("Plugins.Misc.ArtificialIntelligence.Fields.MappingStrategy")]
        // public string MappingStrategy { get; set; } 
        // public SelectList MappingStrategyOptions { get; set; } // For a dropdown in config
    }
}
