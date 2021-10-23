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
        
        public int ProjectId { get; }

        public MainFormData(RedmineClient redmineClient, IList<Project> projects, int projectId, bool onlyMe, Filter filter)
        {
            var projectindentity = projects.FirstOrDefault(p => p.Id == projectId)?.Identifier;
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
                        Versions = redmineClient.FetchVersionListRefsWithFakeItems(projectId);
                    }
                    catch (Exception e)
                    {
                        throw new LoadException(Languages.Lang.BgWork_LoadVersions, e);
                    }
                }
                Trackers.Insert(0, new FakeTrackerRef(String.Empty));

                try
                {
                    Statuses=redmineClient.FetchIssueStatusListRefsWithFakeItems(ProjectId);
                    
                }
                catch (Exception e)
                {
                    throw new LoadException(Languages.Lang.BgWork_LoadStatuses, e);
                }

                try
                {
                    ProjectMembers = redmineClient.FetchUserListWithProjectFilterAndFakeItem(projectId);
                    
                }
                catch (Exception)
                {
                    ProjectMembers = null;
                    //throw new LoadException(Languages.Lang.BgWork_LoadProjectMembers, e);
                }

                try
                {
                    this.redmineClient.RefreshIssuePrioritiesAndActivities();
                    
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
                
                filter.onlyMe = onlyMe;
                Issues =  redmineClient.FetchIssueHeadersWithFilter(projectindentity, filter) ;
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
