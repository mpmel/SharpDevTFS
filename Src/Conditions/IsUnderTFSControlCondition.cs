using System;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Project;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
	public class IsUnderTFSControlCondition : IConditionEvaluator
	{
		public bool IsValid(object caller, Condition condition)
		{		
			var node = ProjectBrowserPad.Instance.SelectedNode as FileNode;
			if (node != null) 
				return TFS.IsUnderTFSControl(node.FileName);		
			
			var dir = ProjectBrowserPad.Instance.SelectedNode as DirectoryNode;
			if (dir != null) 
				return TFS.IsUnderTFSControl(dir.Directory);	
			
			var sol = ProjectBrowserPad.Instance.SelectedNode as SolutionNode;
			if (sol != null) 
				return TFS.IsUnderTFSControl(sol.Solution.Directory);
			
			return false;
		}
	}

    public class HasPendingChangeCondition : IConditionEvaluator
    {
        public bool IsValid(object caller, Condition condition)
        {
            var node = ProjectBrowserPad.Instance.SelectedNode as FileNode;
            if (node != null)
                return TFS.GetPendingChange(node.FileName) != null;

            var dir = ProjectBrowserPad.Instance.SelectedNode as DirectoryNode;
            if (dir != null)
                return TFS.GetPendingChange(dir.Directory) != null;

            var sol = ProjectBrowserPad.Instance.SelectedNode as SolutionNode;
            if (sol != null)
                return TFS.GetPendingChange(sol.Solution.Directory) != null;

            return false;
        }
    }
	
	public class IsUnControlledInTFSWorkspaceCondition : IConditionEvaluator
	{
		public bool IsValid(object caller, Condition condition)
		{		
			var node = ProjectBrowserPad.Instance.SelectedNode as FileNode;
			if (node != null) 
				return TFS.IsUnControlledInTFSWorkspace(node.FileName);		
			
			var dir = ProjectBrowserPad.Instance.SelectedNode as DirectoryNode;
			if (dir != null) 
				return TFS.IsUnControlledInTFSWorkspace(dir.Directory);	
			
			var sol = ProjectBrowserPad.Instance.SelectedNode as SolutionNode;
			if (sol != null) 
				return TFS.IsUnControlledInTFSWorkspace(sol.Solution.Directory);
			
			return false;
		}
	}
}
