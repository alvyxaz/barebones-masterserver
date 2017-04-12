using UnityEngine;
using UnityEngine.EventSystems;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Adding this script to UI elements makes them draggable
    /// </summary>
    public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private CanvasGroup _group;
        private float _offsetY;
        private float _offsetX;

        public bool ChangeOpacity = true;

        public float DraggedOpacity = 0.7f;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _offsetX = transform.position.x - Input.mousePosition.x;
            _offsetY = transform.position.y - Input.mousePosition.y;

            if (_group != null)
                _group.alpha = DraggedOpacity;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = new Vector3(_offsetX + Input.mousePosition.x, _offsetY + Input.mousePosition.y);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_group != null)
                _group.alpha = 1f;
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }
    }
}