/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
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
		public const ushort PumpEventDelay = 75;
	
		public event ErrorOccurredHandler ErrorOccurred;
		public event LengthChangedHandler LengthChanged;
		public event VolumeChangedHandler VolumeChanged;
		public event ContentStateChangedHandler ContentStateChanged;
		public event StatusChangedHandler StatusChanged;
		public event ContentConcludedHandler ContentConcluded;
		public event MuteChangedHandler MuteChanged;
		public event BufferingHandler Buffering;
	
		private int token;
		private ClientEngineCallbacks callbacks;
	
		private uint length;
		private ushort volume;
		private int bandwidth;
		private string title;
		private string status;
		private HxContentState state;
		private bool muted;
		private string uri;
		
		private bool shouldIterate = false;
		
		public HxPlayer()
		{
			callbacks = new ClientEngineCallbacks();
			callbacks.OnVisualStateChanged = OnVisualStateChanged;
			callbacks.OnIdealSizeChanged = OnIdealSizeChanged;
			callbacks.OnLengthChanged = OnLengthChanged;
			callbacks.OnTitleChanged = OnTitleChanged;
			callbacks.OnGroupsChanged = OnGroupsChanged;
			callbacks.OnGroupStarted = OnGroupStarted;
			callbacks.OnContacting = OnContacting;
			callbacks.OnBuffering = OnBuffering;
			callbacks.OnContentStateChanged = OnContentStateChanged;
			callbacks.OnContentConcluded = OnContentConcluded;
			callbacks.OnStatusChanged = OnStatusChanged;
			callbacks.OnVolumeChanged = OnVolumeChanged;
			callbacks.OnMuteChanged = OnMuteChanged;
			callbacks.OnClipBandwidthChanged = OnClipBandwidthChanged;
			callbacks.OnErrorOccurred = OnErrorOccurred;
			callbacks.GoToURL = GoToURL;
			callbacks.RequestAuthentication = RequestAuthentication;
			callbacks.RequestUpgrade = RequestUpgrade;
			callbacks.HasComponent = HasComponent;
			callbacks.PrivateCallback1 = PrivateCallback;
			callbacks.PrivateCallback2 = PrivateCallback;
			
			IntPtr callbacksRaw = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
			Marshal.StructureToPtr(callbacks, callbacksRaw, false);
				
			try {
				if(!HxUnmanaged.ClientPlayerCreate(out token, IntPtr.Zero, 
					IntPtr.Zero, callbacksRaw))
					throw new ApplicationException("Couldn't create player");
			} catch(NullReferenceException e) {
				throw new ApplicationException(
					"Couldn't create player: No HELIX_LIBS?");
			}
		}

		~HxPlayer()
		{
			Dispose();
		}

		public void Dispose()
		{
			HxUnmanaged.ClientPlayerClose(token);
			GC.SuppressFinalize(this);
		}
		
		public bool OpenUri(string uri)
		{
			shouldIterate = HxUnmanaged.ClientPlayerOpenURL(token, uri,
				null);
				
			if(shouldIterate)
				this.uri = uri;
				
			return shouldIterate;
		}

		public void Play()
		{
			HxUnmanaged.ClientPlayerPlay(token);
		}

		public void Pause()
		{
			HxUnmanaged.ClientPlayerPause(token);
		}
		
		public void Stop()
		{
			HxUnmanaged.ClientPlayerStop(token);
		}

		public void Iterate()
		{
			HxUnmanaged.ClientEngineProcessXEvent(IntPtr.Zero);
		}
		
		// Default Callbacks
		
		private void OnErrorOccurred(IntPtr player, uint hxCode, uint userCode, 
			IntPtr pErrorString, IntPtr pUserString, IntPtr pMoreInfoURL)
		{	
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
			title = Marshal.PtrToStringAnsi(pTitle);
		}

		private void OnLengthChanged(IntPtr player, uint length)
		{
			this.length = length;
				
			LengthChangedHandler handler = LengthChanged;
			if(handler != null) {
				LengthChangedArgs args = new LengthChangedArgs();
				args.Player = this;
				args.Length = length;
				
				handler(this, args);
			}		
		}

		private void OnVolumeChanged(IntPtr player, UInt16 volume)
		{
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
			bandwidth = clipBandwidth;
		}

		private void OnBuffering(IntPtr player, HxBufferReason bufferingReason, 
			UInt16 bufferingPercent)
		{
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
		
		}

		private void OnContacting(IntPtr player, IntPtr pHostName)
		{
		
		}
		
		private void OnIdealSizeChanged(IntPtr player, int idealWidth, 
			int idealHeight)
		{
		
		}
		
		private void OnGroupsChanged(IntPtr player)
		{
		
		}

		private void OnGroupStarted(IntPtr player, UInt16 groupIndex)
		{
		
		}

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
		
		private void PrivateCallback(IntPtr userInfo)
		{
			
		}
				
		public uint Length
		{
			get {
				return length;
			}
		}
		
		public ushort Volume
		{
			get {
				return volume;
			}
			
			set {
				HxUnmanaged.ClientPlayerSetVolume(token, value);
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
			
			set {
				HxUnmanaged.ClientPlayerMute(token, value);
			}
		}
		
		public uint Position
		{
			get {
				return HxUnmanaged.ClientPlayerGetPosition(token);
			}
			
			set {
				HxUnmanaged.ClientPlayerSetPosition(token, value);
			}
		}
	}
}

