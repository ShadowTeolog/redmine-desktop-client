using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public interface IIssueCategory
    {
        string Name { get; }
        int Id { get; }
    }
    public class IssueCategoryDescriptor : IIssueCategory
    {
        private IssueCategory real;

        public IssueCategoryDescriptor(IssueCategory c)
        {
            real = c;
        }

        public string Name => real.Name;
        public int Id => real.Id;
    }
    public class FakeIssueCategory : IIssueCategory
    {
        public string Name { get; }
        public int Id => 0;
        public FakeIssueCategory(string name)
        {
            Name = name;
        }
    }
}