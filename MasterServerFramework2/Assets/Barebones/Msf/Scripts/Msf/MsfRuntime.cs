namespace Barebones.MasterServer
{
    public class MsfRuntime
    {
        public bool IsEditor { get; private set; }

        public bool SupportsThreads { get; private set; }

        public MsfRuntime()
        {
#if UNITY_EDITOR
            IsEditor = true;
#else
            IsEditor = false;
#endif
            SupportsThreads = true;
#if !UNITY_EDITOR && (UNITY_WEBGL || !UNITY_WEBPLAYER)
            SupportsThreads = false;
#endif
        }
    }
}