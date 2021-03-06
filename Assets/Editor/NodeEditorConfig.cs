﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KeyZarNodeEditor
{
	[CreateAssetMenu(menuName = "NodeEditorConfig", fileName = "NodeEditorConfig")]
	public class NodeEditorConfig : ScriptableObject
	{
		public List<Node> nodes = new List<Node>();
		public List<PlugConnection> connections = new List<PlugConnection>();

		private Node outNode = null;
		public Node SelectedNode => outNode;
		private Plug outPlug = null;
		public Plug SelectedPlug => outPlug;

		private void OnEnable()
		{
			outNode = null;
			outPlug = null;
			hideFlags = HideFlags.None;
		}

		public void DrawNodes(Vector2 canvasOffset, GUIStyle nodeStyle, Texture2D plugUnconnected, Texture2D plugConnected)
		{
			int i = 0;
			foreach (Node node in nodes) {
				node.posOffset = canvasOffset;
				node.Draw(nodeStyle, plugUnconnected, plugConnected);
				++i;
			}
		}

		public Node GetDraggedNode()
		{
			foreach (Node node in nodes)
				if (node.IsDragged) return node;
			return null;
		}

		public void MouseOverPlug(Vector2 mousePosition)
		{
			bool showedHint = false;
			nodes.ForEach(node => { if (node.ShowPlugHint(mousePosition)) showedHint = true; });
			if (showedHint) GUI.changed = true;
		}

		public void AddNode(Vector2 position, Type type)
		{
			Undo.RecordObject(this, "add node");
			Node newNode = new Node(
				 new Rect
				 {
					 position = position,
					 width = 150,
					 height = 100
				 },
				 type);
			nodes.Add(newNode);
		}

		public void UpdateNodeTypeReferences()
		{
			//nodes.ForEach(n => n.UpdateTypeReference());
		}

		public void ProcessNodeEvents(Event e)
		{
			for (int i = nodes.Count - 1; i >= 0; i--)
				nodes[i].ProcessEvents(e, DeleteNode, OnPlugClicked);
		}

		private void OnPlugClicked(Node node, Plug plug)
		{
			if (plug.type == Plug.PlugType.In && outPlug != null)
			{
				if (node == outNode)
				{
					Debug.Log("Cannot connect plugs of the same node");
					ClearPlugSelection();
					return;
				}
				Undo.RecordObject(this, "add connection");
				PlugConnection newConnection = new PlugConnection(
						nodes.IndexOf(outNode),
						outNode.OutPlugs.IndexOf(outPlug),
						nodes.IndexOf(node),
						node.InPlugs.IndexOf(plug)
				);

				if (connections.Contains(newConnection)) Debug.Log("Connection already exists");
				else
				{
					connections.Add(newConnection);
					outPlug.connected = true;
					plug.connected = true;
				}

				ClearPlugSelection();
			}
			else if (plug.type == Plug.PlugType.Out)
			{
				outNode = node;
				outPlug = plug;
			}
		}

		public void ClearPlugSelection()
		{
			outNode = null;
			outPlug = null;
		}

		public void DeleteNode(Node node)
		{
			Undo.RegisterCompleteObjectUndo(this, "delete node");
			int NodeIdx = nodes.IndexOf(node);

			nodes.Remove(node);
			for (int i = connections.Count - 1; i >= 0; --i) {
				PlugConnection con = connections[i];
				if (con.inNode == NodeIdx || con.outNode == NodeIdx) {
                    // these lead to idx out of bouds exception here
					//if (con.inNode == NodeIdx) { 
					//	GetOutPlug(con).connected = false; 
					//}
					//if (con.outNode == NodeIdx) { 
					//	GetInPlug(con).connected = false; 
					//}
					connections.RemoveAt(i);
				}
				if (con.inNode > NodeIdx) {
					con.inNode -= 1;
				}
				if (con.outNode > NodeIdx) {
					con.outNode -= 1;
				}
			}
		}

		public Plug GetOutPlug(PlugConnection connection)
		{
			return nodes[connection.outNode].OutPlugs[connection.outPlug];
		}

		public Plug GetInPlug(PlugConnection connection)
		{
			return nodes[connection.inNode].InPlugs[connection.inPlug];
		}
	}

	[Serializable]
	public class PlugConnection : IEquatable<PlugConnection>
	{
		public int outNode;
		public int outPlug;
		public int inNode;
		public int inPlug;

		public Vector2 outPos;
		public Vector2 tan1;
		public Vector2 tan2;
		public Vector2 inPos;

		public PlugConnection(
			int outNode,
			int outPlug,
			int inNode,
			int inPlug)
		{
			this.outNode = outNode;
			this.outPlug = outPlug;
			this.inNode = inNode;
			this.inPlug = inPlug;
		}

		public void UpdateVertices(Plug outPlug, Plug inPlug)
		{
			outPos = outPlug.PlugCenter();
			inPos = inPlug.PlugCenter();
		}

		public bool Equals(PlugConnection other)
		{
			return outNode == other.outNode
					   && outPlug == other.outPlug
					   && inNode == other.inNode
					   && inPlug == other.inPlug;
		}
	}
}