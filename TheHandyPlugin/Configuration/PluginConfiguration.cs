using MediaBrowser.Model.Plugins;

namespace Jellyfin.TheHandy.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // set default options here
        this.ConnectionKey = "string";
    }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public string ConnectionKey { get; set; }

}
