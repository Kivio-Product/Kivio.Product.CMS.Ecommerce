using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Data
{
    [NopMigration("2025/10/29 12:00:00", "Misc.DeliveryTimePicker base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<DeliveryTimeSlot>();
            Create.TableFor<DeliveryTimeReservation>();
        }
    }
}
