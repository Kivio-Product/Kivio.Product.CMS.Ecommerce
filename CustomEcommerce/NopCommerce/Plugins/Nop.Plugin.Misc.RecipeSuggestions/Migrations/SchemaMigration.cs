using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.RecipeSuggestions.Models;

namespace Nop.Plugin.Misc.RecipeSuggestions.Migrations
{
    [NopMigration("2025-05-30 09:00:00", "Misc.RecipeSuggestions: Base schema", MigrationProcessType.Installation)]
    // La fecha y hora deben ser únicas y posteriores a cualquier migración anterior del plugin.
    // "MigrationSources.Plugin" es importante para identificarla como migración de plugin.
    public class SchemaMigration : AutoReversingMigration // AutoReversingMigration simplifica el Down()
    {

        public SchemaMigration()
        {
        }

        public override void Up()
        {
            // Nombre de la tabla como se definió en el atributo [Table] del POCO
            const string aiRecipeSuggestionTableName = "RecipeSuggestion_AiRecipeSuggestion";
            const string aiRecipeIngredientTableName = "RecipeSuggestion_AiRecipeIngredient";

            // Crear la tabla AiRecipeSuggestion
            Create.Table(aiRecipeSuggestionTableName)
                .WithColumn(nameof(AiRecipeSuggestion.Id)).AsInt32().Identity().PrimaryKey() // Heredado de BaseEntity
                .WithColumn(nameof(AiRecipeSuggestion.ProductId)).AsInt32().NotNullable()
                .WithColumn(nameof(AiRecipeSuggestion.RecipeTitle)).AsString(400).NotNullable()
                .WithColumn(nameof(AiRecipeSuggestion.Description)).AsCustom("NVARCHAR(MAX)").NotNullable() // O AsString(int.MaxValue) dependiendo del dialecto SQL
                .WithColumn(nameof(AiRecipeSuggestion.ImageBase64)).AsCustom("NVARCHAR(MAX)").Nullable()
                .WithColumn(nameof(AiRecipeSuggestion.CreatedOnUtc)).AsDateTime2().NotNullable();

            // Crear índice en ProductId para AiRecipeSuggestion
            Create.Index($"IX_{aiRecipeSuggestionTableName}_ProductId")
                .OnTable(aiRecipeSuggestionTableName)
                .OnColumn(nameof(AiRecipeSuggestion.ProductId))
                .Ascending()
                .WithOptions().NonClustered();


            // Crear la tabla AiRecipeIngredient
            Create.Table(aiRecipeIngredientTableName)
                .WithColumn(nameof(AiRecipeIngredient.Id)).AsInt32().Identity().PrimaryKey() // Heredado de BaseEntity
                .WithColumn(nameof(AiRecipeIngredient.Name)).AsString(255).NotNullable()
                .WithColumn(nameof(AiRecipeIngredient.ImageUrl)).AsString(1000).Nullable()
                .WithColumn(nameof(AiRecipeIngredient.IsNewIngredient)).AsBoolean().NotNullable()
                .WithColumn(nameof(AiRecipeIngredient.NopCommerceProductId)).AsInt32().Nullable()
                .WithColumn(nameof(AiRecipeIngredient.NopCommerceProductSeName)).AsString(400).Nullable()
                .WithColumn(nameof(AiRecipeIngredient.Base64Image)).AsCustom("NVARCHAR(MAX)").Nullable()
                .WithColumn(nameof(AiRecipeIngredient.AiRecipeSuggestionId)).AsInt32().NotNullable();

            // Crear índice en AiRecipeSuggestionId para AiRecipeIngredient
            Create.Index($"IX_{aiRecipeIngredientTableName}_AiRecipeSuggestionId")
                .OnTable(aiRecipeIngredientTableName)
                .OnColumn(nameof(AiRecipeIngredient.AiRecipeSuggestionId))
                .Ascending()
                .WithOptions().NonClustered();

            // Crear clave foránea de AiRecipeIngredient a AiRecipeSuggestion
            string fkName = $"FK_{aiRecipeIngredientTableName}_{nameof(AiRecipeIngredient.AiRecipeSuggestionId)}_{aiRecipeSuggestionTableName}_{nameof(AiRecipeSuggestion.Id)}";

            Create.ForeignKey(fkName)
                .FromTable(aiRecipeIngredientTableName).ForeignColumn(nameof(AiRecipeIngredient.AiRecipeSuggestionId))
                .ToTable(aiRecipeSuggestionTableName).PrimaryColumn(nameof(AiRecipeSuggestion.Id))
                .OnDeleteOrUpdate(System.Data.Rule.Cascade);
        }
    }
}
