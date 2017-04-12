using UnityEngine;

namespace Barebones.MasterServer
{
    public class MsfUiDestroyer : MonoBehaviour
    {
        public HelpBox _header = new HelpBox()
        {
            Text = "If you run the application with '-msfDestroyUi' arg, " +
                   "every game object, which has this component, will be destroyed"
        };

        void Awake()
        {
            if (Msf.Args.DestroyUi)
            {
                Destroy(gameObject);
            }
        }
    }
}