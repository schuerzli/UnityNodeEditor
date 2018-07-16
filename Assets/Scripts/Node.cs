using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace KeyZarNodeEditor
{
	[Serializable]
	public class Node : ISerializationCallbackReceiver
	{
		public string displayName = "NewNode";
		public Rect transform;
		private List<Plug> inPlugs = new List<Plug>();
		public List<Plug> InPlugs => inPlugs;
		private List<Plug> outPlugs = new List<Plug>();
		public List<Plug> OutPlugs => outPlugs;

		public string targetTypeName;
		public string targetAssemblyQualifiedTypeName;

		private bool isDragged;
		public bool IsDragged => IsDragged;

		public string serializedTarget;
		public Type TargetType { get { return Type.GetType(targetAssemblyQualifiedTypeName); } }
		private object target;

		public object GetTarget()
		{
			if (target == null && !string.IsNullOrEmpty(targetAssemblyQualifiedTypeName))
				target = Activator.CreateInstance(Type.GetType(targetAssemblyQualifiedTypeName));
			return target;
		}

		public Node(Rect transform, Type type)
		{
			this.transform = transform;
			SetType(type);
		}

		public void SetType(string typeName)
		{
			SetType(Type.GetType(typeName));
		}

		public void SetType(Type newNodeType)
		{
			targetAssemblyQualifiedTypeName = newNodeType.AssemblyQualifiedName;
			targetTypeName = newNodeType.Name;
			displayName = newNodeType.Name;

			var inPlugInfo =
				(from m in newNodeType.GetMethods()
				 where m.GetCustomAttributes(typeof(InPlug), false).Length > 0
				 select m).ToArray();

			inPlugs = new List<Plug>();
			int i = 0;
			foreach (MethodInfo info in inPlugInfo)
			{
				inPlugs.Add(new Plug(this, info.Name, i, info));
				++i;
			}

			var outPlugInfo =
				(from m in newNodeType.GetFields()
				 where m.GetCustomAttributes(typeof(OutPlug), false).Length > 0
				 select m).ToList();

			outPlugs = new List<Plug>();
			i = 0;
			foreach (FieldInfo info in outPlugInfo)
			{
				outPlugs.Add(new Plug(this, info.Name, i, info));
				++i;
			}
		}

		public void Draw(GUIStyle nodeStyle, Texture2D plugUnconnected, Texture2D plugConnected)//, SerializedProperty property = null)
		{
			GUI.Box(GetOffsetRect(transform), "", nodeStyle);
			Rect headerRect = GetOffsetRect(transform);
			headerRect.height = 20;
			GUI.Box(
				headerRect,
				displayName,
				new GUIStyle
				{
					alignment = TextAnchor.MiddleCenter,
					normal = new GUIStyleState
					{
						background = nodeStyle.normal.background,
						textColor = nodeStyle.normal.textColor
					}
				}
			);

			inPlugs.ForEach(p => p.Draw(transform, nodeStyle, plugUnconnected, plugConnected));
			outPlugs.ForEach(p => p.Draw(transform, nodeStyle, plugUnconnected, plugConnected));

			//DrawEditableFields(headerRect);
			//SerializedProperty prop = new SerializedProperty(new UnityEngine.Object());
			//SerializedProperty prop = property.FindPropertyRelative("target");
			//if (prop == null){
				//property.arraySize += 1;
				//prop = property.GetArrayElementAtIndex(property.arraySize - 1);
				//typeof(SerializedProperty).GetField("type").SetValue(prop, SerializedPropertyType.Generic);
				//prop.type = SerializedPropertyType.Generic;
			//}

			//if (property != null) DrawPropertyDrawer(headerRect, , GUIContent.none);
		}

		private Rect GetConfigVarRect(Rect headerRect){
            int numPlugs = inPlugs.Count > outPlugs.Count ? inPlugs.Count : outPlugs.Count;
            Rect configVarRect = GetOffsetRect(transform);
			configVarRect.height = TargetType.GetFields().Length * EditorGUIUtility.singleLineHeight;

            Vector2 pos = configVarRect.position;
            pos.y += headerRect.height + Plug.lineHeight * numPlugs + 5;
            configVarRect.position = pos;

			return configVarRect;
		}

		private void DrawPropertyDrawer(Rect position, SerializedProperty property, GUIContent label)
		{
			position = GetConfigVarRect(position);
			if (propertyDrawer != null && property != null) propertyDrawer?.OnGUI(position, property, label);
		}

		private void DrawEditableFields(Rect headerRect)
		{
			if (target == null) target = GetTarget();
			if (target == null) return;

			Rect configVarRect = GetConfigVarRect(headerRect);
			configVarRect.height = EditorGUIUtility.singleLineHeight;
            
			foreach (FieldInfo fieldInfo in TargetType.GetFields())
			{
				if (fieldInfo.FieldType == typeof(float))
				{
					EditorGUIUtility.labelWidth = configVarRect.width / 2;
					fieldInfo.SetValue(
						target,
						EditorGUI.FloatField(
							configVarRect,
							new GUIContent(fieldInfo.Name),
							(float)fieldInfo.GetValue(target)
						)
					);
					configVarRect.y += EditorGUIUtility.singleLineHeight;
				}
				else if (fieldInfo.FieldType == typeof(int))
				{
					EditorGUIUtility.labelWidth = configVarRect.width / 2;
					fieldInfo.SetValue(
						target,
						EditorGUI.IntField(
							configVarRect,
							new GUIContent(fieldInfo.Name),
							(int)fieldInfo.GetValue(target)
						)
					);
					configVarRect.y += EditorGUIUtility.singleLineHeight;
				}
				else if (fieldInfo.FieldType == typeof(string))
				{
					EditorGUIUtility.labelWidth = configVarRect.width / 2;
					fieldInfo.SetValue(
						target,
						EditorGUI.TextField(
							configVarRect,
							new GUIContent(fieldInfo.Name),
							(string)fieldInfo.GetValue(target)
						)
					);
					configVarRect.y += EditorGUIUtility.singleLineHeight;
				}

			}
		}

		public bool Contains(Vector2 position)
		{
			return GetOffsetRect(transform).Contains(position);
		}

		public Vector2 posOffset;
		public Rect GetOffsetRect(Rect rect)
		{
			rect.position += posOffset;
			return rect;
		}

		public struct EventResult
		{
			public bool nodeClicked;
			public bool plugClicked;
			public Plug clickedPlug;
		}

		public bool ProcessEvents(
			Event e,
			Action<Node> deleted,
			Action<Node, Plug> clickedOnPlug)
		{
			if (e.isMouse)
			{
				switch (e.type)
				{
					case EventType.MouseDown:
						foreach (Plug plug in inPlugs)
						{
							if (plug.Contains(e.mousePosition))
							{
								clickedOnPlug(this, plug);
								e.Use();
								return true;
							}
						}
						foreach (Plug plug in outPlugs)
						{
							if (plug.Contains(e.mousePosition))
							{
								clickedOnPlug(this, plug);
								e.Use();
								return true;
							}
						}
						break;
					case EventType.MouseDrag:
						if (e.button == 0 && isDragged)
						{
							Drag(e.delta);
							e.Use();
						}
						break;
				}

				if (Contains(e.mousePosition))
				{
					switch (e.type)
					{
						case EventType.MouseDown:
							if (e.button == 0)
							{
								if (GetOffsetRect(transform).Contains(e.mousePosition))
								{
									isDragged = true;
									GUI.changed = true;
								}
								else GUI.changed = true;
							}
							else if (e.button == 1)
							{
								GenericMenu popup = new GenericMenu();
								popup.AddItem(new GUIContent("Delete"), false, () => deleted(this));
								popup.ShowAsContext();
							}

							break;

						case EventType.MouseUp:
							isDragged = false;
							break;
					}
					return true;
				}
			}

			return false;
		}

		public bool ShowPlugHint(Vector2 mousePosition)
		{
			foreach (Plug plug in inPlugs)
			{
				if (plug.Contains(mousePosition))
				{
					GUIContent content = new GUIContent(plug.inPlugInfo);
					Rect pos = new Rect(mousePosition - Vector2.up * 20, GUI.skin.label.CalcSize(content));
					GUIStyle style = new GUIStyle(EditorStyles.textField);
					style.normal.textColor = new Color(0.15f, 0.15f, 0.15f);
					GUI.Label(
						pos,
						content,
						style);
					return true;
				}
			}
			foreach (Plug plug in outPlugs)
			{
				if (plug.Contains(mousePosition))
				{
					GUIContent content = new GUIContent(plug.outPlugInfo);
					Rect pos = new Rect(mousePosition - Vector2.up * 20, GUI.skin.label.CalcSize(content));
					GUIStyle style = new GUIStyle(EditorStyles.textField);
					style.normal.textColor = new Color(0.15f, 0.15f, 0.15f);
					GUI.Label(
						pos,
						content,
						style);
					return true;
				}
			}
			return false;
		}

		public void Drag(Vector2 delta)
		{
			transform.position += delta;
			inPlugs.ForEach(p => p.UpdateRects(transform));
			outPlugs.ForEach(p => p.UpdateRects(transform));
			GUI.changed = true;
		}

		public void OnBeforeSerialize()
		{
			serializedTarget = JsonUtility.ToJson(target);
		}

		private PropertyDrawer propertyDrawer;
		public void OnAfterDeserialize()
		{
			target = JsonUtility.FromJson(serializedTarget, Type.GetType(targetAssemblyQualifiedTypeName));
			Type t = Type.GetType(targetAssemblyQualifiedTypeName);
			if (t != null) SetType(t);

			propertyDrawer = GetPropertyDrawerFor(typeof(Node));
			if (propertyDrawer != null) Debug.Log(propertyDrawer.ToString());
		}

		public static PropertyDrawer GetPropertyDrawerFor(Type objType)
		{
			Assembly[] AS = AppDomain.CurrentDomain.GetAssemblies();
			FieldInfo propDrawTypeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
			//var field = customPropDrawType.GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
			//Debug.Log(propDrawTypeField.Name);

			foreach (Assembly assem in AS)
			{
				foreach (Type tp in assem.GetTypes())
				{
					foreach (CustomPropertyDrawer attr in tp.GetCustomAttributes(typeof(CustomPropertyDrawer)))
					{
						Type propDrawType = (Type)propDrawTypeField.GetValue(attr);
						if (propDrawType == objType) return (PropertyDrawer)Activator.CreateInstance(tp);
					}
				}
			}

			return null;
		}
	}
}


