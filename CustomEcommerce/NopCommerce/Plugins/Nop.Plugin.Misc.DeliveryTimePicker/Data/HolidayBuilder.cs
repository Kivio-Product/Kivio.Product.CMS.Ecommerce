using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Data
{
    /// <summary>
    /// Represents a holiday entity builder
    /// </summary>
    public class HolidayBuilder : NopEntityBuilder<Holiday>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(Holiday.Date)).AsDate().NotNullable()
                .WithColumn(nameof(Holiday.Name)).AsString(200).NotNullable()
                .WithColumn(nameof(Holiday.IsRecurring)).AsBoolean().NotNullable()
                .WithColumn(nameof(Holiday.CountryCode)).AsString(10).Nullable()
                .WithColumn(nameof(Holiday.IsAutoImported)).AsBoolean().NotNullable()
                .WithColumn(nameof(Holiday.IsActive)).AsBoolean().NotNullable()
                .WithColumn(nameof(Holiday.CreatedOnUtc)).AsDateTime2().NotNullable();
        }

        #endregion
    }
}
