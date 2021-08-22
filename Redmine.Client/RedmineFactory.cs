using Redmine.Net.Api;

namespace Redmine.Client
{
    public static class RedmineFactory
    {
        public static RedmineClient Connect(string redmineUrl, string redmineUser, string redminePassword, MimeFormat defaultCommunicationType)
        {
            var nativeconnection= new RedmineManager(redmineUrl, redmineUser, redminePassword, defaultCommunicationType);
            return new RedmineClient(nativeconnection);
        }

        public static RedmineClient Connect(string redmineUrl, MimeFormat defaultCommunicationType)
        {
            var nativeconnection = new RedmineManager(redmineUrl, defaultCommunicationType);
            return new RedmineClient(nativeconnection);
        }
    }
}