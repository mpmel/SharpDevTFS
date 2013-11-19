// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Workbench;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
	public class RegisterEventsCommand : SimpleCommand
	{
		public RegisterEventsCommand()
		{ 
		}
	
		public override void Execute(object parameter)
		{
		//	workbench.ActiveWorkbenchWindowChanged += ActiveWindowChanged;
			
			FileService.FileCreated += (sender, e) => AddFile(e.FileName);
			FileService.FileCopied += (sender, e) => AddFile(e.TargetFile);
			FileService.FileRemoved += (sender, e) => RemoveFile(e.FileName);
			FileService.FileRenamed += (sender, e) => RenameFile(e.SourceFile, e.TargetFile);
			FileUtility.FileSaved += (sender, e) => TFS.UpdateStatusCacheAndEnqueueFile(e.FileName);
			
			AbstractProjectBrowserTreeNode.OnNewNode += TreeNodeCreated;
			
		}
		
		private void ActiveWindowChanged(object sender, EventArgs e)
		{
		
		}
		
		void AddFile(string fileName)
		{
            TFS.AddFile(fileName);
				
		}


	
		async void RemoveFile(string fileName)
		{
			var item = TFS.GetTFSItem(fileName);
			if (item != null)
			{
				if (item.Workspace.PendDelete(fileName) > 0)
				{
					await TFS.UpdatePendingChanges(item.Workspace);
                    TFS.UpdateStatusCacheAndEnqueueFile(fileName);
				}
				
			}
		}
		
		async void RenameFile(string sourceFileName, string targetFileName)
		{
			var item = TFS.GetTFSItem(sourceFileName);
			if (item != null)
			{
				if (item.Workspace.PendRename(sourceFileName, targetFileName, LockLevel.Unchanged, false, false) > 0)
				{
					await TFS.UpdatePendingChanges(item.Workspace);
					TFS.UpdateStatusCacheAndEnqueueFile(targetFileName);
				}
				
			}
		}
		
		void TreeNodeCreated(object sender, TreeViewEventArgs e)
		{
			SolutionNode sn = e.Node as SolutionNode;
			if (sn != null) {
				OverlayIconManager.Enqueue(sn);
			} else {
				DirectoryNode dn = e.Node as DirectoryNode;
				if (dn != null) {
					OverlayIconManager.Enqueue(dn);
				} else {
					FileNode fn = e.Node as FileNode;
					if (fn != null) {
						OverlayIconManager.Enqueue(fn);
					}
				}
			}
		}

	}
}
