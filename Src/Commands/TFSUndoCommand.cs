using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpDevelop.Project;

namespace SharpDevTFS
{
    public class TFSUndoCommand : TFSCommand
    {
        protected override void Execute(string filename, AbstractProjectBrowserTreeNode node, Action callback)
        {
            try
            {
                TFS.UndoFile(filename);
            }
            catch (Exception ex)
            {
                TFSMessageView.Log("TFSError: Add: " + ex.Message + " (" + filename + ")");
            }
        }
    }
}
