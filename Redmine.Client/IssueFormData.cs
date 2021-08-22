using System;
using System.Collections.Generic;
using System.Text;
using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    /// <summary>
    /// Per project data cache
    /// </summary>
    internal class IssueFormData
    {
        public IList<ProjectTracker> Trackers { get; set; }
        public List<IIssueCategory> Categories { get; set; }
        public IList<IssueStatus> Statuses { get; set; }
//        public List<IdentifiableName> Priorities { get; set; }
        public List<IVersionRef> Versions { get; set; }
//        public List<Assignee> Watchers { get; set; }
        public List<ProjectMember> ProjectMembers { get; set; }
        public IList<IssuePriority> IssuePriorities { get; set; }
        public IList<TimeEntry> TimeEntries { get; set; }
    }
}
