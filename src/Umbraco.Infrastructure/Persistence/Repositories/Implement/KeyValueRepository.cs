using System;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Dtos;
using Umbraco.Core.Persistence.Repositories;
using Umbraco.Core.Scoping;

namespace Umbraco.Infrastructure.Persistence.Repositories.Implement
{
    public class KeyValueRepository : IKeyValueRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly ILogger _logger;

        public KeyValueRepository(IScopeProvider scopeProvider, ILogger logger)
        {
            _scopeProvider = scopeProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public void PerformInitialization()
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                var context = new MigrationContext(scope.Database, _logger);
                var initMigration = new InitializeMigration(context);
                initMigration.Migrate();
                scope.Complete();
            }
        }

        /// <inheritdoc />
        public string GetValue(string key)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                var sql = scope.SqlContext.Sql().Select<KeyValueDto>().From<KeyValueDto>().Where<KeyValueDto>(x => x.Key == key);
                var dto = scope.Database.Fetch<KeyValueDto>(sql).FirstOrDefault();
                scope.Complete();
                return dto?.Value;
            }
        }

        /// <inheritdoc />
        public void SetValue(string key, string value)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                scope.WriteLock(Constants.Locks.KeyValues);

                var sql = scope.SqlContext.Sql().Select<KeyValueDto>().From<KeyValueDto>().Where<KeyValueDto>(x => x.Key == key);
                var dto = scope.Database.Fetch<KeyValueDto>(sql).FirstOrDefault();

                if (dto == null)
                {
                    dto = new KeyValueDto
                    {
                        Key = key,
                        Value = value,
                        Updated = DateTime.Now
                    };

                    scope.Database.Insert(dto);
                }
                else
                {
                    dto.Value = value;
                    dto.Updated = DateTime.Now;
                    scope.Database.Update(dto);
                }

                scope.Complete();
            }
        }

        /// <inheritdoc />
        public bool TrySetValue(string key, string originValue, string newValue)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                scope.WriteLock(Constants.Locks.KeyValues);

                var sql = scope.SqlContext.Sql().Select<KeyValueDto>().From<KeyValueDto>().Where<KeyValueDto>(x => x.Key == key);
                var dto = scope.Database.Fetch<KeyValueDto>(sql).FirstOrDefault();

                if (dto == null || dto.Value != originValue)
                    return false;

                dto.Value = newValue;
                dto.Updated = DateTime.Now;
                scope.Database.Update(dto);

                scope.Complete();
            }

            return true;
        }

        /// <summary>
        /// A custom migration that executes standalone during the Initialize phase of this service.
        /// </summary>
        internal class InitializeMigration : MigrationBase
        {
            public InitializeMigration(IMigrationContext context)
                : base(context)
            { }

            public override void Migrate()
            {
                // as long as we are still running 7 this migration will be invoked,
                // but due to multiple restarts during upgrades, maybe the table
                // exists already
                if (TableExists(Constants.DatabaseSchema.Tables.KeyValue))
                    return;

                Logger.Info<KeyValueRepository>("Creating KeyValue structure.");

                // the locks table was initially created with an identity (auto-increment) primary key,
                // but we don't want this, especially as we are about to insert a new row into the table,
                // so here we drop that identity
                DropLockTableIdentity();

                // insert the lock object for key/value
                Insert.IntoTable(Constants.DatabaseSchema.Tables.Lock).Row(new { id = Constants.Locks.KeyValues, name = "KeyValues", value = 1 }).Do();

                // create the key-value table
                Create.Table<KeyValueDto>().Do();
            }

            private void DropLockTableIdentity()
            {
                // one cannot simply drop an identity, that requires a bit of work

                // create a temp. id column and copy values
                Alter.Table(Constants.DatabaseSchema.Tables.Lock).AddColumn("nid").AsInt32().Nullable().Do();
                Execute.Sql("update umbracoLock set nid = id").Do();

                // drop the id column entirely (cannot just drop identity)
                Delete.PrimaryKey("PK_umbracoLock").FromTable(Constants.DatabaseSchema.Tables.Lock).Do();
                Delete.Column("id").FromTable(Constants.DatabaseSchema.Tables.Lock).Do();

                // recreate the id column without identity and copy values
                Alter.Table(Constants.DatabaseSchema.Tables.Lock).AddColumn("id").AsInt32().Nullable().Do();
                Execute.Sql("update umbracoLock set id = nid").Do();

                // drop the temp. id column
                Delete.Column("nid").FromTable(Constants.DatabaseSchema.Tables.Lock).Do();

                // complete the primary key
                Alter.Table(Constants.DatabaseSchema.Tables.Lock).AlterColumn("id").AsInt32().NotNullable().PrimaryKey("PK_umbracoLock").Do();
            }
        }
    }
}
