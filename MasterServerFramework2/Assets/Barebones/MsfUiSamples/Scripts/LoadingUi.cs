using System.Collections.Generic;
using Barebones.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Represents a loading window
    /// </summary>
    public class LoadingUi : MonoBehaviour
    {
        private EventsChannel _events;

        private GenericPool<LoadingUiItem> _pool;

        private Dictionary<int, LoadingUiItem> _visibleLoadingItems;
        public string DefaultLoadingMessage = "Loading...";


        public LayoutGroup ItemsGroup;
        public LoadingUiItem LoadingItemPrefab;

        public Image RotatingImage;

        protected bool HasSubscribedToEvents;

        private void Awake()
        {
            SubscribeToEvents();
        }

        public void SubscribeToEvents()
        {
            if (HasSubscribedToEvents)
                return;

            HasSubscribedToEvents = true;

            Msf.Events.Subscribe(Msf.EventNames.ShowLoading, OnLoadingEvent);
        }

        private void Update()
        {
            RotatingImage.transform.Rotate(Vector3.forward, Time.deltaTime*360*2);
        }

        private void OnEnable()
        {
            gameObject.transform.SetAsLastSibling();
        }

        private void OnLoadingEvent(object arg1, object arg2)
        {
            HandleEvent(arg1 as EventsChannel.Promise, arg2 as string);
        }

        protected virtual void HandleEvent(EventsChannel.Promise promise, string message)
        {
            if (_visibleLoadingItems == null) 
                _visibleLoadingItems = new Dictionary<int, LoadingUiItem>();

            if (_pool == null)
                _pool = new GenericPool<LoadingUiItem>(LoadingItemPrefab);

            // If this is the first item to get to the list
            if (_visibleLoadingItems.Count == 0)
                gameObject.SetActive(true);

            OnLoadingStarted(promise, message ?? DefaultLoadingMessage);
            promise.Subscribe(OnLoadingFinished);
        }

        protected virtual void OnLoadingStarted(EventsChannel.Promise promise, string message)
        {
            // Create an item
            var newItem = _pool.GetResource();
            newItem.Id = promise.EventId;
            newItem.Message.text = message;

            // Move item to the list
            newItem.transform.SetParent(ItemsGroup.transform, false);
            newItem.transform.SetAsLastSibling();
            newItem.gameObject.SetActive(true);

            // Store the item
            _visibleLoadingItems.Add(newItem.Id, newItem);
        }

        protected virtual void OnLoadingFinished(EventsChannel.Promise promise)
        {
            LoadingUiItem item;
            _visibleLoadingItems.TryGetValue(promise.EventId, out item);

            if (item == null)
                return;

            // Remove the item
            _visibleLoadingItems.Remove(promise.EventId);
            _pool.Store(item);

            // if everything is done loading, we can disable the loading view
            if (_visibleLoadingItems.Count == 0)
                gameObject.SetActive(false);
        }
    }
}