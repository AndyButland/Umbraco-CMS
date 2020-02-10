using System;
using System.Linq;
using Umbraco.Core.Configuration;
using Umbraco.Core.Persistence.Repositories;

namespace Umbraco.Core.Services.Implement
{
    internal class KeyValueService : IKeyValueService
    {
        private readonly object _initialock = new object();        
        private readonly IKeyValueRepository _keyValueRepository;
        private readonly IUmbracoVersion _umbracoVersion;
        private bool _initialized;

        public KeyValueService(IKeyValueRepository keyValueRepository, IUmbracoVersion umbracoVersion)
        {
            _keyValueRepository = keyValueRepository;
            _umbracoVersion = umbracoVersion;
        }

        private void EnsureInitialized()
        {
            lock (_initialock)
            {
                if (_initialized) return;
                Initialize();
            }
        }

        private void Initialize()
        {
            // the key/value service is entirely self-managed, because it is used by the
            // upgrader and anything we might change need to happen before everything else

            // if already running 8, either following an upgrade or an install,
            // then everything should be ok (the table should exist, etc)

            if (_umbracoVersion.LocalVersion != null && _umbracoVersion.LocalVersion.Major >= 8)
            {
                _initialized = true;
                return;
            }

            // else we are upgrading from 7, we can assume that the locks table
            // exists, but we need to create everything for key/value

            _keyValueRepository.PerformInitialization();

            // but don't assume we are initializing
            // we are upgrading from v7 and if anything goes wrong,
            // the table and everything will be rolled back
        }

        /// <inheritdoc />
        public string GetValue(string key)
        {
            EnsureInitialized();
            return _keyValueRepository.GetValue(key);
        }

        /// <inheritdoc />
        public void SetValue(string key, string value)
        {
            EnsureInitialized();
            _keyValueRepository.SetValue(key, value);
        }

        /// <inheritdoc />
        public void SetValue(string key, string originValue, string newValue)
        {
            if (!TrySetValue(key, originValue, newValue))
                throw new InvalidOperationException("Could not set the value.");
        }

        /// <inheritdoc />
        public bool TrySetValue(string key, string originValue, string newValue)
        {
            EnsureInitialized();
            return _keyValueRepository.TrySetValue(key, originValue, newValue);
        }

        /// <summary>
        /// Gets a value directly from the database, no scope, nothing.
        /// </summary>
        /// <remarks>Used by <see cref="Runtime.CoreRuntime"/> to determine the runtime state.</remarks>
        internal static string GetValue(IUmbracoDatabase database, string key)
        {
            if (database is null) return null;

            var sql = database.SqlContext.Sql()
                .Select<KeyValueDto>()
                .From<KeyValueDto>()
                .Where<KeyValueDto>(x => x.Key == key);
            return database.FirstOrDefault<KeyValueDto>(sql)?.Value;
        }
    }
}
