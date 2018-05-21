using System;
using UnityEditor;
using UnityEngine;

namespace KeyZarNodeEditor
{
	[CustomPropertyDrawer(typeof(Node))]
	public class NodePropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(position, property.FindPropertyRelative("displayName"));
			EditorGUI.EndProperty();
        }
	}
}
