using System;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.Networking
{
    /// <summary>
    ///     This is an object which gets spawned into game once.
    ///     It's main purpose is to call update methods
    /// </summary>
    public class BmUpdateRunner : MonoBehaviour
    {
        private static BmUpdateRunner _instance;

        private List<IUpdatable> _addList;
        private List<IUpdatable> _removeList;

        private List<IUpdatable> _runnables;

        public event Action ApplicationQuit; 

        public static BmUpdateRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject();
                    obj.name = "Update Runner";
                    _instance = obj.AddComponent<BmUpdateRunner>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            _runnables = new List<IUpdatable>();
            _addList = new List<IUpdatable>();
            _removeList = new List<IUpdatable>();
        }

        private void Update()
        {
            if (_addList.Count > 0)
            {
                _runnables.AddRange(_addList);
                _addList.Clear();
            }

            foreach (var runnable in _runnables)
                runnable.Update();

            if (_removeList.Count > 0)
            {
                _runnables.AddRange(_removeList);
                _removeList.Clear();
            }
        }

        public void Add(IUpdatable updatable)
        {
            if (_addList.Contains(updatable))
                return;

            _addList.Add(updatable);
        }

        public void Remove(IUpdatable updatable)
        {
            _removeList.Add(updatable);
        }

        void OnApplicationQuit()
        {
            if (ApplicationQuit != null)
                ApplicationQuit.Invoke();
        }
    }

    public interface IUpdatable
    {
        void Update();
    }
}