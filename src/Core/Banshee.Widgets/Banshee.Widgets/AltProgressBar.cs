
/***************************************************************************
 *  AltProgressBar.cs
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
using Gtk;
using Pango;

public class AltProgressBar : Widget
{
	private double fraction;
	private string text;
	
	private Gdk.Window m_refGdkWindow;

	public AltProgressBar() : base()
	{
		SetFlag(WidgetFlags.NoWindow);
	}

	protected override void OnSizeRequested(ref Requisition req)
	{
		req.Width = 10;
		req.Height = 10;

		base.OnSizeRequested(ref req);
	}

	protected override void OnSizeAllocated(Gdk.Rectangle allocation)
	{
		if(m_refGdkWindow != null) {
			m_refGdkWindow.MoveResize(allocation.X, allocation.Y,
				allocation.Width, allocation.Height);
		}
		base.OnSizeAllocated (allocation);
	}

   	protected override void OnMapped()
	{
		m_refGdkWindow.Show ();
		base.OnMapped ();
	}
	
	protected override void OnRealized()
	{
		base.OnRealized();

		Gdk.WindowAttr attributes = new Gdk.WindowAttr();
		attributes.X = Allocation.X;
		attributes.Y = Allocation.Y;
		attributes.Width = Allocation.Width;
		attributes.Height = Allocation.Height;
		attributes.EventMask = (int)Events |
			(int)(Gdk.EventMask.ButtonPressMask | 
			      Gdk.EventMask.ButtonReleaseMask | 
			      Gdk.EventMask.ExposureMask);
		attributes.Wclass = Gdk.WindowClass.InputOnly;

		m_refGdkWindow = new Gdk.Window(GdkWindow, attributes, 
			Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y);
		m_refGdkWindow.UserData = Handle;
	}

	protected override void OnUnrealized () 
	{
		m_refGdkWindow.Dispose();
		m_refGdkWindow = null;
		base.OnUnrealized();
	}

	protected override bool OnExposeEvent(Gdk.EventExpose expose)
	{
		int x = Allocation.X;
		int y = Allocation.Y;
		int width = Allocation.Width;
		int height = Allocation.Height;
		
		int barX = x + 2;
		int barY = y + 2;
		int barWidth = (int)((fraction * (double)width)) - 2;
		int barHeight = height - 5;
		
		Gtk.Style.PaintBox(this.Style, GdkWindow, StateType.Normal,
			ShadowType.In, expose.Area, this, "trough", 
			x, y, width - 1, height - 1);

		if(barWidth > 0) {
			Gtk.Style.PaintBox(this.Style, GdkWindow, StateType.Selected,
				ShadowType.Out, expose.Area, this, "bar", 
				barX, barY, barWidth, barHeight);
				
			Gtk.Style.PaintShadow(this.Style, GdkWindow, StateType.Selected,
				ShadowType.Out, expose.Area, this, "",
				barX, barY, barWidth, barHeight);
		}
                
		if(text != null && text != String.Empty) {
			Pango.Layout layout = new Pango.Layout(PangoContext);
			FontDescription fd = PangoContext.FontDescription.Copy();
			layout.FontDescription = fd;
			layout.SetMarkup(text);
		
			int layoutWidth, layoutHeight;
			int layoutX, layoutY;
		
			layout.GetPixelSize(out layoutWidth, out layoutHeight);

			if(layoutWidth < width - 4 && layoutHeight < height - 4) {
				layoutX = x + (width / 2 - layoutWidth / 2);
				layoutY = y + (height / 2 - layoutHeight / 2);

				GdkWindow.DrawLayout(Style.ForegroundGC(StateType.Normal),
					layoutX, layoutY, layout);
			}
		} 

		return base.OnExposeEvent(expose);
	}
	
	public double Fraction
	{
		get {
			return fraction;
		}

		set {
			fraction = value;

			if(fraction < 0.0)
				fraction = 0.0;
			if(fraction > 1.0)
				fraction = 1.0;
				
			QueueDraw();
		}
	}

	public string Text
	{
		get {
			return text;
		}

		set {
			text = value;

			QueueDraw();
		}
	}
}

/*
public class AltProgressBar : Widget
{
	private double fraction;
	private string text;
	
	public AltProgressBar() : base()
	{
		SetFlag(WidgetFlags.NoWindow);
	}

	protected override void OnSizeRequested(ref Requisition req)
	{
		req = new Requisition();
		req.Width = 10;
		req.Height = 10;
		base.OnSizeRequested(ref req);
	}

	protected override bool OnExposeEvent(Gdk.EventExpose expose)
	{
		int x = Allocation.X;
		int y = Allocation.Y;
		int width = Allocation.Width;
		int height = Allocation.Height;
		
		int barX = x + 2;
		int barY = y + 2;
		int barWidth = (int)((fraction * (double)width)) - 3;
		int barHeight = height - 4;

		//GdkWindow.DrawRectangle(Style.BackgroundGC(StateType.Active),
		//	false, x, y, width - 1, height - 1);

		this.Style.PaintBox(
		
		Style, 
		
		GdkWindow, 
			StateType.Active, ShadowType.In,
			null, this, "trough", x, y, width - 1, height - 1);
			
			

		if(barWidth > 0)
			GdkWindow.DrawRectangle(Style.BackgroundGC(StateType.Selected),
				true, barX, barY, barWidth, barHeight);

		if(text != null && text != String.Empty) {
			Pango.Layout layout = new Pango.Layout(PangoContext);
			FontDescription fd = PangoContext.FontDescription.Copy();
			layout.FontDescription = fd;
			layout.SetMarkup(text);
		
			int layoutWidth, layoutHeight;
			int layoutX, layoutY;
		
			layout.GetPixelSize(out layoutWidth, out layoutHeight);

			if(layoutWidth < width - 4 && layoutHeight < height - 4) {
				layoutX = x + (width / 2 - layoutWidth / 2);
				layoutY = y + (height / 2 - layoutHeight / 2);

				GdkWindow.DrawLayout(Style.ForegroundGC(StateType.Normal),
					layoutX, layoutY, layout);
			}
		} 

		return base.OnExposeEvent(expose);
	}
	
	public double Fraction
	{
		get {
			return fraction;
		}

		set {
			fraction = value;

			if(fraction < 0.0)
				fraction = 0.0;
			if(fraction > 1.0)
				fraction = 1.0;
				
			QueueDraw();
		}
	}

	public string Text
	{
		get {
			return text;
		}

		set {
			text = value;
			QueueDraw();
		}
	}
}

*/