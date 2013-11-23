using System;
using ICSharpCode.SharpDevelop.Project;

namespace SharpDevTFS
{
    public class TFSAddCommand : TFSCommand
    {
        protected override void Execute(string filename, AbstractProjectBrowserTreeNode node, Action callback)
        {
            try {
                TFS.AddFile(filename);
            } 
            catch (Exception ex)
            {
                TFSMessageView.Log("TFSError: Add: " + ex.Message + " (" + filename + ")");
            }				
        }
    }
}