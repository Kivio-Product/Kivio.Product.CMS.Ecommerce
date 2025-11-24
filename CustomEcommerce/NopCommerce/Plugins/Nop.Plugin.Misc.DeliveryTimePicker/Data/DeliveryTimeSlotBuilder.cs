using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Data
{
    /// <summary>
    /// Represents a delivery time slot entity builder
    /// </summary>
    public class DeliveryTimeSlotBuilder : NopEntityBuilder<DeliveryTimeSlot>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(DeliveryTimeSlot.DayOfWeek)).AsInt32().NotNullable()
                .WithColumn(nameof(DeliveryTimeSlot.StartTime)).AsTime().NotNullable()
                .WithColumn(nameof(DeliveryTimeSlot.EndTime)).AsTime().NotNullable()
                .WithColumn(nameof(DeliveryTimeSlot.IsEnabled)).AsBoolean().NotNullable()
                .WithColumn(nameof(DeliveryTimeSlot.MaxCapacity)).AsInt32().Nullable()
                .WithColumn(nameof(DeliveryTimeSlot.DisplayOrder)).AsInt32().NotNullable()
                .WithColumn(nameof(DeliveryTimeSlot.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(DeliveryTimeSlot.UpdatedOnUtc)).AsDateTime2().NotNullable();
        }

        #endregion
    }
}
