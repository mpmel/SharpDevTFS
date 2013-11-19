using System;
using System.Reflection;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace SharpDevTFS
{
    public class TfsHistoryDialogWrapper
    {
        private readonly Type _dialogHistoryType;
        private readonly object _historyDialogInstance;

        public TfsHistoryDialogWrapper(VersionControlServer versionControl, string historyItem, VersionSpec itemVersion, int itemDeletionId, RecursionType recursionType, VersionSpec versionFrom, VersionSpec versionTo, string userFilter, int maxVersions, bool? slotMode)
        {
            Assembly tfsAssembly = typeof(Microsoft.TeamFoundation.VersionControl.Controls.LocalPathLinkBox).Assembly;
            _dialogHistoryType = tfsAssembly.GetType("Microsoft.TeamFoundation.VersionControl.Controls.DialogHistory");

            _historyDialogInstance = _dialogHistoryType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, 
                new Type[]{typeof(VersionControlServer), typeof(string), typeof(VersionSpec), typeof(int), typeof(RecursionType), typeof(VersionSpec), typeof(VersionSpec), typeof(string), typeof(int), typeof(bool?)},
                null).Invoke(new object[]{ versionControl, historyItem, itemVersion, itemDeletionId, recursionType, versionFrom, versionTo, userFilter, maxVersions, slotMode });
        }

        public void ShowDialog()
        {
            _dialogHistoryType.GetMethod("ShowDialog", new Type[]{}).Invoke(_historyDialogInstance, new object[]{});
        }

    }
}