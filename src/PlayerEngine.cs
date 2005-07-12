/***************************************************************************
 *  PlayerEngine.cs
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

namespace Sonance
{	
	public class TickEventArgs : System.EventArgs
	{
    	public long Position;
	}
	
	public class VolumeChangedEventArgs : System.EventArgs
	{
		public double Volume;
	}
	
	public delegate void TickEventHandler(object o, TickEventArgs args);
	public delegate void VolumeChangedEventHandler(object o, 
		VolumeChangedEventArgs args);

	abstract public class PlayerEngine
	{
		//public event TickEventHandler Tick;
		//public event System.EventHandler Eos;
		//public event VolumeChangedEventHandler VolumeChanged;
	
		abstract public bool Open(TrackInfo ti);
		abstract public void Close();
		abstract public void Play();
		abstract public void Pause();
		
		abstract public bool Loaded {
			get;
		}
		
		abstract public bool Playing {
			get;
		}
		
		abstract public bool HasFile {
			get;
		}
		
		abstract public bool AtEos {
			get;
		}
		
		abstract public long Position {
			get;
			set;
		}
		
		abstract public double Volume {
			get;
			set;
		}
	}

	public class DummyPlayer : PlayerEngine
	{
		private TrackInfo track;
		private bool playing;
		private long position, updatedPosition;
		private bool atEos;
		
		public DummyPlayer()
		{
			DebugLog.Add("DummyPlayer initialized");
		}
		
		public override bool Open(TrackInfo track)
		{
			this.track = track;
			return true;
		}
		
		public override void Close()
		{
			this.track = null;
		}
		
		public override void Play()
		{
			playing = true;
		}
		
		public override void Pause()
		{
			playing = false;
		}
		
		public override bool Loaded
		{
			get {
				return track != null;
			}
		}
		
		public override bool Playing
		{
			get {
				return playing;
			}
		}
		
		public override bool HasFile
		{
			get {
				return track != null;
			}
		}	
		
		public override bool AtEos {
			get {
				return atEos;
			}
		}
		
		public override long Position {
			get {
				return position;
			}
			
			set {
				updatedPosition = value;
			}
		}
		
		public override double Volume {
			get {
				return 1.0;
			}
			
			set {
			
			}
		}
	}
}
