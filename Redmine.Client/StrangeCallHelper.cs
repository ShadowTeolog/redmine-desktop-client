using Redmine.Net.Api.Types;

namespace Redmine.Client
{
    public static class StrangeCallHelper
    {
        public static IdentifiableName CreateIdentifiableName(int Id, string name)
        {
            var result = IdentifiableName.Create<IdentifiableName>(Id);
            result.Name = name;
            return result;
        } 
    }
}