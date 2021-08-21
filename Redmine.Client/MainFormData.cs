using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Redmine.Net.Api.Types;
using Redmine.Net.Api;

namespace Redmine.Client
{
    public enum ApiVersion
    {
        V10x,
        V11x,
        V12x,
        V13x,
        V14x,
        V20x,
        V21x,
        V22x,
        V23x,
        V24x,
        V25x,
        V41x
    }

    public class Filter : ICloneable
    {
        public int TrackerId = 0;
        public int StatusId = 0;
        public int PriorityId = 0;
        public string Subject = "";
        public int AssignedToId = 0;
        public int VersionId = 0;
        public int CategoryId = 0;

        #region ICloneable Members

        public object Clone()
        {
            return new Filter { TrackerId = TrackerId, StatusId = StatusId, PriorityId = PriorityId, Subject = Subject, AssignedToId = AssignedToId, VersionId = VersionId, CategoryId = CategoryId };
        }

        #endregion ICloneable Members
    }

    internal class LoadException : Exception
    {
        public LoadException(String action, Exception innerException) : base(action, innerException)
{

}
    }

    internal class MainFormData
    {
        public List<IClientProject> Projects { get;}
        public IList<Issue> Issues { get; set; }
        public IList<CustomField> CustomFields { get; }

        // search data
        public List<ITrackerRef> Trackers { get; }

        public List<IIssueCategory> Categories { get; private set; }
        public List<IssueStatus> Statuses { get;}
        public List<Redmine.Net.Api.Types.Version> Versions { get;}
        public List<ProjectMember> ProjectMembers { get;}
        public List<Enumerations.EnumerationItem> IssuePriorities { get;}
        public List<Enumerations.EnumerationItem> Activities { get;}
        public int ProjectId { get; }

        public MainFormData(IList<Project> projects, int projectId, bool onlyMe, Filter filter)
        {
            ProjectId = projectId;
            Projects = new List<IClientProject>();
            Projects.Add(new FakeClientProject(FakeClientProject.FakeProjectId.AllIssues, Languages.Lang.ShowAllIssues));
            foreach(var p in projects)
            {
                Projects.Add(new ClientProject(p));
            }
            if (RedmineClientForm.RedmineVersion >= ApiVersion.V13x)
            {
                if (projectId < 0) //fake project 
                {
                    try
                    {
                        var allTrackers = RedmineClientForm.redmine.GetObjects<Tracker>();
                        Trackers = allTrackers.Select(i=>(ITrackerRef)new TrackerRef(i)).ToList();
                    }
                    catch (Exception e)
                    {
                        throw new LoadException(Languages.Lang.BgWork_LoadTrackers, e);
                    }
                    Categories = null;
                    Versions = null;
                }
                else
                {
                    try
                    {
                        var project = RedmineClientForm.redmine.GetObject<Project>(projectId.ToString(),
                            new NameValueCollection { { "include", "trackers" } });
                        Trackers = project.Trackers.Select(i=>(ITrackerRef)new ProjectTrackerRef(i)).ToList();
                    }
                    catch (Exception e)
                    {
                        throw new LoadException(Languages.Lang.BgWork_LoadProjectTrackers, e);
                    }

                    FillCategories();

                    try
                    {
                        Versions = RedmineClientForm.redmine.GetObjects<Redmine.Net.Api.Types.Version>(InitParameters());
                        Versions.Insert(0, new Redmine.Net.Api.Types.Version { Id = 0, Name = "" });
                    }
                    catch (Exception e)
                    {
                        throw new LoadException(Languages.Lang.BgWork_LoadVersions, e);
                    }
                }
                Trackers.Insert(0, new FakeTrackerRef(String.Empty));

                try
                {
                    Statuses = new List<IssueStatus>(RedmineClientForm.redmine.GetObjects<IssueStatus>(InitParameters()));
                    Statuses.Insert(0, new IssueStatus { Id = 0, Name = Languages.Lang.AllOpenIssues });
                    Statuses.Add(new IssueStatus { Id = -1, Name = Languages.Lang.AllClosedIssues });
                    Statuses.Add(new IssueStatus { Id = -2, Name = Languages.Lang.AllOpenAndClosedIssues });
                }
                catch (Exception e)
                {
                    throw new LoadException(Languages.Lang.BgWork_LoadStatuses, e);
                }

                try
                {
                    if (RedmineClientForm.RedmineVersion >= ApiVersion.V14x
                        && projectId > 0)
                    {
                        List<ProjectMembership> projectMembers = (List<ProjectMembership>)RedmineClientForm.redmine.GetObjects<ProjectMembership>(InitParameters());
                        ProjectMembers = projectMembers.ConvertAll(new Converter<ProjectMembership, ProjectMember>(ProjectMember.MembershipToMember));
                    }
                    else
                    {
                        var allUsers = RedmineClientForm.redmine.GetObjects<User>();
                        ProjectMembers = allUsers.ConvertAll(UserToProjectMember);
                    }
                    ProjectMembers.Insert(0, new ProjectMember());
                }
                catch (Exception)
                {
                    ProjectMembers = null;
                    //throw new LoadException(Languages.Lang.BgWork_LoadProjectMembers, e);
                }

                try
                {
                    if (RedmineClientForm.RedmineVersion >= ApiVersion.V22x)
                    {
                        Enumerations.UpdateIssuePriorities(RedmineClientForm.redmine.GetObjects<IssuePriority>());
                        Enumerations.SaveIssuePriorities();

                        Enumerations.UpdateActivities(RedmineClientForm.redmine.GetObjects<TimeEntryActivity>());
                        Enumerations.SaveActivities();
                    }
                    IssuePriorities = new List<Enumerations.EnumerationItem>(Enumerations.IssuePriorities);
                    IssuePriorities.Insert(0, new Enumerations.EnumerationItem { Id = 0, Name = "", IsDefault = false });

                    Activities = new List<Enumerations.EnumerationItem>(Enumerations.Activities);
                    Activities.Insert(0, new Enumerations.EnumerationItem { Id = 0, Name = "", IsDefault = false });
                }
                catch (Exception e)
                {
                    throw new LoadException(Languages.Lang.BgWork_LoadPriorities, e);
                }

                try
                {
                    if (RedmineClientForm.RedmineVersion >= ApiVersion.V24x)
                    {
                        CustomFields = RedmineClientForm.redmine.GetObjects<CustomField>();
                    }
                }
                catch (Exception e)
                {
                    throw new LoadException(Languages.Lang.BgWork_LoadCustomFields, e);
                }
            }

            try
            {
                NameValueCollection parameters = InitParameters();
                if (onlyMe)
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

                Issues = RedmineClientForm.redmine.GetObjects<Issue>(parameters);
                }
            catch (Exception e)
            {
                throw new LoadException(Languages.Lang.BgWork_LoadIssues, e);
            }
        }

        private void FillCategories()
        {
            try
            {
                var nativelist = RedmineClientForm.redmine.GetObjects<IssueCategory>(InitParameters());
                Categories = nativelist.Select(i=>(IIssueCategory)new IssueCategoryDescriptor(i)).ToList();
                Categories.Insert(0, new FakeIssueCategory(""));
            }
            catch (Exception e)
            {
                throw new LoadException(Languages.Lang.BgWork_LoadCategories, e);
            }
        }

        private NameValueCollection InitParameters()
        {
            var parameters = new NameValueCollection();
            if (ProjectId != -1)
                parameters.Add(RedmineKeys.PROJECT_ID, ProjectId.ToString());
            return parameters;
        }

        private static ProjectTracker TrackerToProjectTracker(Tracker tracker)
        {
            return new ProjectTracker { Id = tracker.Id, Name = tracker.Name };
        }

        private static ProjectMember UserToProjectMember(User user)
        {
            return new ProjectMember(user);
        }

        public static Dictionary<int, T> ToDictionaryId<T>(IList<T> list) where T : Identifiable<T>, System.IEquatable<T>
        {
            Dictionary<int, T> dict = new Dictionary<int,T>();
            foreach (T element in list)
            {
                dict.Add(element.Id, element);
            }
            return dict;
        }

        public static Dictionary<int, Y> ToDictionaryName<Y>(IList<Y> list) where Y : IdentifiableName
        {
            Dictionary<int, Y> dict = new Dictionary<int, Y>();
            foreach (Y element in list)
            {
                dict.Add(element.Id, element);
            }
            return dict;
        }
    }
}
