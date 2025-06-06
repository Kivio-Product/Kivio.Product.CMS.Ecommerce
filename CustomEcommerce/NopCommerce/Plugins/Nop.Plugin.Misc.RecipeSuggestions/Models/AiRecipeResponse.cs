using System.Collections.Generic;

namespace Nop.Plugin.Misc.RecipeSuggestions.Models;
public class AiRecipeResponse
{
    public string Title { get; set; }
    public List<AiIngredient> Ingredients { get; set; }
    public string Instructions { get; set; }
}

public class AiIngredient
{
    public string Id { get; set; }
    public string Name { get; set; }
} 