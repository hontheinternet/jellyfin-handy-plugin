
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Common.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jellyfin.TheHandy.Configuration;
using Microsoft.Extensions.Logging;


namespace Jellyfin.TheHandy.Notifiers;


/// <summary>
/// Playback start notifier.
/// </summary>
public class PlaybackProgressNotifier : IEventConsumer<PlaybackProgressEventArgs>
{
    private readonly IServerApplicationHost _applicationHost;
    private readonly ILogger<PlaybackProgressNotifier> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private static TheHandyPlugin Instance =>
        TheHandyPlugin.Instance!;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackProgressNotifier"/> class.
    /// </summary>
    /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{GenericClient}"/> interface.</param>
    public PlaybackProgressNotifier(
        IServerApplicationHost applicationHost,
        IHttpClientFactory httpClientFactory,
        ILogger<PlaybackProgressNotifier> logger)
    {
        _applicationHost = applicationHost;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task OnEvent(PlaybackProgressEventArgs eventArgs)
    {
        if (eventArgs.Item is null)
        {
            return;
        }

        if (eventArgs.Item.IsThemeMedia)
        {
            // Don't report theme song or local trailer playback.
            return;
        }

        if (eventArgs.Users.Count == 0)
        {
            // No users in playback session.
            return;
        }

        // Current path
        if (!(eventArgs.MediaInfo.Path is null)) {
            PlaybackChange change = PlaybackChange.PlaybackStart;
            if (eventArgs.IsPaused) {
                change = PlaybackChange.PlaybackStop;
            }
            await Instance.HandleEvent(eventArgs, change);
        }
    }

    
}