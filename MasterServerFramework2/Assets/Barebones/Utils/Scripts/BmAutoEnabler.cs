using System.Collections.Generic;
using UnityEngine;

namespace Barebones.Utils
{
    /// <summary>
    ///     A script that enables all of the objects within the list.
    ///     Useful for enabling objects that you don't want enabled in the editor
    /// </summary>
    public class BmAutoEnabler : MonoBehaviour
    {
        public List<GameObject> Objects;

        // Use this for initialization
        private void Awake()
        {
            foreach (var obj in Objects)
                if (obj != null)
                    obj.SetActive(true);
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }
}