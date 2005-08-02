/***************************************************************************
 *  HxPlayer.cs
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
using System.Runtime.InteropServices;

namespace Helix
{
	public class HxPlayer : IDisposable
	{
		private HandleRef handleRaw;
		private uint handle;
		
		public event ErrorOccurredHandler ErrorOccurred;
		public event LengthChangedHandler LengthChanged;
		public event VolumeChangedHandler VolumeChanged;
		public event ContentStateChangedHandler ContentStateChanged;
		public event StatusChangedHandler StatusChanged;
		public event ContentConcludedHandler ContentConcluded;
		public event MuteChangedHandler MuteChanged;
		public event BufferingHandler Buffering;
		
		private uint length;
		private uint volume;
		private int bandwidth;
		private string title;
		private string status;
		private HxContentState state;
		private bool muted;
		private string uri;
		
		public uint Length
		{
			get {
				return length;
			}
		}
		
		public uint Volume
		{
			get {
				return volume;
			}
			
			set {
				HxUnmanaged.ClientPlayerSetVolume(handle, value);
			}
		}
		
		public int Bandwidth
		{
			get {
				return bandwidth;
			}
		}
		
		public string Title
		{
			get {
				return title;
			}
		}
		
		public string Status
		{
			get {
				return status;
			}
		}
		
		public HxContentState State
		{
			get {
				return state;
			}
		}
		
		public string Uri
		{
			get {
				return uri;
			}
		}
		
		public bool Muted 
		{
			get {
				return muted;
			}
		}
		
		public int Position
		{
			get {
				return HxUnmanaged.HXPlayerGetPosition(handleRaw);
			}
			
			set {
				HxUnmanaged.HXPlayerSetPosition(handleRaw, value);
			}
		}
		
		private bool shouldIterate = false;
		
		public HxPlayer()
		{
			IntPtr ptr = HxUnmanaged.HXPlayerCreate();
			if(ptr.Equals(IntPtr.Zero))
				throw new ApplicationException("Could not create HxPlayer");

			handleRaw = new HandleRef(this, ptr);
		
			if(!HxUnmanaged.HXPlayerInit(handleRaw))
				throw new ApplicationException("Could not initialize HxPlayer");

			handle = HxUnmanaged.HXPlayerGetHandle(handleRaw);
			
			RegisterCallbacks();
		}

		~HxPlayer()
		{
			Dispose();
		}

		public void Dispose()
		{
			HxUnmanaged.HXPlayerShutdown(handleRaw);
			HxUnmanaged.HXPlayerFree(handleRaw);
		}

		private bool IsSelf(IntPtr check)
		{
			return check.Equals(handleRaw.Handle);
		}
	
		private void RegisterCallbacks()
		{
			HxUnmanaged.HXPlayerRegisterOnErrorOccurredCallback(handleRaw,
				OnErrorOccurred);
			HxUnmanaged.HXPlayerRegisterOnVisualStateChangedCallback(handleRaw,
				OnVisualStateChanged);
			HxUnmanaged.HXPlayerRegisterOnContactingCallback(handleRaw,
				OnContacting);
			HxUnmanaged.HXPlayerRegisterOnContentStateChangedCallback(handleRaw,
				OnContentStateChanged);
			HxUnmanaged.HXPlayerRegisterOnStatusChangedCallback(handleRaw,
				OnStatusChanged);
			HxUnmanaged.HXPlayerRegisterOnMuteChangedCallback(handleRaw,
				OnMuteChanged);
			HxUnmanaged.HXPlayerRegisterOnContentConcludedCallback(handleRaw,
				OnContentConcluded);
			HxUnmanaged.HXPlayerRegisterOnTitleChangedCallback(handleRaw,
				OnTitleChanged);
			HxUnmanaged.HXPlayerRegisterOnLengthChangedCallback(handleRaw,
				OnLengthChanged);
			HxUnmanaged.HXPlayerRegisterOnVolumeChangedCallback(handleRaw,
				OnVolumeChanged);
			HxUnmanaged.HXPlayerRegisterOnGroupsChangedCallback(handleRaw,
				OnGroupsChanged);
			HxUnmanaged.HXPlayerRegisterOnIdealSizeChangedCallback(handleRaw,
				OnIdealSizeChanged);
			HxUnmanaged.HXPlayerRegisterHasComponentCallback(handleRaw,
				HasComponent);
			HxUnmanaged.HXPlayerRegisterOnBufferingCallback(handleRaw,
				OnBuffering);
			HxUnmanaged.HXPlayerRegisterRequestUpgradeCallback(handleRaw,
				RequestUpgrade);
			HxUnmanaged.HXPlayerRegisterOnGroupStartedCallback(handleRaw,
				OnGroupStarted);
			HxUnmanaged.HXPlayerRegisterRequestAuthenticationCallback(handleRaw,
				RequestAuthentication);
			HxUnmanaged.HXPlayerRegisterOnClipBandwidthChangedCallback(
				handleRaw, OnClipBandwidthChanged);
			HxUnmanaged.HXPlayerRegisterGoToURLCallback(handleRaw,
				GoToURL);
		}

		public bool OpenUri(string uri)
		{
			Console.WriteLine("URI: " + uri);
		
			shouldIterate = HxUnmanaged.HXPlayerOpenUrl(handleRaw, uri);
				
			if(shouldIterate)
				this.uri = uri;
				
			return shouldIterate;
		}

		public void Play()
		{
			HxUnmanaged.HXPlayerPlay(handleRaw);
		}

		public void Pause()
		{
			HxUnmanaged.HXPlayerPause(handleRaw);
		}
		
		public void Stop()
		{
			HxUnmanaged.HXPlayerStop(handleRaw);
		}

		public bool Iterate()
		{
			HxUnmanaged.HXPlayerIterate(handleRaw);
			return shouldIterate;
		}
		
		// Default Callbacks
		
		private void OnErrorOccurred(IntPtr player, uint hxCode, uint userCode, 
			IntPtr pErrorString, IntPtr pUserString, IntPtr pMoreInfoURL)
		{
			if(!IsSelf(player)) return;
			
			ErrorOccurredHandler handler = ErrorOccurred;
			if(handler != null) {			
				string errorString = Marshal.PtrToStringAnsi(pErrorString);
				string userString = Marshal.PtrToStringAnsi(pUserString);
				string moreInfoUrl = Marshal.PtrToStringAnsi(pMoreInfoURL);
				
				ErrorOccurredArgs args = new ErrorOccurredArgs();
				args.Player = this;
				args.Error = errorString;
				args.UserError = userString;
				args.MoreInfoUrl = moreInfoUrl;
				
				handler(this, args);
			}			
		}

		private void OnContentStateChanged(IntPtr player, 
			HxContentState oldContentState, HxContentState newContentState)
		{
			if(!IsSelf(player)) return;
			
			state = newContentState;
			
			ContentStateChangedHandler handler = ContentStateChanged;
			if(handler != null) {
				ContentStateChangedArgs args = new ContentStateChangedArgs();
				args.Player = this;
				args.NewState = newContentState;
				args.OldState = oldContentState;
				
				handler(this, args);
			}
		}

		private void OnStatusChanged(IntPtr player, IntPtr pStatus)
		{
			if(!IsSelf(player)) return;
			
			status = Marshal.PtrToStringAnsi(pStatus);
			
			StatusChangedHandler handler = StatusChanged;
			if(handler != null) {
				StatusChangedArgs args = new StatusChangedArgs();
				args.Player = this;
				args.Status = status;
				
				handler(this, args);
			}
		}

		private void OnMuteChanged(IntPtr player, bool hasMuted)
		{
			if(!IsSelf(player)) return;
			
			muted = hasMuted;
			
			MuteChangedHandler handler = MuteChanged;
			if(handler != null) {
				MuteChangedArgs args = new MuteChangedArgs();
				args.Player = this;
				args.Muted = muted;
				
				handler(this, args);
			}
		}

		private void OnContentConcluded(IntPtr player)
		{
			if(!IsSelf(player)) return;
			
			ContentConcludedHandler handler = ContentConcluded;
			if(handler != null) {
				HxPlayerArgs args = new HxPlayerArgs();
				args.Player = this;
				
				handler(this, args);
			}
			
			shouldIterate = false;
		}

		private void OnTitleChanged(IntPtr player, IntPtr pTitle)
		{
			if(!IsSelf(player)) return;
			
			title = Marshal.PtrToStringAnsi(pTitle);
		}

		private void OnLengthChanged(IntPtr player, uint length)
		{
			if(!IsSelf(player)) return;
				
			this.length = length;
				
			LengthChangedHandler handler = LengthChanged;
			if(handler != null) {
				LengthChangedArgs args = new LengthChangedArgs();
				args.Player = this;
				args.Length = length;
				
				handler(this, args);
			}		
		}

		private void OnVolumeChanged(IntPtr player, uint volume)
		{
			if(!IsSelf(player)) return;
				
			this.volume = volume;
				
			VolumeChangedHandler handler = VolumeChanged;
			if(handler != null) {
				VolumeChangedArgs args = new VolumeChangedArgs();
				args.Player = this;
				args.Volume = volume;
				
				handler(this, args);
			}		
		}

		private void OnClipBandwidthChanged(IntPtr player, int clipBandwidth)
		{
			if(!IsSelf(player)) return;
			
			bandwidth = clipBandwidth;
		}

		private void OnBuffering(IntPtr player, HxBufferReason bufferingReason, 
			uint bufferingPercent)
		{
			if(!IsSelf(player)) return;
			
			BufferingHandler handler = Buffering;
			if(handler != null) {
				BufferingArgs args = new BufferingArgs();
				args.Player = this;
				args.BufferingReason = bufferingReason;
				args.BufferingPercent = bufferingPercent;
				
				handler(this, args);
			}
		}

		// Not supported yet in these bindings

		private void OnVisualStateChanged(IntPtr player, bool hasVisualContent)
		{
			if(!IsSelf(player)) return;
		}

		private void OnContacting(IntPtr player, IntPtr pHostName)
		{
			if(!IsSelf(player)) return;
		}
		
		private void OnIdealSizeChanged(IntPtr player, int idealWidth, 
			int idealHeight)
		{
			if(!IsSelf(player)) return;
		}
		
		private void OnGroupsChanged(IntPtr player)
		{
			if(!IsSelf(player)) return;
		}

		private void OnGroupStarted(IntPtr player, uint groupIndex)
		{
			if(!IsSelf(player)) return;
		}

		// Not supported yet in HXPlayer glue

		private bool GoToURL(IntPtr player, IntPtr url,  IntPtr target, 
			bool isPlayerUrl, bool isAutoActivated)
		{
			return false;
		}
		
		private bool HasComponent(IntPtr player, IntPtr componentName)
		{	
			return false;
		}

		private bool RequestUpgrade(IntPtr player, IntPtr pUrl, 
			uint numOfComponents, IntPtr componentNamesArr, bool isBlocking)
		{
			return false;
		}

		private bool RequestAuthentication(IntPtr player, IntPtr pServer, 
			IntPtr pRealm, bool isProxyServer)
		{
			return false;
		}
	}
}

