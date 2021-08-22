using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public interface IIssueStatusRef
    {
        int Id { get; }
        string Name { get; }
    }
    class IssueStatusRef : IIssueStatusRef
    {
        private IssueStatus nativeref;
        public int Id => nativeref.Id;
        public string Name => nativeref.Name;
        public IssueStatusRef(IssueStatus nativeref)
        {
            this.nativeref = nativeref;
        }
    }

    class FakeStatusRef : IIssueStatusRef
    {
        public int Id { get; }
        public string Name { get; }

        public FakeStatusRef(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}