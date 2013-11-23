using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpDevelop.Project;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
    public class TFSGetLatestCommand : TFSCommand
    {
    	private void GetNodesRecursive(List<AbstractProjectBrowserTreeNode> nodes, AbstractProjectBrowserTreeNode node)
    	{
    		foreach (var node1 in node.Nodes.OfType<AbstractProjectBrowserTreeNode>())
                {
                    nodes.Add(node1);
                    GetNodesRecursive(nodes, node1);
                }
    	}
    	
        protected override void Execute(string filename, AbstractProjectBrowserTreeNode node, Action callback)
        {
            try
            {
                var nodes = new List<AbstractProjectBrowserTreeNode> {node};

                GetNodesRecursive(nodes, node);
                var anythingUpdated = false;

                foreach (var treeNode in nodes)
                {
                    var itemPath = TFS.GetFileName(treeNode);
                    if (string.IsNullOrWhiteSpace(itemPath)) continue;
                    var item = TFS.GetTfsItem(itemPath);
                    if (item == null) continue;
                    GetRequest request = new GetRequest(item.ItemSpec, VersionSpec.Latest);
                    GetStatus status = item.Workspace.Get(request, GetOptions.None);
                    if (!status.NoActionNeeded)
                       TFSMessageView.Log(item.Path + " updated.");                    	
                    
                    anythingUpdated = anythingUpdated || !status.NoActionNeeded;
                }

                if (!anythingUpdated)
                    TFSMessageView.Log("All files are up to date.");
            }
            catch (Exception ex)
            {
            }
        }
    }
}