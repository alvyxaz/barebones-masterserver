namespace Barebones.MasterServer
{
    public enum SpawnStatus
    {
        Killed = -3,
        Aborted = -2,
        Aborting = -1,

        None,
        InQueue,
        StartingProcess,
        WaitingForProcess,
        ProcessRegistered,
        Finalized
    }
}