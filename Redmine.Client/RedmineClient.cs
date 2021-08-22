using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public class RedmineClient : IDisposable
    {
        public RedmineManager redmine;

        public RedmineClient(RedmineManager nativeconnection)
        {
            redmine = nativeconnection;
        }

        public void Dispose()
        {
            //how to disconnect?
        }

        public IList<Project> FetchAllProjects()
        {
            var parameters = new NameValueCollection();
            return redmine.GetObjects<Project>(parameters);
        }

        public IList<Project> FetchMyProjects(User user)
        {
            var parameters = new NameValueCollection();
            return redmine.GetObjects<Project>(parameters)
                .Where(project => user.Memberships.Any(m => project.Id == m.Project.Id))
                .ToList();
        }

        public List<ITrackerRef> GetAllTrackersAsRefs()
        {
            return redmine.GetObjects<Tracker>().
                Select(i => (ITrackerRef)new TrackerRef(i))
                .ToList();
        }

        public void CreateTimeEntry(TimeEntry entry) 
            => redmine.CreateObject(entry);
        public void UpdateTimeEntry(int id, TimeEntry curTimeEntry)
        {
            redmine.UpdateObject(id.ToString(), curTimeEntry);
        }
        public IList<TimeEntry> GetTimeEntriesForIssue(int issueId)
        {
            var parameters = new NameValueCollection { { "issue_id", issueId.ToString() } };
            return redmine.GetObjects<TimeEntry>(parameters);
        }
        public void DeleteTimeEntry(int timeEntryId) => redmine.DeleteObject<TimeEntry>(timeEntryId.ToString());

        public List<ITrackerRef> GetProjectTrackersAsRefs(int projectId)
        {
            var project = redmine.GetObject<Project>(projectId.ToString(),
                new NameValueCollection { { "include", "trackers" } });
            return project.Trackers.Select(i => (ITrackerRef)new ProjectTrackerRef(i)).ToList();
        }

        public User GetUserAndMembership()
        {
            var parameters = new NameValueCollection { { "include", "memberships" } };
            return redmine.GetCurrentUser(parameters);
        }

        /// <summary>
        /// fetch issue with lot of additional data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Issue FetchIssue(int id)
        {
            var nameValueCollection = new NameValueCollection
            {
                { RedmineKeys.INCLUDE, $"{RedmineKeys.CHILDREN},{RedmineKeys.ATTACHMENTS},{RedmineKeys.RELATIONS},{RedmineKeys.CHANGE_SETS},{RedmineKeys.JOURNALS},{RedmineKeys.WATCHERS}" }
            };
            return redmine.GetObject<Issue>(id.ToString(), nameValueCollection);
        }

        /// <summary>
        /// fetch issue with minimal data
        /// </summary>
        /// <param name="issueId"></param>
        /// <returns></returns>
        public Issue FetchIssueHeader(int issueId) => redmine.GetObject<Issue>(issueId.ToString(), null);

        public void UpdateIssue(int IssueId, Issue newIssue)
        {
            redmine.UpdateObject<Issue>(IssueId.ToString(), newIssue);
        }

        public IdentifiableName ResolveIssueStatusIdToName(int idState)
        {
            var info=redmine.GetObjects<IssueStatus>().FirstOrDefault(i=>i.Id== idState);
            return info == null ? StrangeCallHelper.CreateIdentifiableName(info.Id, info.Name) : null;
        }

        public List<CustomField> FetchCustomFields()
        {
            return RedmineClientForm.RedmineVersion >= ApiVersion.V24x 
                ? redmine.GetObjects<CustomField>()
                : new List<CustomField>();
        }

        public List<IIssueCategory> FetchIssueCategoryRefsWithFakeItems(int projectId)
        {
            var parameters = (projectId > 0)
                ? new NameValueCollection { { RedmineKeys.PROJECT_ID, projectId.ToString() } }
                : null;
            var nativelist = redmine.GetObjects<IssueCategory>(parameters);
            var result = new List<IIssueCategory> { new FakeIssueCategory("") };
            result.AddRange(nativelist.Select(i => (IIssueCategory)new IssueCategoryDescriptor(i)));
            return result;
        }
    }
}