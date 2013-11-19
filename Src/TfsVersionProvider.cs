// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;

using System.Threading.Tasks;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using Microsoft.Win32.SafeHandles;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
	/// <summary>
	/// Description of TFSVersionProvider.
	/// </summary>
	public class TfsVersionProvider : IDocumentVersionProvider
	{
        public Task<Stream> OpenBaseVersionAsync(FileName fileName)
        {
            return Task<Stream>.Factory.StartNew(
                () =>
                {
                    var path = Path.GetTempFileName();
                    if (!TFS.IsUnderTfsControl(fileName))
                        return null;

                    var item = TFS.GetTfsItem(fileName.ToString());
                    if (item == null) return null;

                    var tfsItem = item.Workspace.VersionControlServer.GetItem(fileName, VersionSpec.Latest, DeletedState.Any, true);
                    if (tfsItem == null) return null;

                    return tfsItem.DownloadFile();
                });
        }
		
		public IDisposable WatchBaseVersionChanges(FileName fileName, EventHandler callback)
		{
            return null;
            //if (!File.Exists(fileName))
            //    return null;
            //if (!TFS.IsUnderTFSControl(fileName))
            //    return null;
			
            //string git = null; // = TFS.FindTFS();
            //if (git == null)
            //    return null;
			
            //return new BaseVersionChangeWatcher(fileName, GetBlobHashAsync(git, fileName).Result, callback);
		}
	}
	
	class BaseVersionChangeWatcher : IDisposable
	{
		EventHandler callback;
		FileName fileName;
		string hash;
		RepoChangeWatcher watcher;
		
		public BaseVersionChangeWatcher(FileName fileName, string hash, EventHandler callback)
		{
//			string root = TFS.FindWorkingCopyRoot(fileName);
//			if (root == null)
//				throw new InvalidOperationException(fileName + " must be under version control!");
//			
//			this.callback = callback;
//			this.fileName = fileName;
//			this.hash = hash;
//			
//			watcher = RepoChangeWatcher.AddWatch(Path.Combine(root, ".git"), HandleChanges);
		}
		
		void HandleChanges()
		{
			return;
            //string newHash = TFSVersionProvider.GetBlobHashAsync(null /*TFS.FindTFS()*/, fileName).Result;
            //if (newHash != hash) {
            //    LoggingService.Info(fileName + " was changed!");
            //    callback(this, EventArgs.Empty);
            //}
            //this.hash = newHash;
		}
		
		public void Dispose()
		{
			watcher.ReleaseWatch(HandleChanges);
		}
	}
}
