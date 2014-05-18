using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LibSiteScraper
{
	sealed class DirectoryTree
	{
		public DirectoryTree()
		{
			m_root = null;
			m_nodes = new SortedSet<DirectoryTreeNode>();
		}

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
			if (origin == null && m_root != null)
				throw new ArgumentNullException("origin");

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
					if (origin != null)
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

	sealed class DirectoryTreeNode : IComparable
	{
		public DirectoryTreeNode(Uri path, HttpStatusCode status)
		{
			m_path = path;
			m_status = status;
			m_links = new Dictionary<string, DirectoryTreeNode>();
		}

		int IComparable.CompareTo(object obj)
		{
			DirectoryTreeNode otherNode = (DirectoryTreeNode) obj;
			if (this.Path != otherNode.Path)
			{
				return Uri.Compare(this.Path, otherNode.Path, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.InvariantCulture);
			}
			else
			{
				if (this.Status < otherNode.Status)
					return -1;
				else if (this.Status > otherNode.Status)
					return 1;
				else
					return 0;
			}
		}

		public Uri Path { get { return m_path; } }
		public HttpStatusCode Status { get { return m_status; } }
		public Dictionary<string, DirectoryTreeNode> Links { get { return m_links; } }

		readonly Uri m_path;
		readonly HttpStatusCode m_status;
		readonly Dictionary<string, DirectoryTreeNode> m_links;
	}
}
