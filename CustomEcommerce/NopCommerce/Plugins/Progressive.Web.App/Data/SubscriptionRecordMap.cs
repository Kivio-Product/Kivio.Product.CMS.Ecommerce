using FluentMigrator;
using Nop.Core.Domain.Customers;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Progressive.Web.App.Domain;

namespace Nop.Plugin.Progressive.Web.App.Data
{
    [NopMigration("2025-08-06 12:00:00", "Progressive.Web.App base schema", MigrationProcessType.Installation)]
    public class InstallationMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.Table("WebPushSubscriptions") 
                .WithColumn(nameof(SubscriptionRecord.Id)).AsInt32().PrimaryKey().Identity()
                .WithColumn(nameof(SubscriptionRecord.CustomerId)).AsInt32().ForeignKey<Customer>().NotNullable()
                .WithColumn(nameof(SubscriptionRecord.Endpoint)).AsString(4096).NotNullable()
                .WithColumn(nameof(SubscriptionRecord.P256DHKey)).AsString(512).Nullable()
                .WithColumn(nameof(SubscriptionRecord.AuthKey)).AsString(512).Nullable();
        }
    }
}