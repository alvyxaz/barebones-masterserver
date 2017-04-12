using UnityEngine;
using UnityEngine.UI;

namespace Barebones.MasterServer
{
    /// <summary>
    ///     Represents a single item in the loading window
    /// </summary>
    public class LoadingUiItem : MonoBehaviour
    {
        public int Id;
        public Text Message;
    }
}