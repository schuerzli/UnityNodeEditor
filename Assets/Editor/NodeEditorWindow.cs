using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace KeyZarNodeEditor
{
    public class NodeEditorWindow : EditorWindow
    {
        //private static NodeEditorWindow window;
        private NodeEditorConfig nodeConfig;
        private Vector2 cachedMousePos;
        private GUIStyle nodeStyle;

        private Texture2D plugUnconnected;
        private Texture2D plugConnected;
        private Texture2D windowBG;
        private Texture2D nodeBG;
        private Texture2D connectionTex;
        //private List<Node> selectedNodes = new List<Node>();
        private Color connectionColor = new Color(0.65f, 0.97f, 0.65f);

        public static Vector2 canvasOffset = Vector2.zero;

        public static Rect GetOffsetRect(Rect rect)
        {
            rect.position += canvasOffset;
            return rect;
        }

        [MenuItem("Window/NodeEditor")]
        public static void ShowWindow()
        {
			GetWindow<NodeEditorWindow>();
        }

        private void OnEnable()
        {
            windowBG = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            windowBG.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.22f));
            windowBG.Apply();

            nodeBG = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            nodeBG.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.36f, 0.7f));
            nodeBG.Apply();

            connectionTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            connectionTex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.7f));
            connectionTex.Apply();

            //		nodeStyle = EditorStyles.helpBox;
            nodeStyle = new GUIStyle();
            nodeStyle.alignment = TextAnchor.UpperCenter;
            nodeStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            nodeStyle.normal.background = nodeBG;
            //		nodeStyle.normal.background = EditorGUIUtility.FindTexture ("Shader Icon");

            plugUnconnected = Resources.Load("plug_dot_unconnected") as Texture2D;
            plugConnected = Resources.Load("plug_dot_connected") as Texture2D;

            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
			Undo.undoRedoPerformed += OnUndoRedo;
        }

		private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
			Undo.undoRedoPerformed -= OnUndoRedo;
			AssetDatabase.SaveAssets();
        }

        private void OnAfterAssemblyReload()
        {
            if (nodeConfig != null) nodeConfig.UpdateNodeTypeReferences();
		}

        private void OnUndoRedo()
        {
            Repaint();
        }

        void OnGUI()
        {
            GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), windowBG, ScaleMode.StretchToFill);
            DrawGrid(20, new Color(1, 1, 1, 0.2f));
            DrawGrid(100, new Color(1, 1, 1, 0.4f));

            Event e = Event.current;
            cachedMousePos = e.mousePosition;

            if (nodeConfig != null)
            {
				if (nodeConfig.nodes != null) nodeConfig.ProcessNodeEvents(Event.current);
                ProcessEvents(e);

				nodeConfig.DrawNodes(canvasOffset, nodeStyle, plugUnconnected, plugConnected);
                DrawConnections();
				nodeConfig.MouseOverPlug(cachedMousePos);
            }

            // On top of everything else (I love u philipp)
            nodeConfig = EditorGUILayout.ObjectField(nodeConfig, typeof(NodeEditorConfig), false) as NodeEditorConfig;

            EditorGUILayout.LabelField(
                $"{cachedMousePos.ToString()}",
                EditorStyles.whiteLabel
            );

            if (GUI.changed)
            {
                Repaint();
                OnEnable();
            }
        }

        private void DrawConnections()
        {
            if (nodeConfig != null)
            {
                foreach (PlugConnection connection in nodeConfig.connections)
                {
                    connection.UpdateVertices(
                        nodeConfig.nodes[connection.outNode].OutPlugs[connection.outPlug],
                        nodeConfig.nodes[connection.inNode].InPlugs[connection.inPlug]
                    );
                    DrawConnection(connection.outPos, connection.inPos);
                }
                if (nodeConfig.SelectedPlug != null)
                {
                    DrawConnection(nodeConfig.SelectedPlug.PlugCenter(), cachedMousePos);
                    GUI.changed = true;
                }
            }
        }

        private void DrawConnection(Vector2 p1, Vector2 p2)
        {
            float dist = (p1 - p2).magnitude;
            Vector2 tan1 = p1 + Vector2.right * dist / 2f;
            Vector2 tan2 = p2 - Vector2.right * dist / 2f;

            Handles.DrawBezier(
                p1,
                p2,
                tan1,
                tan2,
                connectionColor,
                connectionTex,
                1            
            );
        }

        bool clickedOnCanvas = false;
        bool isDragging = false;
        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    // check if y position is below tile bar
                    if (e.button == 1 && e.mousePosition.y > 0) clickedOnCanvas = true;
                    else if (e.button == 0) nodeConfig.ClearPlugSelection();
                    break;
                case EventType.MouseUp:
                    if (e.button == 1 && clickedOnCanvas && !isDragging) ShowContextMenu(e.mousePosition);
                    isDragging = false;
                    clickedOnCanvas = false;
                    break;
                case EventType.MouseDrag:
                    if (e.button == 1 && clickedOnCanvas)
                    {
                        isDragging = true;
                        canvasOffset += Event.current.delta;
                        GUI.changed = true;
                    }
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.F)
                    {
                        canvasOffset = Vector2.zero;
                        GUI.changed = true;
                    }
                    break;
            }
        }

        Vector2 selectionStartPos;
        Vector2 selectionEndPos;
        void StartSelection() { }
        void EndSelection() { }

        void ShowContextMenu(Vector2 mousePosition)
        {
            Node clickedOn = null;
            foreach (Node node in nodeConfig.nodes)
            {
                if (node.Contains(mousePosition))
                {
                    clickedOn = node;
                    break;
                }
            }

            if (clickedOn == null)
            {
				GenericMenu popup = new GenericMenu();
                popup.AddItem(new GUIContent("MinimalPlugTest"), false, AddNodeType1);
                popup.AddItem(new GUIContent("ComprehensivePlugTest"), false, AddNodeType2);
                popup.ShowAsContext();
            }
        }

        private void AddNodeType1()
        {
            nodeConfig.AddNode(cachedMousePos - canvasOffset, typeof(MinimalPlugTest));
        }
		
		private void AddNodeType2()
		{
			nodeConfig.AddNode(cachedMousePos - canvasOffset, typeof(ComprehensivePlugTest));
		}

        private void DrawGrid(float gridSpacing, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = gridColor;

            Vector3 modOffset = new Vector3(canvasOffset.x % gridSpacing, canvasOffset.y % gridSpacing, 0);

            for (int i = 0; i <= widthDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(gridSpacing * i + modOffset.x, 0, 0), 
                    new Vector3(gridSpacing * i + modOffset.x, position.height, 0f));
            }

            for (int j = 0; j <= heightDivs; j++)
            {
                Handles.DrawLine(
                    new Vector3(0, gridSpacing * j + modOffset.y, 0), 
                    new Vector3(position.width, gridSpacing * j + modOffset.y, 0f));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }
    }
}