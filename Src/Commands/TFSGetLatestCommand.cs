using System;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
    public class TFSGetLatestCommand : TFSCommand
    {
        protected override void Execute(string filename, Action callback)
        {
            var item = TFS.GetTfsItem(filename);
            if(item != null)
            {
                try {
							
                    GetRequest request = new GetRequest(item.ItemSpec, VersionSpec.Latest);
                    GetStatus status = item.Workspace.Get(request, GetOptions.None); // this line doesn't do anything - no failures or errors
                    if (status.NoActionNeeded)
                        TFSMessageView.Log("All files are up to date.");
                    else
                        TFSMessageView.Log(filename + " updated.");
					
                } 
                catch (Exception ex)
                {
					
				
                }
				
            }
        }
    }
}