// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.SharpDevelop.Gui;

namespace SharpDevTFS
{
	/// <summary>
	/// Output pad category for git.
	/// </summary>
	public static class TFSMessageView
	{
		static MessageViewCategory category;
		
		/// <summary>
		/// Gets the git message view category.
		/// </summary>
		public static MessageViewCategory Category {
			get {
				if (category == null) {
                    MessageViewCategory.Create(ref category, "Source Control - Team Foundation");
				}
				return category;
			}
		}
		
		/// <summary>
		/// Appends a line to the git message view.
		/// </summary>
		public static void Log(string text, params object[] args)
		{
			Category.AppendLine(string.Format(text, args));
		}

	}
}
