using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public interface ITrackerRef
    {
        string Name { get; }
        int Id { get; }
    }
    public class TrackerRef : ITrackerRef
    {
        private readonly Tracker tracker;

        public TrackerRef(Tracker tracker)
        {
            this.tracker = tracker;
        }

        public string Name => tracker.Name;
        public int Id => tracker.Id;
    }
    public class ProjectTrackerRef : ITrackerRef
    {
        private readonly ProjectTracker tracker;

        public ProjectTrackerRef(ProjectTracker tracker)
        {
            this.tracker = tracker;
        }

        public string Name => tracker.Name;
        public int Id => tracker.Id;
    }
    public class FakeTrackerRef : ITrackerRef
    {
        public FakeTrackerRef(string name)
        {
            Name = name;
        }

        public string Name {get;}
        public int Id => 0;
    }
}