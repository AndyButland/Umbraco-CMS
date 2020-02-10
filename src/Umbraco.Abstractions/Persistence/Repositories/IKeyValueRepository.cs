using System;
using System.Collections.Generic;
using System.Text;

namespace Umbraco.Core.Persistence.Repositories
{
    public interface IKeyValueRepository
    {
        /// <summary>
        /// Performs initialization of database structure.
        /// </summary>
        void PerformInitialization();

        /// <summary>
        /// Gets a value.
        /// </summary>
        /// <remarks>Returns <c>null</c> if no value was found for the key.</remarks>
        string GetValue(string key);

        /// <summary>
        /// Sets a value.
        /// </summary>
        void SetValue(string key, string value);

        /// <summary>
        /// Tries to set a value.
        /// </summary>
        /// <remarks>Sets the value to <paramref name="newValue"/> if the value is <paramref name="originValue"/>,
        /// and returns true; otherwise returns false. In other words, ensures that the value has not changed
        /// before setting it.</remarks>
        bool TrySetValue(string key, string originValue, string newValue);
    }
}
