using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KeyZarNodeEditor
{
	[Serializable]
    public class Plug : IEquatable<Plug>
    {
        public enum PlugType { In, Out }

		[HideInInspector, NonSerialized]
        private Node node;

        public string name;
        public int plugIdx;
        public PlugType type;
        public Rect plugRect;
        public Rect labelRect;
        public Plug connectedTo;
        public string outPlugInfo;
        public string inPlugInfo;
        public bool connected;


        public static float plugSize = 8;
		public static float headerHeight = 30;
		public static float lineHeight = 12;

        public Plug(Node node, string name, int plugIdx, FieldInfo outPlugInfo)
        {
            this.node = node;
            this.name = name;
            this.plugIdx = plugIdx;

            this.outPlugInfo = "";
            outPlugInfo.FieldType.GetMethod("Invoke").GetParameters().ToList().ForEach(pInfo => this.outPlugInfo += $"{pInfo.ParameterType.Name}, ");
            type = PlugType.Out;
            UpdateRects(node.transform);
        }

        public Plug(Node node, string name, int plugIdx, MethodInfo inPlugInfo)
        {
            this.node = node;
            this.name = name;
            this.plugIdx = plugIdx;
            this.inPlugInfo = "";
            inPlugInfo.GetParameters().ToList().ForEach(pInfo => this.inPlugInfo += $"{pInfo.ParameterType.Name}, "); ;
            type = PlugType.In;
            UpdateRects(node.transform);
        }

        public bool Equals(Plug other)
        {
            return other.node == node && name.Equals(other.name);
        }

        public Vector2 PlugCenter()
        {
            return node.GetOffsetRect(plugRect).center;
        }

        public bool Contains(Vector2 position)
        {
            return node.GetOffsetRect(plugRect).Contains(position);
        }

        public void Draw(Rect nodeRect, GUIStyle style, Texture plugEmpty, Texture plugConnected)
        {
            UpdateRects(nodeRect);

            GUI.DrawTexture(node.GetOffsetRect(plugRect), connected ? plugConnected : plugEmpty);
            GUI.Label(
                node.GetOffsetRect(labelRect),
                name,
                new GUIStyle
                {
                    alignment = type == PlugType.In ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                    fontSize = style.fontSize,
                    normal = new GUIStyleState { textColor = style.normal.textColor }
                }
            );
        }

        public void UpdateRects(Rect nodeRect)
        {

            plugRect = new Rect
            {
                position = new Vector2(
                    (type == PlugType.In ? nodeRect.x : nodeRect.xMax) - plugSize / 2,
                    nodeRect.y + headerHeight + lineHeight * plugIdx - plugSize / 2
                ),
                height = plugSize,
                width = plugSize
            };

            labelRect = new Rect
            {
                position = new Vector2(
                    type == PlugType.In ? plugRect.x + 10 : plugRect.x - 100 - plugSize / 2,
                    plugRect.y - plugSize / 2
                ),
                height = 10,
                width = 100
            };
        }
    }
}
