using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.TheHandy.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Net;
using Newtonsoft.Json.Linq;

namespace Jellyfin.TheHandy;

/// <summary>
/// The main plugin.
/// </summary>
public class TheHandyPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TheHandyPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TheHandyPlugin}"/> interface.</param>
    public TheHandyPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, 
        IHttpClientFactory httpClientFactory,
        ILogger<TheHandyPlugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        states = new Dictionary<string, TheHandySessionState>();
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static TheHandyPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5dfbd8b1-7439-4f3a-ac9f-69525d349d48");

    /// <inheritdoc />
    public override string Name => "TheHandy";

    private readonly ILogger<TheHandyPlugin> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    
    private readonly string UploadURI = "https://www.handyfeeling.com/api/sync/upload";
    private readonly string URL_BASE = "https://www.handyfeeling.com/";
    private readonly string URL_API_ENDPOINT = "api/v1/";


    private Dictionary<string, TheHandySessionState> states; 

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", this.GetType().Namespace)
            }
        };
    }


    public async Task HandleEvent(PlaybackProgressEventArgs eventArgs, PlaybackChange change) {
        _logger.LogWarning("Event: {@int}", states.Keys.Count);
        if (states.Keys.Count > 0) {
            _logger.LogWarning("Event: {@string}", states.Keys.First<string>());
        }
        
        if (!states.ContainsKey(eventArgs.MediaInfo.Path)) {
            states.Add(eventArgs.MediaInfo.Path, new TheHandySessionState());
        }

        var CurrentSessionState = states[eventArgs.MediaInfo.Path];
        
        if (CurrentSessionState.state == State.NewVideo) {
            CurrentSessionState.FunscriptPath = Path.ChangeExtension(eventArgs.MediaInfo.Path, ".funscript");
        }
        _logger.LogWarning("Event: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);

        if (CurrentSessionState.state == State.NewVideo) {
            if (File.Exists(CurrentSessionState.FunscriptPath)) {
                CurrentSessionState.state = State.UploadingScript;
            _logger.LogWarning("Script Upload Starting: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, UploadURI);
                var content = new MultipartFormDataContent();

                FileStream fs = File.OpenRead(CurrentSessionState.FunscriptPath);

                var streamContent = new StreamContent(fs);
                content.Add(streamContent, "file", Path.GetFileName(CurrentSessionState.FunscriptPath));
                httpRequestMessage.Content = content;
                using var response = await _httpClientFactory
                    .CreateClient(NamedClient.Default)
                    .SendAsync(httpRequestMessage)
                    .ConfigureAwait(false);
                    
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                JObject responseJson = JObject.Parse(jsonString);

                if (responseJson["success"].Value<Boolean>()) {
                    CurrentSessionState.FunscriptURL = responseJson["url"].Value<string>();
                }

                CurrentSessionState.state = State.UploadedScript;
                _logger.LogWarning("Script Upload Done: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
                await UpdateServerTime(CurrentSessionState);
            }
        }
        if (CurrentSessionState.state == State.UploadedScript) {
            CurrentSessionState.state = State.SyncStarting;
            _logger.LogWarning("Sync Prepare Starting: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
            var url = GetUrlAPI() + "/syncPrepare";
            string query;
            using(var content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "url", CurrentSessionState.FunscriptURL},
                { "timeout", "30000" },
            })) {
                query = content.ReadAsStringAsync().Result;
            }
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url + "?" + query);

            using var response = await _httpClientFactory
                    .CreateClient(NamedClient.Default)
                    .SendAsync(httpRequestMessage)
                    .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            JObject responseJson = JObject.Parse(jsonString);
            if (responseJson["connected"].Value<bool>()) {
                CurrentSessionState.state = State.SyncStarted;
                _logger.LogWarning("Sync Prepare Done: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
            }
            else {
                CurrentSessionState.state = State.NewVideo;
                _logger.LogWarning("Sync Prepare Failed: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
            }

            
        }
        if (CurrentSessionState.state == State.SyncStarted || CurrentSessionState.state == State.Playing || CurrentSessionState.state == State.Paused) {
            if (change == PlaybackChange.PlaybackStart && (CurrentSessionState.state == State.Paused || CurrentSessionState.state == State.SyncStarted)) {
                var videoTime = TimeSpan.FromTicks((long) eventArgs.PlaybackPositionTicks);

                _logger.LogWarning("Playback Starting: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
                var url = GetUrlAPI() + "/syncPlay";
                string query;
                using(var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "play", "true"},
                    { "serverTime", ((long)DateTimeToJavaTimeStamp(GetServerTime(CurrentSessionState))).ToString()},
                    { "time", ((long)videoTime.TotalMilliseconds).ToString() },
                })) {
                    query = content.ReadAsStringAsync().Result;
                }
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url + "?" + query);

                using var response = await _httpClientFactory
                        .CreateClient(NamedClient.Default)
                        .SendAsync(httpRequestMessage)
                        .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(jsonString);
                CurrentSessionState.state = State.Playing;
                _logger.LogWarning("Playback Started: {@string}", query);
            }
            else if (change == PlaybackChange.PlaybackStop && (CurrentSessionState.state == State.Playing)) {
                _logger.LogWarning("Playback Stopping: {@string} {@Boolean} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), eventArgs.IsPaused, CurrentSessionState.state);
                var url = GetUrlAPI() + "/syncPlay";
                string query;
                using(var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "play", "false"},
                })) {
                    query = content.ReadAsStringAsync().Result;
                }
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url + "?" + query);

                using var response = await _httpClientFactory
                        .CreateClient(NamedClient.Default)
                        .SendAsync(httpRequestMessage)
                        .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                JObject responseJson = JObject.Parse(jsonString);
                CurrentSessionState.state = State.Paused;
                _logger.LogWarning("Playback Stopped: {@string} {@Boolean} {@string}", query, eventArgs.IsPaused, jsonString);
            }

        }
    }

    private DateTime GetServerTime(TheHandySessionState CurrentSessionState) {
        var serverTimeNow = DateTime.Now + CurrentSessionState.timeSyncAverageOffset + CurrentSessionState.timeSyncInitialOffset;
        return serverTimeNow;
    }

    private string GetUrlAPI() {
        return URL_BASE + URL_API_ENDPOINT + Configuration.ConnectionKey;
    }

    private static DateTime JavaTimeStampToDateTime( double javaTimeStamp )
    {
        // Java timestamp is milliseconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds( javaTimeStamp ).ToLocalTime();
        return dateTime;
    }

    private static double DateTimeToJavaTimeStamp( DateTime datetime )
    {
        // Java timestamp is milliseconds past epoch
        DateTime javadateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return (datetime.ToUniversalTime() - javadateTime).TotalMilliseconds;
    }

    private async Task UpdateServerTime(TheHandySessionState CurrentSessionState) {
        var sendTime = DateTime.Now;
        var url = GetUrlAPI() + "/getServerTime";

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);

        using var response = await _httpClientFactory
                    .CreateClient(NamedClient.Default)
                    .SendAsync(httpRequestMessage)
                    .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var jsonString = await response.Content.ReadAsStringAsync();

        JObject responseJson = JObject.Parse(jsonString);

        var now = DateTime.Now;

        var receiveTime = now;

        var rtd = receiveTime - sendTime;

        var serverTimeInUnix = responseJson["serverTime"].Value<double>();
        var serverTime = JavaTimeStampToDateTime(serverTimeInUnix);

        var estimatedServerTimeNow = serverTime + rtd / 2;

        _logger.LogWarning("Sync Timeoffset: rtd: {@TimeSpan} now: {@DateTime} serverTime: {@DateTime} estimatedServerTimeNow: {@DateTime}", rtd, now, serverTime, estimatedServerTimeNow);

        TimeSpan offset = TimeSpan.Zero;

        if (CurrentSessionState.timeSyncMessage == 0) {
            CurrentSessionState.timeSyncInitialOffset = estimatedServerTimeNow - now;
            _logger.LogWarning("Sync Timeoffset: timeSyncInitialOffset: {@TimeSpan}", CurrentSessionState.timeSyncInitialOffset);
        } else {
            offset = estimatedServerTimeNow - receiveTime - CurrentSessionState.timeSyncInitialOffset;
            CurrentSessionState.timeSyncAverageOffset = (CurrentSessionState.timeSyncAggregatedOffset + offset) / CurrentSessionState.timeSyncMessage;
            CurrentSessionState.timeSyncAggregatedOffset = CurrentSessionState.timeSyncAggregatedOffset + offset;

            _logger.LogWarning("Sync Timeoffset: {@string}  offset: {@TimeSpan} timeSyncAggregatedOffset: {@TimeSpan} timeSyncAverageOffset: {@TimeSpan} index: {@int} ", Path.GetFileName(CurrentSessionState.FunscriptPath), offset, CurrentSessionState.timeSyncAggregatedOffset, CurrentSessionState.timeSyncAverageOffset, CurrentSessionState.timeSyncMessage);
        }
        // DebugLogger("Time sync reply nr " + CurrentSessionState.timeSyncMessage + " (rtd, this offset, avrage offset): " + rtd + ' ' + offset + ' ' + timeSyncAverageOffset);
        //console.log("Time sync reply nr " + CurrentSessionState.timeSyncMessage + " (rtd, this offset, avrage offset):",rtd,offset,timeSyncAverageOffset);
        CurrentSessionState.timeSyncMessage++;
        if (CurrentSessionState.timeSyncMessage < 30) {
            await UpdateServerTime(CurrentSessionState);
        } else {
            _logger.LogWarning("Server Time Sync Done: {@string} {@State}", Path.GetFileName(CurrentSessionState.FunscriptPath), CurrentSessionState.state);
            //Time in sync
            //document.getElementById("state").innerHTML += "<li>Server time in sync. Avrage offset from client time: " + Math.round(timeSyncAverageOffset) + "ms</li>"; 
        }
    }


}
