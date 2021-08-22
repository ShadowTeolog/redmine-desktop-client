namespace Redmine.Client
{
    public interface IVersionRef
    {
        int Id { get; }
        string Name { get; }
    }
    public class VersionRef : IVersionRef
    {
        private Redmine.Net.Api.Types.Version nativeref;

        public int Id => nativeref.Id;
        public string Name => nativeref.Name;
        public VersionRef(Redmine.Net.Api.Types.Version item)
        {
            nativeref = item;
        }
    }
    public class FakeVersionRef : IVersionRef
    {
        
        public int Id => 0;
        public string Name { get; }

        public FakeVersionRef(string name)
        {
            Name = name;
        }
    }
}