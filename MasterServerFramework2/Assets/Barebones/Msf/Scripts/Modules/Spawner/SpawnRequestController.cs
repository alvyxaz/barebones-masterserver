using System;
using System.Collections.Generic;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class SpawnRequestController
    {
        private readonly IClientSocket _connection;
        public int SpawnId { get; set; }

        public event Action<SpawnStatus> StatusChanged;

        public SpawnStatus Status { get; private set; }

        /// <summary>
        /// A dictionary of options that user provided when requesting a 
        /// process to be spawned
        /// </summary>
        public Dictionary<string, string> SpawnOptions;

        public SpawnRequestController(int spawnId, IClientSocket connection, Dictionary<string, string> spawnOptions)
        {
            _connection = connection;
            SpawnId = spawnId;
            SpawnOptions = spawnOptions;

            // Set handlers
            connection.SetHandler((short) MsfOpCodes.SpawnRequestStatusChange, HandleStatusUpdate);
        }

        public void Abort()
        {
            Msf.Client.Spawners.AbortSpawn(SpawnId);
        }

        public void Abort(MsfSpawnersClient.AbortSpawnHandler handler)
        {
            Msf.Client.Spawners.AbortSpawn(SpawnId, handler);
        }

        [Obsolete("Use GetFinalizationData")]
        public void GetCompletionData(MsfSpawnersClient.FinalizationDataHandler handler)
        {
            Msf.Client.Spawners.GetFinalizationData(SpawnId, handler, _connection);
        }

        public void GetFinalizationData(MsfSpawnersClient.FinalizationDataHandler handler)
        {
            Msf.Client.Spawners.GetFinalizationData(SpawnId, handler, _connection);
        }

        private static void HandleStatusUpdate(IIncommingMessage message)
        {
            var data = message.Deserialize(new SpawnStatusUpdatePacket());

            var controller = Msf.Client.Spawners.GetRequestController(data.SpawnId);

            if (controller == null)
                return;

            controller.Status = data.Status;

            if (controller.StatusChanged != null)
                controller.StatusChanged.Invoke(data.Status);
        }
    }
}