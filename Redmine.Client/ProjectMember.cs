using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    /// <summary>
    /// Interface for showing Assignee information
    /// </summary>
    public interface IProjectMember
    {
        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <value>The id of the assignee.</value>
        int Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name of the assignee.</value>
        string Name { get; }
    }

    public class ProjectMember : IProjectMember
    {
        private IdentifiableName User;
        private IdentifiableName Group;

        public ProjectMember()
        {
            User = IdentifiableName.Create<IdentifiableName>(0);
            User.Name=string.Empty;
        }
        public ProjectMember(User user)
        {
            User = IdentifiableName.Create<IdentifiableName>(user.Id);
            User.Name = user.CompleteName();
        }
        public ProjectMember(ProjectMembership projectMember)
        {
            User = projectMember.User;
            Group = projectMember.Group;
        }
        public int Id => User?.Id ?? Group.Id;
        public string Name => User?.Name ?? Group.Name;


        public static ProjectMember MembershipToMember(ProjectMembership projectMember)
        {
            return new ProjectMember(projectMember);
        }

    }

}
