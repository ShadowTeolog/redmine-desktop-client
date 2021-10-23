using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public class ConnectionCashed
    {
        public List<Enumerations.EnumerationItem> IssuePriorities { get; private set; }
        public List<Enumerations.EnumerationItem> Activities { get; private set; }

        public void RefreshIssuePriorities(List<Enumerations.EnumerationItem> newcollection)
        {
            var issuePriorities = new List<Enumerations.EnumerationItem>
            {
                new Enumerations.EnumerationItem { Id = 0, Name = "", IsDefault = false }
            };
            issuePriorities.AddRange(newcollection);
            IssuePriorities = issuePriorities;
        }
        public void RefreshActivity(List<Enumerations.EnumerationItem> newcollection)
        {
            var activities = new List<Enumerations.EnumerationItem>
            {
                new Enumerations.EnumerationItem { Id = 0, Name = "", IsDefault = false }
            };
            activities.AddRange(newcollection);
            Activities = activities;
        }

    }
    public class RedmineClient : IDisposable
    {
        public RedmineManager redmine;
        public ConnectionCashed Cache { get; } = new ConnectionCashed();

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
            var parameters = ProjectParametersFilter(projectId);
            var nativelist = redmine.GetObjects<IssueCategory>(parameters);
            var result = new List<IIssueCategory> { new FakeIssueCategory("") };
            result.AddRange(nativelist.Select(i => (IIssueCategory)new IssueCategoryDescriptor(i)));
            return result;
        }

        private static NameValueCollection ProjectParametersFilter(int projectId)
        {
            var parameters = (projectId > 0)
                ? new NameValueCollection { { RedmineKeys.PROJECT_ID, projectId.ToString() } }
                : null;
            return parameters;
        }

        public List<Issue> FetchIssueHeadersWithFilter(string projectIdentity, Filter filter)
        {
            var parameters = new NameValueCollection();
            if (!string.IsNullOrWhiteSpace(projectIdentity))
                parameters.Add(RedmineKeys.PROJECT_ID, projectIdentity);
            if (filter.onlyMe)
                parameters.Add(RedmineKeys.ASSIGNED_TO_ID, "me");
            else if (filter.AssignedToId > 0)
                parameters.Add(RedmineKeys.ASSIGNED_TO_ID, filter.AssignedToId.ToString());

            if (filter.TrackerId > 0)
                parameters.Add(RedmineKeys.TRACKER_ID, filter.TrackerId.ToString());

            if (filter.StatusId > 0)
                parameters.Add(RedmineKeys.STATUS_ID, filter.StatusId.ToString());
            else if (filter.StatusId < 0)
            {
                switch (filter.StatusId)
                {
                    case -1: // all closed issues
                        parameters.Add(RedmineKeys.STATUS_ID, "closed");
                        break;

                    case -2: // all open and closed issues
                        parameters.Add(RedmineKeys.STATUS_ID, " *");
                        break;
                }
            }

            if (filter.PriorityId > 0)
                parameters.Add(RedmineKeys.PRIORITY_ID, filter.PriorityId.ToString());

            if (filter.VersionId > 0)
                parameters.Add(RedmineKeys.FIXED_VERSION_ID, filter.VersionId.ToString());

            if (filter.CategoryId > 0)
                parameters.Add(RedmineKeys.CATEGORY_ID, filter.CategoryId.ToString());

            if (!String.IsNullOrEmpty(filter.Subject))
                parameters.Add(RedmineKeys.SUBJECT, "~" + filter.Subject);
            return redmine.GetObjects<Issue>(parameters);

        }

        public List<IssueStatus> GetNativeIssueStatusList(int projectId)
        {
            var parameters = ProjectParametersFilter(projectId);
            return redmine.GetObjects<IssueStatus>(parameters);
        }
        public List<IIssueStatusRef> FetchIssueStatusListRefsWithFakeItems(int projectId)
        {
            var nativeStatusList = GetNativeIssueStatusList(projectId);
            var result= new List<IIssueStatusRef> { new FakeStatusRef(0, Languages.Lang.AllOpenIssues) };
            result.AddRange(nativeStatusList.Select(i => new IssueStatusRef(i)));
            result.Add(new FakeStatusRef(-1, Languages.Lang.AllClosedIssues));
            result.Add(new FakeStatusRef(-2, Languages.Lang.AllOpenAndClosedIssues));
            return result;
        }

        public List<IVersionRef> FetchVersionListRefsWithFakeItems(int projectId)
        {
            var parameters = ProjectParametersFilter(projectId);
            var result= redmine.GetObjects<Redmine.Net.Api.Types.Version>(parameters).Select(i => (IVersionRef)new VersionRef(i)).ToList();
            result.Add(new FakeVersionRef(string.Empty));
            return result;
        }

        public List<ProjectMember> FetchUserListWithProjectFilterAndFakeItem(int projectId)
        {
            var userList = new List<ProjectMember> { new ProjectMember() };

            if (RedmineClientForm.RedmineVersion >= ApiVersion.V14x && projectId > 0)
            {
                var parameters = ProjectParametersFilter(projectId);
                var projectMembers = redmine.GetObjects<ProjectMembership>(parameters);
                userList.AddRange(projectMembers.Select(i=>new ProjectMember(i)));
            }
            else
            {
                var allUsers = redmine.GetObjects<User>();
                userList.AddRange(allUsers.Select(i => new ProjectMember(i)));
            }
            return userList;
        }

        public void RefreshIssuePrioritiesAndActivities()
        {
            if (RedmineClientForm.RedmineVersion >= ApiVersion.V22x)
            {
                Enumerations.UpdateIssuePriorities(redmine.GetObjects<IssuePriority>());
                Enumerations.SaveIssuePriorities();

                Enumerations.UpdateActivities(redmine.GetObjects<TimeEntryActivity>());
                Enumerations.SaveActivities();
            }
            Cache.RefreshIssuePriorities(Enumerations.IssuePriorities);
            Cache.RefreshActivity(Enumerations.Activities);

            
        }

        public Project FetchProjectWithTrackers(int projectId)
        {
            var projectParameters = new NameValueCollection { { "include", "trackers" } };
            return redmine.GetObject<Project>(projectId.ToString(), projectParameters);
        }

        public Upload UploadLocalFile(string localfilepath)
        {
            var file = System.IO.File.ReadAllBytes(localfilepath);
            return redmine.UploadFile(file);
        }

        public Issue CreateIssue(Issue newIssue)
        {
            return redmine.CreateObject<Issue>(newIssue);
        }
    }
}