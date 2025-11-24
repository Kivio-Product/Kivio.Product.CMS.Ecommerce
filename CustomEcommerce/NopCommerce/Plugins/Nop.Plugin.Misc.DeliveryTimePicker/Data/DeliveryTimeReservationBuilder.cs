using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Data
{
    /// <summary>
    /// Represents a delivery time reservation entity builder
    /// </summary>
    public class DeliveryTimeReservationBuilder : NopEntityBuilder<DeliveryTimeReservation>
    {
        #region Methods

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(DeliveryTimeReservation.OrderId)).AsInt32().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.DeliveryDate)).AsDate().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.MinDeliveryTime)).AsTime().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.MaxDeliveryTime)).AsTime().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.TimeSlotId)).AsInt32().Nullable()
                .WithColumn(nameof(DeliveryTimeReservation.IsConfirmed)).AsBoolean().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.CustomerId)).AsInt32().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(DeliveryTimeReservation.ReservedUntilUtc)).AsDateTime2().Nullable()
                .WithColumn(nameof(DeliveryTimeReservation.HasExitoProducts)).AsBoolean().NotNullable();
        }

        #endregion
    }
}
