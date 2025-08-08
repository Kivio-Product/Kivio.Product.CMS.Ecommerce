using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.PushNotifications.Domain;

namespace Nop.Plugin.Misc.PushNotifications.Data
{
    [NopMigration("2025-08-07 00:00:00", "Misc.PushNotifications: Base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<PushSubscription>();
        }
    }
}
