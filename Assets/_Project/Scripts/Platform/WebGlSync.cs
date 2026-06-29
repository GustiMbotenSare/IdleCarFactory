namespace CarFactoryIdle.Platform
{
    /// <summary>Placeholder for flushing the WebGL IndexedDB filesystem so PlayerPrefs survive a
    /// page refresh or a tab close. SaveSystem calls FlushFileSystem after every save, but only
    /// inside a UNITY_WEBGL build, so on other platforms this is never hit.
    ///
    /// FOR THE UNITY DEVELOPER: this is a stub (empty body). On WebGL, PlayerPrefs writes are not
    /// guaranteed to reach IndexedDB right away. Replace the body with a small JavaScript interop
    /// call (a .jslib plugin that runs FS.syncfs(false, ...)) so progress is not lost when the
    /// player closes the tab.</summary>
    public static class WebGlSync
    {
        public static void FlushFileSystem() { }
    }
}
