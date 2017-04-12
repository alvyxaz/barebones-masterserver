using System;
using System.Collections;
using System.Collections.Generic;
using Barebones.MasterServer;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HelpBox))]
public class HelpBoxDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var info = fieldInfo.GetValue(property.serializedObject.targetObject) as HelpBox;

        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.HelpBox(position, info.Text, (MessageType)info.Type);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var info = fieldInfo.GetValue(property.serializedObject.targetObject) as HelpBox;
        return info.Height;
    }
}