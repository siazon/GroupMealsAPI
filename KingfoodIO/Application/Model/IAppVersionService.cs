using System.Reflection;

namespace KingfoodIO.Application.Model
{
    public interface IAppVersionService
    {
        string Version { get; }
    }
    public class AppVersionService : IAppVersionService
    {
        public string Version =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
