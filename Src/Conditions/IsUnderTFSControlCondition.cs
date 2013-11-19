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
				return TFS.IsUnderTfsControl(node.FileName);

            var proj = ProjectBrowserPad.Instance.SelectedNode as ProjectNode;
            if (proj != null)
                return TFS.IsUnderTfsControl(proj.Project.FileName);	
			
			var dir = ProjectBrowserPad.Instance.SelectedNode as DirectoryNode;
			if (dir != null) 
				return TFS.IsUnderTfsControl(dir.Directory);	
			
			var sol = ProjectBrowserPad.Instance.SelectedNode as SolutionNode;
			if (sol != null) 
				return TFS.IsUnderTfsControl(sol.Solution.Directory);
			
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

            var proj = ProjectBrowserPad.Instance.SelectedNode as ProjectNode;
            if (proj != null)
                return TFS.GetPendingChange(proj.Project.FileName) != null;	

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
				return TFS.IsUnControlledInTfsWorkspace(node.FileName);

            var proj = ProjectBrowserPad.Instance.SelectedNode as ProjectNode;
            if (proj != null)
                return TFS.IsUnControlledInTfsWorkspace(proj.Project.FileName);	

			
			var dir = ProjectBrowserPad.Instance.SelectedNode as DirectoryNode;
			if (dir != null) 
				return TFS.IsUnControlledInTfsWorkspace(dir.Directory);	
			
			var sol = ProjectBrowserPad.Instance.SelectedNode as SolutionNode;
			if (sol != null) 
				return TFS.IsUnControlledInTfsWorkspace(sol.Solution.Directory);
			
			return false;
		}
	}
}
