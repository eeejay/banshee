/***************************************************************************
 *  ImageAnimation.cs
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
using Gdk;

public class ImageAnimation : Gtk.Image
{
	static GLib.GType gtype;
	public static new GLib.GType GType
	{
		get {
			if(gtype == GLib.GType.Invalid)
				gtype = RegisterGType(typeof(ImageAnimation));
			return gtype;
		}
	}
	
	private Pixbuf sourcePixbuf;
	private Pixbuf inactivePixbuf;
	private int frameWidth, frameHeight, maxFrames, currentFrame;
	private uint refreshRate;
	private Pixbuf [] frames;
	private bool active = true;
	
	protected ImageAnimation() : base()
	{
		
	}
	
	public ImageAnimation(Pixbuf sourcePixbuf, uint refreshRate, 
		int frameWidth, int frameHeight) :
		this(sourcePixbuf, refreshRate, frameWidth, frameHeight, 0)
	{
	}
	
	public ImageAnimation(Pixbuf sourcePixbuf, uint refreshRate, 
		int frameWidth, int frameHeight, int maxFrames) : base()
	{
		Load(sourcePixbuf, frameWidth, frameHeight, maxFrames);
		this.refreshRate = refreshRate;
		GLib.Timeout.Add(refreshRate, new GLib.TimeoutHandler(OnTimeout));
	}
	
	public void Load(Pixbuf sourcePixbuf, int frameWidth, int frameHeight, 
		int maxFrames)
	{
		this.sourcePixbuf = sourcePixbuf;
		this.frameWidth = frameWidth;
		this.frameHeight = frameHeight;
		this.maxFrames = maxFrames;
		SpliceImage();	
	}

	public Pixbuf InactivePixbuf {
		set {
			inactivePixbuf = value;
			if(!active)
				Pixbuf = value;
		}
	}
	
	public void SetActive()
	{
		active = true;
	}
	
	public void SetInactive()
	{
		active = false;
	}
	
	private void SpliceImage()
	{
		int width, height, rows, cols, frameCount;
		
		if(sourcePixbuf == null)
			throw new Exception("No source pixbuf specified");
			
		width = sourcePixbuf.Width;
		height = sourcePixbuf.Height;
		
		if(width % frameWidth != 0 || height % frameHeight != 0)
			throw new Exception("Invalid frame dimensions");
			
		rows = height / frameHeight;
		cols = width / frameWidth;
		frameCount = rows * cols;
		
		frames = new Pixbuf[maxFrames > 0 ? maxFrames : frameCount];
		
		bool doBreak = false;
		
		for(int y = 0, n = 0; y < rows; y++) {
			for(int x = 0; x < cols; x++, n++) {
				frames[n] = new Pixbuf(sourcePixbuf,
					x * frameWidth,
					y * frameHeight,
					frameWidth,
					frameHeight
				);
				
				if(maxFrames > 0 && n >= maxFrames - 1) {
					doBreak = true;
					break;
				}
			}
			
			if(doBreak)
				break;
		}
		
		currentFrame = 0;
	}
	
	private bool OnTimeout()
	{
		if(!active) {
			if(inactivePixbuf != null && Pixbuf != inactivePixbuf)
				Pixbuf = inactivePixbuf;
				
			return true;
		}
	
		if(frames == null || frames.Length == 0)
			return true;
			
		Pixbuf = frames[currentFrame++ % frames.Length];
			
		return true;
	}
}
