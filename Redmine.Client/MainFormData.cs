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

    internal class LoadException : Exception
    {
        public LoadException(String action, Exception innerException) : base(action, innerException)
        {

        }
    }


    internal class MainFormData
    {
        private readonly RedmineClient redmineClient;
        public List<IClientProject> Projects { get; } = new List<IClientProject>();
        public IList<Issue> Issues { get; set; }
        public IList<CustomField> CustomFields { get; }

        // search data
        public List<ITrackerRef> Trackers { get; }

        public List<IIssueCategory> Categories { get; private set; }
        public List<IIssueStatusRef> Statuses { get;}
        public List<IVersionRef> Versions { get;}
        public List<ProjectMember> ProjectMembers { get;}
        public List<Enumerations.EnumerationItem> IssuePriorities { get;}
        public List<Enumerations.EnumerationItem> Activities { get;}
        public int ProjectId { get; }

        public MainFormData(RedmineClient redmineClient, IList<Project> projects, int projectId, bool onlyMe, Filter filter)
        {
            this.redmineClient = redmineClient;
            ProjectId = projectId;
            Projects.Add(new FakeClientProject(FakeClientProject.FakeProjectId.AllIssues, Languages.Lang.ShowAllIssues));
            Projects.AddRange(projects.Select(i=> new ClientProject(i)));

            if (RedmineClientForm.RedmineVersion >= ApiVersion.V13x)
            {
                if (projectId < 0) //fake project 
                {
                    try
                    {
                        
                        Trackers = redmineClient.GetAllTrackersAsRefs();
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
                        Trackers = redmineClient.GetProjectTrackersAsRefs(projectId);
                        
                    }
                    catch (Exception e)
                    {
                        throw new LoadException(Languages.Lang.BgWork_LoadProjectTrackers, e);
                    }

                    FillCategories();

                    try
                    {
                        Versions = RedmineClientForm.redmine.GetObjects<Redmine.Net.Api.Types.Version>(InitParameters()).Select(i=>(IVersionRef)new VersionRef(i)).ToList();
                        Versions.Add(new FakeVersionRef(string.Empty));
                    }
                    catch (Exception e)
                    {
                        throw new LoadException(Languages.Lang.BgWork_LoadVersions, e);
                    }
                }
                Trackers.Insert(0, new FakeTrackerRef(String.Empty));

                try
                {
                    Statuses = new List<IIssueStatusRef>();
                    Statuses.Add(new FakeStatusRef(0, Languages.Lang.AllOpenIssues));
                    Statuses.AddRange(RedmineClientForm.redmine.GetObjects<IssueStatus>(InitParameters()).Select(i=>new IssueStatusRef(i)));
                    Statuses.Add(new FakeStatusRef(-1, Languages.Lang.AllClosedIssues));
                    Statuses.Add(new FakeStatusRef(-2,Languages.Lang.AllOpenAndClosedIssues));
                }
                catch (Exception e)
                {
                    throw new LoadException(Languages.Lang.BgWork_LoadStatuses, e);
                }

                try
                {
                    if (RedmineClientForm.RedmineVersion >= ApiVersion.V14x && projectId > 0)
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

                    CustomFields = redmineClient.FetchCustomFields();
                    
                }
                catch (Exception e)
                {
                    throw new LoadException(Languages.Lang.BgWork_LoadCustomFields, e);
                }
            }

            try
            {
                var parameters = InitParameters();
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
                Categories = redmineClient.FetchIssueCategoryRefsWithFakeItems(ProjectId);
                
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
