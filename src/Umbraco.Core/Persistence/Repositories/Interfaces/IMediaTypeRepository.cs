﻿using System.Collections.Generic;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Querying;

namespace Umbraco.Core.Persistence.Repositories
{
    public interface IMediaTypeRepository : IContentTypeCompositionRepository<IMediaType>
    {
        /// <summary>
        /// Gets all entities of the specified <see cref="PropertyType"/> query
        /// </summary>
        /// <param name="query"></param>
        /// <returns>An enumerable list of <see cref="IContentType"/> objects</returns>
        IEnumerable<IMediaType> GetByQuery(IQuery<PropertyType> query);

        IEnumerable<MoveEventInfo<IMediaType>> Move(IMediaType toMove, EntityContainer container);

        /// <summary>
        /// Derives a unique alias from an existing alias.
        /// </summary>
        /// <param name="alias">The original alias.</param>
        /// <returns>The original alias with a number appended to it, so that it is unique.</returns>
        /// <remarks>Unique accross all content, media and member types.</remarks>
        string GetUniqueAlias(string alias);

        /// <summary>
        /// Extracts a set of properties from a media type into a new composition type
        /// </summary>
        /// <param name="mediaType"><see cref="IMediaType"/> to extract composition from</param>
        /// <param name="compositionMediaType"><see cref="IMediaType"/> to extract composition to</param>
        /// <param name="propertyAliases">Aliases of properties to move to composition type</param>
        void ExtractComposition(IMediaType mediaType, IMediaType compositionMediaType, string[] propertyAliases);

        /// <summary>
        /// Checks to see if a given media type is used as a composition on other content types
        /// </summary>
        /// <param name="mediaTypeId">Id of content type</param>
        /// <returns>True if used as a composition on another type, otherwise false</returns>
        bool IsUsedAsComposition(int mediaTypeId);
    }
}