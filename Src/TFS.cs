// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
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
	
	public class TfsItem
	{
		public TfsItem(string path)
		{
			Path = path;					
			var info = Workstation.Current.GetLocalWorkspaceInfo(path);
			if ( info != null)		
			{
				Workspace = TFS.GetWorkspace(info);
				PendingChange = Workspace.GetPendingChange(path);
			}

			WorkspaceInfo = info;
		}
		
		public string Path { get; set; }

	    public ItemSpec ItemSpec
	    {
	        get { return ItemSpec.FromStrings(new[] {Path}, RecursionType.None)[0]; }

	    }

	    public Workspace Workspace { get; set; }
		public WorkspaceInfo WorkspaceInfo{ get; set; }
		
		PendingChange PendingChange { get; set; }
		
		public ChangeType? GetChangeType()
		{
			PendingChange pendingChange = Workspace.GetPendingChange(Path);
			PendingChange = pendingChange;
			if (PendingChange != null)
			{
				return PendingChange.ChangeType;
			}
			
			if (Workspace.VersionControlServer.ServerItemExists(Path, Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any))
				return ChangeType.None;
		    return null;
		}
		
		public ExtendedItem GetExtendedItem()
		{
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
        static ConcurrentDictionary<string, TfsItem> tfsItemDict = new ConcurrentDictionary<string, TfsItem>();
        static ConcurrentDictionary<string, Workspace> tfsWorkspaceCache = new ConcurrentDictionary<string, Workspace>();
        static ConcurrentDictionary<Workspace, Dictionary<string, PendingChange>> pendingChangesCache = new ConcurrentDictionary<Workspace, Dictionary<string, PendingChange>>(new GenericEqualityComparer<object>(ReferenceEquals, RuntimeHelpers.GetHashCode));
		
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
            var item = GetTfsItem(fileName);
            if (item != null)
                if (item.Workspace.PendAdd(fileName) > 0)            
                    UpdateStatusCacheAndEnqueueFile(fileName);
        }

        public static void UndoFile(string fileName)
        {
            var item = GetTfsItem(fileName);
            if (item != null)
            {
                int changes = item.Workspace.Undo(fileName);
                var status = item.Workspace.Get(new[] { fileName }, VersionSpec.Latest, RecursionType.None, GetOptions.Overwrite);
                TFSMessageView.Log("TFS: {0} changes undone", changes);
                if (changes > 0)
                    UpdateStatusCacheAndEnqueueFile(fileName);
            }
            
        }

        public static async void UpdateStatusCacheAndEnqueueFile(string fileName)
        {
            var item = GetTfsItem(fileName);
            if (item != null)
              await UpdatePendingChanges(item.Workspace);
            ProjectBrowserPad pad = ProjectBrowserPad.Instance;
            if (pad == null) return;
            FileNode node = pad.ProjectBrowserControl.FindFileNode(fileName);
            if (node == null) return;
            OverlayIconManager.EnqueueParents(node);
        }

		public static bool IsUnderTfsControl(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return false;
			var item = GetTfsItem(fileName);
			if (item.Workspace == null)
				return false;
			return item.Workspace.VersionControlServer.ServerItemExists(item.Path, Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any);
		}
		
		public static bool IsUnControlledInTfsWorkspace(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return false;
			var item = GetTfsItem(fileName);
			if (item.Workspace == null)
				return false;

            return !item.Workspace.VersionControlServer.ServerItemExists(item.Path, Microsoft.TeamFoundation.VersionControl.Client.ItemType.Any) && (TFS.GetFileStatus(fileName) == TFSStatus.None);
		}

        public static PendingChange GetPendingChange(this TfsItem item)
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
            
            return null;
        }

        public static PendingChange GetPendingChange(string path)
        {
            var item = GetTfsItem(path);
            if (item == null)
                return null;

            Dictionary<string, PendingChange> changeDict;
            if (pendingChangesCache.TryGetValue(item.Workspace, out changeDict))
            {
                PendingChange pendingChange;
                changeDict.TryGetValue(item.Path, out pendingChange);
                return pendingChange;
            }
            
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
		    
            return null;
		}
		
		public static Workspace GetWorkspace(WorkspaceInfo info)
		{
			lock (getWorkspaceLock) {
				Workspace workspace;
				if (!tfsWorkspaceCache.TryGetValue(info.Name, out workspace))
				{
					workspace = info.GetWorkspace(new TfsTeamProjectCollection(info.ServerUri));
					tfsWorkspaceCache.TryAdd(info.Name, workspace);
				    var changes = workspace.GetPendingChanges();
				    pendingChangesCache.TryAdd(workspace, changes.ToDictionary(x => x.LocalItem));	
					workspace.VersionControlServer.PendingChangesChanged += PendingChangesModified;			    
				}
						
				return workspace;
			}
		}
		
		private static void PendingChangesModified(object sender, WorkspaceEventArgs e)
		{
			
		}
		
		public static TfsItem GetTfsItem(string fileName)
		{
			TfsItem tfsItem;
			if(tfsItemDict.TryGetValue(fileName, out tfsItem))
			{
				return tfsItem;
			}
			
			tfsItem = new TfsItem(fileName);
			tfsItemDict.TryAdd(fileName, tfsItem);
			return tfsItem;
		}

	    public static TFSStatus GetFileStatus(string fileName)
	    {
	        if (string.IsNullOrWhiteSpace(fileName))
	            return TFSStatus.None;

	        var tfsItem = GetTfsItem(fileName);

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
	        catch (Exception)
	        {
	            return TFSStatus.None;
	        }

	        return TFSStatus.None;

	    }

	}
	
}
