using System;
using System.Collections.Generic;
using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public interface IClientProject
    {
        int Id { get; }
        string Name { get; }
        string Identifier { get; }
    }
    public class ClientProject : IClientProject
    {
        private readonly Project p;

        public int Id => p.Id;
        public string Name => p.Name;
        public string Identifier => p.Identifier;
        public string Description => p.Description;
        public IdentifiableName Parent => p.Parent;
        public string HomePage => p.HomePage;
        public DateTime? CreatedOn => p.CreatedOn;
        public DateTime? UpdatedOn => p.UpdatedOn;
        public IList<ProjectTracker> Trackers => p.Trackers;
        public IList<IssueCustomField> CustomFields => p.CustomFields;
        public ClientProject(Project p) {
            this.p = p;
        }

        public string DisplayName {
            get {
                if (Parent != null)
                    return Parent.Name + " - " + Name;
                return Name;
            }
        }
    }

    public class FakeClientProject : IClientProject
    {
        private readonly FakeProjectId id;
        public string Name { get; }
        public string Identifier => null; //no identity


        public enum FakeProjectId
        {
            AllIssues=-1
        }
        public FakeClientProject(FakeProjectId id,string name)
        {
            this.id = id;
            Name = name;
        }

        public int Id => (int)id;
        
    }
}