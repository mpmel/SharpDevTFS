using System;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
    public class TFSViewHistoryCommand : TFSCommand
    {
        protected override void Execute(string filename, Action callback)
        {
            var item = TFS.GetTfsItem(filename);
            if(item != null)
            {
				
                try {
                    var historyList = item.Workspace.VersionControlServer.QueryHistory(item.ItemSpec).ToList();
                    var earliest = historyList.OrderByDescending(x => x.CreationDate).FirstOrDefault();
					
					
					
                    var wrapper = new TfsHistoryDialogWrapper(
                        item.Workspace.VersionControlServer, 
                        filename, 
                        VersionSpec.Latest, 
                        item.ItemSpec.DeletionId, 
                        RecursionType.Full, 
                        new DateVersionSpec(earliest.CreationDate),
                        VersionSpec.Latest,
                        string.Empty, 
                        int.MaxValue, 
                        false);
                    wrapper.ShowDialog();
					
                    //item.Workspace.VersionControlServer.GetItem(filename);
                } 
                catch (Exception ex)
                {
					
				
                }
				
            }
        }
    }
}