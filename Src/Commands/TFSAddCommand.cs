using System;

namespace SharpDevTFS
{
    public class TFSAddCommand : TFSCommand
    {
        protected override void Execute(string filename, Action callback)
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