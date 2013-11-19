﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.Core.WinForms;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;

namespace SharpDevTFS
{
	/// <summary>
	/// Description of OverlayIconManager.
	/// </summary>
	public class OverlayIconManager
	{
		static Queue<AbstractProjectBrowserTreeNode> queue = new Queue<AbstractProjectBrowserTreeNode>();
		static HashSet<AbstractProjectBrowserTreeNode> inQueue = new HashSet<AbstractProjectBrowserTreeNode>();
		static bool threadRunning;
		
		public static void Enqueue(AbstractProjectBrowserTreeNode node)
		{
			lock (queue) {
				if (inQueue.Add(node)) {
					queue.Enqueue(node);
					if (!threadRunning) {
						threadRunning = true;
						ThreadPool.QueueUserWorkItem(Run);
					}
				}
			}
		}
		
		public static void EnqueueRecursive(AbstractProjectBrowserTreeNode node)
		{
			lock (queue) {
				if (inQueue.Add(node))
					queue.Enqueue(node);
				// use breadth-first search
				Queue<AbstractProjectBrowserTreeNode> q = new Queue<AbstractProjectBrowserTreeNode>();
				q.Enqueue(node);
				while (q.Count > 0) {
					node = q.Dequeue();
					foreach (TreeNode n in node.Nodes) {
						node = n as AbstractProjectBrowserTreeNode;
						if (node != null) {
							q.Enqueue(node);
							if (inQueue.Add(node))
								queue.Enqueue(node);
						}
					}
				}
				
				if (!threadRunning) {
					threadRunning = true;
					ThreadPool.QueueUserWorkItem(Run);
				}
			}
		}
		
		public static void EnqueueParents(AbstractProjectBrowserTreeNode node)
		{
			lock (queue) {
				while (node != null) {
					if (inQueue.Add(node))
						queue.Enqueue(node);
					node = node.Parent as AbstractProjectBrowserTreeNode;
				}
				
				if (!threadRunning) {
					threadRunning = true;
					ThreadPool.QueueUserWorkItem(Run);
				}
			}
		}
		
		static void Run(object state)
		{
			LoggingService.Debug("Git: OverlayIconManager Thread started");
			// sleep a tiny bit to give main thread time to add more jobs to the queue
			Thread.Sleep(100);
			while (true) {
				if (SD.ParserService.LoadSolutionProjectsThread.IsRunning) {
					// Run OverlayIconManager much more slowly while solution is being loaded.
					// This prevents the disk from seeking too much
					Thread.Sleep(100);
				}
				AbstractProjectBrowserTreeNode node;
				lock (queue) {
					if (queue.Count == 0) {
						LoggingService.Debug("Git: OverlayIconManager Thread finished");
						Debug.Assert(inQueue.Count == 0);
						inQueue.Clear();
						threadRunning = false;
						return;
					}
					node = queue.Dequeue();
					inQueue.Remove(node);
				}
				try {
					RunStep(node);
				} catch (Exception ex) {
					MessageService.ShowException(ex);
				}
			}
		}
		
		static void RunStep(AbstractProjectBrowserTreeNode node)
		{
			if (node.IsDisposed) return;
			
			var fileNode = node as FileNode;
			TFSStatus status;
			if (fileNode != null) {
				status = TFS.GetFileStatus(fileNode.FileName);
			} else {
				
				var projectNode = node as ProjectNode;
				if (projectNode != null) {
					status = TFS.GetFileStatus(projectNode.Project.FileName);
				} else {				
					var directoryNode = node as DirectoryNode;
					if (directoryNode != null) {
						status = TFS.GetFileStatus(directoryNode.Directory);
					} else {
						var solNode = node as SolutionNode;
						if (solNode != null) {
							status = TFS.GetFileStatus(solNode.Solution.Directory);
						} else {
							return;
						}
					}
				}
			}
			
			SD.MainThread.InvokeAsyncAndForget(delegate {
				Image image = GetImage(status);
				if (image != null) {
					node.Overlay = image;
				} else
		if (node.Overlay != null && (node.Overlay.Tag as Type) == typeof(OverlayIconManager)) {
					// reset overlay to null only if the old overlay belongs to the OverlayIconManager
					node.Overlay = null;
				}
			});
		}
		
		public static Image GetImage(TFSStatus status)
		{
			switch (status) {
				case TFSStatus.Added:
					return GetImage(StatusIcon.Added);
				case TFSStatus.Deleted:
					return GetImage(StatusIcon.Deleted);
				case TFSStatus.Modified:
					return GetImage(StatusIcon.Modified);
				case TFSStatus.OK:
					return GetImage(StatusIcon.OK);
				default:
					return null;
			}
		}
		
		#region SVN icons
		static Bitmap statusImages;
		
		public static Bitmap StatusImages {
			get {
				if (statusImages == null) {
					statusImages = WinFormsResourceService.GetBitmap("Icons.Svn.StatusImages");
				}
				return statusImages;
			}
		}
		
		enum StatusIcon {
			Empty = 0,
			OK,
			Added,
			Deleted,
			Info,
			Empty2,
			Exclamation,
			PropertiesModified,
			Unknown,
			Modified
		}
		
		static Image[] statusIcons = new Image[10];
		
		static Image GetImage(StatusIcon status)
		{
			int index = (int)status;
			if (statusIcons[index] == null) {
				Bitmap statusImages = StatusImages;
				Bitmap smallImage = new Bitmap(7, 10);
				using (Graphics g = Graphics.FromImage(smallImage)) {
					//g.DrawImageUnscaled(statusImages, -index * 7, -3);
					Rectangle srcRect = new Rectangle(index * 7, 3, 7, 10);
					Rectangle destRect = new Rectangle(0, 0, 7, 10);
					g.DrawImage(statusImages, destRect, srcRect, GraphicsUnit.Pixel);
					//g.DrawLine(Pens.Black, 0, 0, 7, 10);
				}
				smallImage.Tag = typeof(OverlayIconManager);
				statusIcons[index] = smallImage;
			}
			return statusIcons[index];
		}
		#endregion
	}
}
