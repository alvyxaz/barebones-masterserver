using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Displays progress of game creation
    /// </summary>
    public class CreateGameProgressUi : MonoBehaviour
    {
        public Button AbortButton;

        public float EnableAbortAfterSeconds = 10;
        public float ForceCloseAfterAbortRequestTimeout = 10;

        public string PleaseWaitText = "Please wait...";

        protected SpawnRequestController Request;
        public Image RotatingImage;

        public Text StatusText;

        public bool SetAsLastSiblingOnEnable = true;

        // Use this for initialization
        private void Start()
        {
        }

        private void Update()
        {
            RotatingImage.transform.Rotate(Vector3.forward, Time.deltaTime*360*2);

            if (Request == null)
                return;

            if (StatusText != null)
                StatusText.text = string.Format("Progress: {0}/{1} ({2})", 
                    (int)Request.Status, 
                    (int)SpawnStatus.Finalized,
                    Request.Status);
        }

        public void OnEnable()
        {
            if (SetAsLastSiblingOnEnable)
                transform.SetAsLastSibling();
        }

        public void OnAbortClick()
        {
            if (Request == null)
            {
                // If there's no  request to abort, just hide the window
                gameObject.SetActive(false);
                return;
            }

            // Start a timer which will close the window
            // after timeout, in case abortion fails
            StartCoroutine(CloseAfterRequest(ForceCloseAfterAbortRequestTimeout, Request.SpawnId));

            // Disable abort button
            AbortButton.interactable = false;

            Request.Abort((isHandled, error) =>
            {
                // If request is not handled, enable the button abort button
                AbortButton.interactable = !isHandled;
            });
        }

        public IEnumerator EnableAbortDelayed(float seconds, int spawnId)
        {
            yield return new WaitForSeconds(seconds);

            if ((Request != null) && (Request.SpawnId == spawnId))
                AbortButton.interactable = true;
        }

        public IEnumerator CloseAfterRequest(float seconds, int spawnId)
        {
            yield return new WaitForSeconds(seconds);

            if ((Request != null) && (Request.SpawnId == spawnId))
            {
                gameObject.SetActive(false);

                // Send another abort request just in case
                // (maybe something unstuck?)
                Request.Abort();
            }
        }

        protected void OnStatusChange(SpawnStatus status)
        {
            if (status < SpawnStatus.None)
            {
                // If game was aborted
                Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                    DialogBoxData.CreateInfo("Game creation aborted"));

                Logs.Error("Game creation aborted");

                // Hide the window
                gameObject.SetActive(false);
            }

            if (status == SpawnStatus.Finalized)
            {
                Request.GetFinalizationData((data, error) =>
                {
                    if (data == null)
                    {
                        Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                            DialogBoxData.CreateInfo("Failed to retrieve completion data: " + error));

                        Logs.Error("Failed to retrieve completion data: " + error);

                        Request.Abort();
                        return;
                    }

                    // Completion data received
                    OnFinalizationDataRetrieved(data);
                });
            }
        }

        public void OnFinalizationDataRetrieved(Dictionary<string, string> data)
        {
            if (!data.ContainsKey(MsfDictKeys.RoomId))
            {
                throw new Exception("Game server finalized, but didn't include room id");
            }

            var roomId = int.Parse(data[MsfDictKeys.RoomId]);

            var password = data.ContainsKey(MsfDictKeys.RoomPassword)
                ? data[MsfDictKeys.RoomPassword]
                : "";

            Msf.Client.Rooms.GetAccess(roomId, password, (access, error) =>
            {
                if (access == null)
                {
                    Msf.Events.Fire(Msf.EventNames.ShowDialogBox,
                            DialogBoxData.CreateInfo("Failed to get access to room: " + error));

                    Logs.Error("Failed to get access to room: " + error);

                    return;
                }

                OnRoomAccessReceived(access);
            });
        }

        public void OnRoomAccessReceived(RoomAccessPacket access)
        {
            // We're hoping that something will handle the Msf.Client.Rooms.AccessReceived event
            // (for example, SimpleAccessHandler)
        }

        public void Display(SpawnRequestController request)
        {
            if (Request != null)
                Request.StatusChanged -= OnStatusChange;

            if (request == null)
                return;

            request.StatusChanged += OnStatusChange;

            Request = request;
            gameObject.SetActive(true);

            // Disable abort, and enable it after some time
            AbortButton.interactable = false;
            StartCoroutine(EnableAbortDelayed(EnableAbortAfterSeconds, request.SpawnId));

            if (StatusText != null)
                StatusText.text = PleaseWaitText;
        }

        private void OnDestroy()
        {
            if (Request != null)
                Request.StatusChanged -= OnStatusChange;
        }
    }
}