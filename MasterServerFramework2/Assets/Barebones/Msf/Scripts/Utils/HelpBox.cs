using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barebones.MasterServer
{
    [Serializable]
    public class HelpBox
    {
        [NonSerialized] public string Text;
        [NonSerialized] public float Height;
        [NonSerialized] public HelpBoxType Type;

        private string _constructorValue;

        public HelpBox(string text, float height, HelpBoxType type = HelpBoxType.Info)
        {
            Text = text;
            Height = height;
            Type = type;
        }

        public HelpBox(string text, HelpBoxType type = HelpBoxType.Info)
        {
            Text = text;
            Height = 40;
            Type = type;
        }

        public HelpBox()
        {
            Height = 40;
            Text = "";
            Type = HelpBoxType.Info;
        }
    }
}


