using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SiteScraper
{
	sealed class DirectoryTree
	{
		public DirectoryTree(DirectoryTreeNode root)
		{
			m_root = root;

			m_nodes = new SortedSet<DirectoryTreeNode>();
			m_nodes.Add(root);
		}

		/// <summary>
		/// Adds a new link to a current node.
		/// </summary>
		/// <param name="origin">The current node.</param>
		/// <param name="path">The path to the linked node.</param>
		/// <param name="status">The status code when reaching the link</param>
		/// <returns>The new discovered node or null.</returns>
		public DirectoryTreeNode AddLink(DirectoryTreeNode origin, string path, HttpStatusCode status)
		{
			if (path == null)
				throw new ArgumentNullException("path");
			if (!m_nodes.Contains(origin))
				throw new ArgumentException("Origin doesn't currently exist. Cannot be the origin of this new link.");

			Uri outUri;
			if (Uri.TryCreate(path, UriKind.Absolute, out outUri))
			{
				DirectoryTreeNode newNode = new DirectoryTreeNode(outUri, status);
				if (m_nodes.Contains(newNode))
				{
					if (origin.Links.ContainsKey(path))
					{
						Console.WriteLine(string.Format(@"Path '{0}' already exists.", path));
					}
					else
					{
						DirectoryTreeNode existingNode = m_nodes.First(x => x == newNode);
						origin.Links.Add(path, existingNode);
					}
				}
				else
				{
					origin.Links.Add(path, newNode);
					m_nodes.Add(newNode);
					return newNode;
				}
			}
			else
			{
				throw new ArgumentException("Incorrect type of Uri");
			}
			return null;
		}

		public DirectoryTreeNode Root { get { return m_root; } }
		public SortedSet<DirectoryTreeNode> Nodes { get { return m_nodes; } }

		readonly DirectoryTreeNode m_root;
		readonly SortedSet<DirectoryTreeNode> m_nodes;
	}

	sealed class DirectoryTreeNode
	{
		public DirectoryTreeNode(Uri path, HttpStatusCode status)
		{
			m_path = path;
			m_status = status;
			m_links = new Dictionary<string, DirectoryTreeNode>();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is DirectoryTreeNode))
				return false;

			DirectoryTreeNode otherNode = (DirectoryTreeNode) obj;

			return (otherNode.Path == this.Path && otherNode.Status == this.Status);
		}

		public override int GetHashCode()
		{
			return (this.Path.AbsoluteUri + this.Status.ToString()).GetHashCode();
		}

		public Uri Path { get { return m_path; } }
		public HttpStatusCode Status { get { return m_status; } }
		public Dictionary<string, DirectoryTreeNode> Links { get { return m_links; } }

		readonly Uri m_path;
		readonly HttpStatusCode m_status;
		readonly Dictionary<string, DirectoryTreeNode> m_links;
	}
}
