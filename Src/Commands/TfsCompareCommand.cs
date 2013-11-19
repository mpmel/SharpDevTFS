using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
    public class TfsCompareCommand : TFSCommand
    {
        protected override void Execute(string filename, Action callback)
        {
            try
            {
                var item = TFS.GetTfsItem(filename);
                if (item == null) return;
                var itemEx = item.Workspace.VersionControlServer.GetItem(filename, VersionSpec.Latest, DeletedState.Any, true);
                var path = Path.GetTempPath() + itemEx.ChangesetId + "_"+
                           itemEx.ServerItem.Split('/')[itemEx.ServerItem.Split('/').Length - 1];
                itemEx.DownloadFile(path); 

                Difference.VisualDiffFiles(path, filename, "Latest", "Local","Latest", "Local",true, false, true, false);
            }
            catch (Exception ex)
            {
                TFSMessageView.Log("TFSError: Add: " + ex.Message + " (" + filename + ")");
            }
        }
    }
}
