using System;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class MsfConcurrency
    {
        public void RunOnThreadPool(Action action)
        {
            action.Invoke();
        }

        public void RunOnMainThread(Action action)
        {
            BTimer.ExecuteOnMainThread(action);
        }
    }
}