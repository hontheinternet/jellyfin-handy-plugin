namespace Jellyfin.TheHandy;

/// <summary>
/// The configuration options.
/// </summary>
public enum State
{
    /// <summary>
    /// Option one.
    /// </summary>
    NewVideo,

    /// <summary>
    /// Option one.
    /// </summary>
    UploadingScript,

    /// <summary>
    /// Option one.
    /// </summary>
    UploadedScript,

    /// <summary>
    /// Option one.
    /// </summary>
    UpdatingServerTimeSync,

    /// <summary>
    /// Option one.
    /// </summary>
    UpdateServerTimeSyncDone,

    /// <summary>
    /// Second option.
    /// </summary>
    SyncStarting,

    /// <summary>
    /// Second option.
    /// </summary>
    SyncStarted,

    /// <summary>
    /// Second option.
    /// </summary>
    Playing,
    
    /// <summary>
    /// Second option.
    /// </summary>
    Paused,
}


public class TheHandySessionState {
    public int timeSyncMessage = 0;
    public TimeSpan timeSyncAggregatedOffset;
    public TimeSpan timeSyncAverageOffset;
    public TimeSpan timeSyncInitialOffset;

    public State state = State.NewVideo;
    public string FunscriptPath = "";
    public string FunscriptURL = "";
}


/// <summary>
/// The configuration options.
/// </summary>
public enum PlaybackChange
{
    /// <summary>
    /// Option one.
    /// </summary>
    PlaybackStart,
    /// <summary>
    /// Option one.
    /// </summary>
    PlaybackStop,
}
