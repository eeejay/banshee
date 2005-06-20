/***************************************************************************
 *  LibraryTransactions.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Text.RegularExpressions;
using Gtk;

namespace Sonance 
{
	public sealed class Dnd
	{
		public enum TargetType {
			UriList,
			PlayList,
			ModelRow,
			SourceView
		};
		
		public static readonly TargetEntry TargetUriList = 
			new TargetEntry("text/uri-list", 0, (uint)TargetType.UriList);
	
		public static readonly TargetEntry TargetPlayList = 
			new TargetEntry("SONANCE_PLAYLIST", TargetFlags.App, 
				(uint)TargetType.PlayList);

		public static readonly TargetEntry TargetTreeModelRow = 
			new TargetEntry("SONANCE_TREE_MODEL_ROW", TargetFlags.Widget, 
				(uint)TargetType.ModelRow);

		public static readonly TargetEntry TargetSourceView = 
			new TargetEntry("SONANCE_SOURCE_VIEW", TargetFlags.Widget, 
				(uint)TargetType.SourceView);

		public static string SelectionDataToString(Gtk.SelectionData data)
		{
			return System.Text.Encoding.UTF8.GetString(data.Data);
		}

		public static string [] SplitSelectionData(Gtk.SelectionData data)
		{
			string str = SelectionDataToString(data);
			return SplitSelectionData(str);
		}

		public static string [] SplitSelectionData(string data)
		{
			return Regex.Split(data, "\r\n");
		}
		
		public static TargetEntry [] sourceViewDestEntries = 
			new TargetEntry [] {
				Dnd.TargetTreeModelRow,
				Dnd.TargetSourceView,
				Dnd.TargetPlayList
			};
	}
}
