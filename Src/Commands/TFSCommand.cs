/*
 * Created by SharpDevelop.
 * User: MikeM
 * Date: 11/16/2013
 * Time: 9:36 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Workbench;

namespace SharpDevTFS
{
	public class ToolCommand1 : AbstractMenuCommand
	{
		public override void Run()
		{
			// Here an example that shows how to access the current text document:
			ITextEditor textEditor = SD.GetActiveViewContentService<ITextEditor>();
			var aa = SD.Workbench.ActiveContent;
			//var tecp = SD.Workbench.ActiveContent as ITextEditorControlProvider;
			if (textEditor == null) {
				// active content is not a text editor control
				return;
			}
			// Get the active text area from the control:
			var textArea = textEditor.Document;// tecp.TextEditorControl.ActiveTextAreaControl.TextArea;
			/*if (!textArea.SelectionManager.HasSomethingSelected)
				return;
			// get the selected text:
			string text = textArea.SelectionManager.SelectedText;
			// reverse the text:
			StringBuilder b = new StringBuilder(text.Length);
			for (int i = text.Length - 1; i >= 0; i--)
				b.Append(text[i]);
			string newText = b.ToString();
			// ensure caret is at start of selection
			textArea.Caret.Position = textArea.SelectionManager.SelectionCollection[0].StartPosition;
			// deselect text
			textArea.SelectionManager.ClearSelection();
			// replace the selected text with the new text:
			// Replace() takes the arguments: start offset to replace, length of the text to remove, new text
			textArea.Document.Replace(textArea.Caret.Offset,
			                          text.Length,
			                          newText);
			// Redraw:
			textArea.Refresh();*/
		}
	}

	
	public abstract class TFSCommand : SimpleCommand
	{
		protected abstract void Execute(string filename, Action callback);
		
		public override void Execute(object parameter)
		{
			AbstractProjectBrowserTreeNode node = ProjectBrowserPad.Instance.SelectedNode;
			if (node != null) {
				string nodeFileName = null;
			    var projectNode = node as ProjectNode;
			    if (projectNode != null)
			        nodeFileName = projectNode.Project.FileName;
			    else
			    {
			        var directoryNode = node as DirectoryNode;
			        if (directoryNode != null)
			            nodeFileName = directoryNode.Directory;
			        else
			        {
			            var fileNode = node as FileNode;
			            if (fileNode != null)
			                nodeFileName = fileNode.FileName;
			            else
			            {
			                var solutionNode = node as SolutionNode;
			                if (solutionNode != null)
			                    nodeFileName = solutionNode.Solution.Directory;
			            }
			        }
			    }
			    if (nodeFileName != null) {
					List<OpenedFile> unsavedFiles = new List<OpenedFile>();
					foreach (OpenedFile file in SD.FileService.OpenedFiles) {
						if (file.IsDirty && !file.IsUntitled) {
							if (string.IsNullOrEmpty(file.FileName)) continue;
							if (FileUtility.IsUrl(file.FileName)) continue;
							if (FileUtility.IsBaseDirectory(nodeFileName, file.FileName)) {
								unsavedFiles.Add(file);
							}
						}
					}
					if (unsavedFiles.Count > 0) {
						if (MessageService.ShowCustomDialog(
							MessageService.DefaultMessageBoxTitle,
							"The version control operation would affect files with unsaved modifications.\n" +
							"You have to save those files before running the operation.",
							0, 1,
							"Save files", "Cancel")
						    == 0)
						{
							// Save
							foreach (OpenedFile file in unsavedFiles) {
								ICSharpCode.SharpDevelop.Commands.SaveFile.Save(file);
							}
						} else {
							// Cancel
							return;
						}
					}
					// now run the actual operation:
					Execute(nodeFileName, AfterCommand(nodeFileName, node));
				}
			}
		}
		
		Action AfterCommand(string nodeFileName, AbstractProjectBrowserTreeNode node)
		{
			return delegate {
				SD.MainThread.VerifyAccess();
				// and then refresh the project browser:
			//////////	TFSStatusCache.ClearCachedStatus(nodeFileName);
				OverlayIconManager.EnqueueRecursive(node);
				OverlayIconManager.EnqueueParents(node);
			};
		}
	}
}
