using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.RecipeSuggestions.Models;

namespace Nop.Plugin.Misc.RecipeSuggestions.Migrations
{
    [NopMigration("2025-06-05 09:00:00", "Misc.RecipeSuggestions: Base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {

        public SchemaMigration()
        {
        }

        public override void Up()
        {
            const string aiRecipeSuggestionTableName = nameof(RecipeSuggestion);
            const string aiRecipeIngredientTableName = nameof(RecipeIngredient);

            Create.TableFor<RecipeSuggestion>();
            Create.TableFor<RecipeIngredient>();

            // Foreign key from RecipeIngredient to RecipeSuggestion
            string fkName = $"FK_{aiRecipeIngredientTableName}_{nameof(RecipeIngredient.RecipeSuggestionId)}_{aiRecipeSuggestionTableName}_{nameof(RecipeSuggestion.Id)}";

            Create.ForeignKey(fkName)
                .FromTable(aiRecipeIngredientTableName).ForeignColumn(nameof(RecipeIngredient.RecipeSuggestionId))
                .ToTable(aiRecipeSuggestionTableName).PrimaryColumn(nameof(RecipeSuggestion.Id))
                .OnDeleteOrUpdate(System.Data.Rule.Cascade);
        }
    }
}
