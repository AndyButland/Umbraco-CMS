using System.Linq;
using Umbraco.Core.Persistence.Dtos;

namespace Umbraco.Core.Models.Membership
{
    public static class UserGroupExtensions
    {
        public static IReadOnlyUserGroup ToReadOnlyGroup(this UserGroupDto group)
        {
            return new ReadOnlyUserGroup(group.Id, group.Name, group.Icon,
                group.StartContentId, group.StartMediaId, group.Alias,
                group.UserGroup2AppDtos.Select(x => x.AppAlias).ToArray(),
                group.DefaultPermissions == null ? Enumerable.Empty<string>() : group.DefaultPermissions.ToCharArray().Select(x => x.ToString()));
        }
    }
}
