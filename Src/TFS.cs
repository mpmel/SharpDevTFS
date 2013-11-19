// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Workbench;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using ICSharpCode.SharpDevelop.Project;

namespace SharpDevTFS
{
	public enum TFSStatus
	{
		None,
		Added,
		Modified,
		Deleted,
		OK,
		Ignored
	}
	
	public class TFSItem
	{
		public TFSItem(string path)
		{
			Stopwatch sw = new Stopwatch();
			try 
			{
				sw.Start();
			Path = path;
			
			
			var info = Workstation.Current.GetLocalWorkspaceInfo(path);
			if ( info != null)		
			{
				Workspace = TFS.GetWorkspace(info);
				PendingChange = Workspace.GetPendingChange(path);
			}
			WorkspaceInfo = info;

			//UpdateInfo();
			sw.Stop();
			} 
			finally 
			{
				SD.Log.DebugFormatted("TFS Item: {0}, Elapsed time: {1}", path, sw.Elapsed.TotalMilliseconds);
			}

		}
		
		public string Path { get; set; }
		///public ExtendedItem ExtendedItem { get; set; }
		public ItemSpec ItemSpec { get {
			    return ItemSpec.FromStrings(new[] { Path }, RecursionType.None)[0];
			}
			
		}
		public Workspace Workspace { get; set; }
		public WorkspaceInfo WorkspaceInfo{ get; set; }
		
		PendingChange PendingChange { get; set; }
		
		public ChangeType? GetChangeType()
		{
			PendingChange pendingChange = TFS.GetPendingChange(Workspace, Path);
			PendingChange = pendingChange;
			if (PendingChange != null)
			{
				return PendingChange.ChangeType;
			}
			
			if (Workspace.VersionControlServer.ServerItemExists(Path, Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any))
				return ChangeType.None;
			else
				return null;
			    
		}
		
		
		
		public ExtendedItem GetExtendedItem()
		{
			var pending = Workspace.GetPendingChanges();
			if (Workspace == null)
				return null;
			
			var extendedItems = Workspace.GetExtendedItems(
			new[]{ ItemSpec.FromStrings(new[] { Path }, RecursionType.None)[0] }, 
			DeletedState.Any, 
			Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any, 
			GetItemsOptions.None);
			
			if (extendedItems[0].Length == 0)
				return null;
			
			return extendedItems[0][0];
		}
	}
		
	public class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equals;

        private readonly Func<T, int> _getHashCode;

        public GenericEqualityComparer(Func<T, T, bool> equals = null, Func<T, int> getHashCode = null)
        {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        public bool Equals(T x, T y)
        {
            return _equals != null ? _equals(x, y) : object.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _getHashCode != null ? _getHashCode(obj) : (obj as object).GetHashCode();
        }
    }
	
	/// <summary>
	/// Description of TFSStatusCache.
	/// </summary>
	public static class TFS
	{	
		static object getStatusLock = new object();
		static object getWorkspaceLock = new object();
		static Dictionary<string, TFSItem> tfsItemDict = new Dictionary<string, TFSItem>();
		static Dictionary<string, Workspace> tfsWorkspaceCache = new Dictionary<string, Workspace>();
		static Dictionary<Workspace, Dictionary<string, PendingChange>> pendingChangesCache = new Dictionary<Workspace, Dictionary<string, PendingChange>>(new GenericEqualityComparer<object>(ReferenceEquals, RuntimeHelpers.GetHashCode));
		
		public static Task UpdatePendingChanges(Workspace workspace)
		{	
			return Task.Factory.StartNew(
			() => 
			{
				if (workspace != null)
				{
					var changes = workspace.GetPendingChanges().ToDictionary(x => x.LocalItem);
				   	pendingChangesCache[workspace] = changes;
				}
			});		
		}
	

        public static void AddFile(string fileName)
        {
            var item = TFS.GetTFSItem(fileName);
            if (item != null)
                if (item.Workspace.PendAdd(fileName) > 0)            
                    TFS.UpdateStatusCacheAndEnqueueFile(fileName);
        }

        public static void UndoFile(string fileName)
        {
            var item = TFS.GetTFSItem(fileName);
            if (item != null)
            {
                int changes = item.Workspace.Undo(fileName);
                var status = item.Workspace.Get(new[] { fileName }, VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);
                TFSMessageView.Log("TFS: {0} changes undone", changes);
                if (changes > 0)
                    TFS.UpdateStatusCacheAndEnqueueFile(fileName);
            }
            
        }

        public static async void UpdateStatusCacheAndEnqueueFile(string fileName)
        {
            var item = GetTFSItem(fileName);
            if (item != null)
              await TFS.UpdatePendingChanges(item.Workspace);
            ProjectBrowserPad pad = ProjectBrowserPad.Instance;
            if (pad == null) return;
            FileNode node = pad.ProjectBrowserControl.FindFileNode(fileName);
            if (node == null) return;
            OverlayIconManager.EnqueueParents(node);
        }

		public static bool IsUnderTFSControl(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return false;
			var item = TFS.GetTFSItem(fileName);
			if (item.Workspace == null)
				return false;
			return item.Workspace.VersionControlServer.ServerItemExists(item.Path, Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any);
		}
		
		public static bool IsUnControlledInTFSWorkspace(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return false;
			var item = TFS.GetTFSItem(fileName);
			if (item.Workspace == null)
				return false;

            return !item.Workspace.VersionControlServer.ServerItemExists(item.Path, Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any) && (TFS.GetFileStatus(fileName) == TFSStatus.None);
		}

        public static PendingChange GetPendingChange(this TFSItem item)
        {
            if (item == null)
                return null;

            Dictionary<string, PendingChange> changeDict;
            if (pendingChangesCache.TryGetValue(item.Workspace, out changeDict))
            {
                PendingChange pendingChange;
                changeDict.TryGetValue(item.Path, out pendingChange);
                return pendingChange;
            }
            else
                return null;

        }

        public static PendingChange GetPendingChange(string path)
        {
            var item = GetTFSItem(path);
            if (item == null)
                return null;

            Dictionary<string, PendingChange> changeDict;
            if (pendingChangesCache.TryGetValue(item.Workspace, out changeDict))
            {
                PendingChange pendingChange;
                changeDict.TryGetValue(item.Path, out pendingChange);
                return pendingChange;
            }
            else
                return null;

        }
		
		public static PendingChange GetPendingChange(this Workspace workspace, string path)
		{
			Dictionary<string, PendingChange> changeDict;
			if (pendingChangesCache.TryGetValue(workspace, out changeDict))
			{
				PendingChange pendingChange;
				changeDict.TryGetValue(path, out pendingChange);
				return pendingChange;			
			}
			else
				return null;
			
		}
		
		public static Workspace GetWorkspace(WorkspaceInfo info)
		{
			lock (getWorkspaceLock) {
				Workspace workspace;
				if (!tfsWorkspaceCache.TryGetValue(info.Name, out workspace))
				{
					workspace = info.GetWorkspace(new TeamFoundationServer(info.ServerUri));
					tfsWorkspaceCache.Add(info.Name, workspace);
				    var changes = workspace.GetPendingChanges();
				    pendingChangesCache.Add(workspace, changes.ToDictionary(x => x.LocalItem));	
					workspace.VersionControlServer.PendingChangesChanged += PendingChangesModified;			    
				}
						
				return workspace;
			}
		}
		
		private static void PendingChangesModified(object sender, WorkspaceEventArgs e)
		{
			
		}
		
		public static TFSItem GetTFSItem(string fileName)
		{
			TFSItem tfsItem;
			if(tfsItemDict.TryGetValue(fileName, out tfsItem))
			{
				return tfsItem;
			}
			
			tfsItem = new TFSItem(fileName);
			tfsItemDict.Add(fileName, tfsItem);
			return tfsItem;
		}
		
		public static TFSStatus GetFileStatus(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return TFSStatus.None;
			    
			TFSItem tfsItem = GetTFSItem(fileName);
			
			if (tfsItem.WorkspaceInfo == null)
				return TFSStatus.None;

		try
		{					
			var change = tfsItem.GetChangeType();
			if (change == null)
				return TFSStatus.None;
			
			if (change.Value == ChangeType.None)
				return TFSStatus.OK;
			
			if ((change.Value & ChangeType.Add) == ChangeType.Add)
				return TFSStatus.Added;
			
			if ((change.Value & ChangeType.Edit) == ChangeType.Edit)
				return TFSStatus.Modified;
			
		}
		catch(Exception ex)
		{
			return TFSStatus.None;
		}
	
		return TFSStatus.None;
			
		}
			
	}
	
}
