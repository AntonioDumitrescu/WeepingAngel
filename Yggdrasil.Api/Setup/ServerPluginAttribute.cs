namespace Yggdrasil.Api.Setup;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServerPluginAttribute : Attribute
{
    public ServerPluginAttribute(string friendlyName, string package, string version, string author)
    {
        FriendlyName = friendlyName;
        Package = package;
        Version = version;
        Author = author;
    }

    public string FriendlyName { get; }
    public string Package { get; }
    public string Version { get; }
    public string Author { get; }
}