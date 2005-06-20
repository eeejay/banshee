/***************************************************************************
 *  SimpleNotebook.cs
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
using System.Collections;
using Gtk;

namespace Sonance
{
	public class SimpleNotebook : Alignment
	{
		private ArrayList pages;
		private Widget activeWidget;
		
		public event EventHandler PageAdded;
		public event EventHandler PageRemoved;
		public event EventHandler PageCountChanged;
		
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(SimpleNotebook));
				return gtype;
			}
		}
		
		public SimpleNotebook() : base(0.0f, 0.0f, 0.0f, 0.0f)
		{
			pages = new ArrayList();
			Xscale = 1.0f;
		}
		
		private void ExecHandler(EventHandler cb)
		{
			EventHandler handler = cb;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public void AddPage(Widget widget)
		{
			AddPage(widget, false);
		}
		
		public void AddPage(Widget widget, bool changePage)
		{
			pages.Add(widget);
			
			if(changePage)
				ActivePageWidget = widget;
				
			ExecHandler(PageAdded);
			ExecHandler(PageCountChanged);
		}
		
		public void InsertPage(Widget widget, int position)
		{
			pages.Insert(position, widget);
			
			ExecHandler(PageAdded);
			ExecHandler(PageCountChanged);
		}
		
		public void RemovePage(Widget widget)
		{	
			if(activeWidget == widget) {
				activeWidget = null;	
				Remove(widget);
			}
			
			pages.Remove(widget);
			
			if(pages.Count > 0)
				ActivePage = 0;
				
			ExecHandler(PageRemoved);
			ExecHandler(PageCountChanged);
		}
		
		public void RemovePage(int position)
		{
			RemovePage(pages[position] as Widget);
		}
		
		public void NextPage()
		{
			if(ActivePage < pages.Count - 1)
				ActivePage++;
		}
		
		public void PreviousPage()
		{
			if(ActivePage > 0)
				ActivePage--;
		}
		
		public void Cycle()
		{
			if(ActivePage < pages.Count - 1)
				ActivePage++;
			else
				ActivePage = 0;
		}
		
		public int ActivePage 
		{
			set {
				ActivePageWidget = pages[value] as Widget;
			}
			
			get {
				return pages.IndexOf(activeWidget);
			}
		}
		
		public Widget ActivePageWidget
		{
			set {
				Widget newWidget = value;
				
				if(newWidget.Equals(activeWidget))
					return;

				if(activeWidget != null) {
					activeWidget.Visible = false;
					Remove(activeWidget);
				}
				
				Add(newWidget);
				newWidget.Show();
				
				activeWidget = newWidget;
			}
			
			get {
				return activeWidget;
			}
		}
		
		public int Count
		{
			get {
				return pages.Count;
			}
		}
	}
}
