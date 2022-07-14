namespace Yggdrasil.Api.Setup;

/// <summary>
///     Used to create a dependency on another plugin.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PackageDependencyAttribute : Attribute
{
    public string Package { get; }

    public DependencyType Type { get; }

    public enum DependencyType
    {
        /// <summary>
        ///     The plugin can still function without the dependency plugin.
        /// </summary>
        Optional,
        /// <summary>
        ///     The plugin cannot be loaded without the dependency plugin.
        /// </summary>
        Required
    }
    /// <summary>
    ///     Creates a new instance of the PackageDependencyAttribute class.
    /// </summary>
    /// <param name="package">The package to depend on. It is the one passed to the plugin attribute.</param>
    /// <param name="type"></param>

    public PackageDependencyAttribute(string package, DependencyType type = DependencyType.Required)
    {
        Package = package;
        Type = type;
    }
}