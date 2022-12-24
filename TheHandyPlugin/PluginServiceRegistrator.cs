using Jellyfin.TheHandy.Notifiers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.TheHandy;

/// <summary>
/// Register webhook services.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection)
    {
        
        // Session consumers.
        // serviceCollection.AddScoped<IEventConsumer<PlaybackStartEventArgs>, PlaybackStartNotifier>();
        // serviceCollection.AddScoped<IEventConsumer<PlaybackStopEventArgs>, PlaybackStopNotifier>();
        serviceCollection.AddScoped<IEventConsumer<PlaybackProgressEventArgs>, PlaybackProgressNotifier>();
        // serviceCollection.AddScoped<IEventConsumer<SessionStartedEventArgs>, SessionStartNotifier>();
    }
}