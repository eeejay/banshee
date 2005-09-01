/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  HxCallbacks.cs
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
	[StructLayout(LayoutKind.Sequential)]
	public struct ClientEngineCallbacks
	{
		public OnVisualStateChangedCallback OnVisualStateChanged;
		public OnIdealSizeChangedCallback OnIdealSizeChanged;
		public OnLengthChangedCallback OnLengthChanged;
		public OnTitleChangedCallback OnTitleChanged;
		public OnGroupsChangedCallback OnGroupsChanged;
		public OnGroupStartedCallback OnGroupStarted;
		public OnContactingCallback OnContacting;
		public OnBufferingCallback OnBuffering;
		public OnContentStateChangedCallback OnContentStateChanged;
		public OnContentConcludedCallback OnContentConcluded;
		public OnStatusChangedCallback OnStatusChanged;
		public OnVolumeChangedCallback OnVolumeChanged;
		public OnMuteChangedCallback OnMuteChanged;
		public OnClipBandwidthChangedCallback OnClipBandwidthChanged;
		public OnErrorOccurredCallback OnErrorOccurred;
		public GoToURLCallback GoToURL;
		public RequestAuthenticationCallback RequestAuthentication;
		public RequestUpgradeCallback RequestUpgrade;
		public HasComponentCallback HasComponent;
		public PrivateCallback PrivateCallback1;
		public PrivateCallback PrivateCallback2;
	}

	public delegate void OnVisualStateChangedCallback(IntPtr userInfo,
		bool hasVisualContent);

	public delegate void OnIdealSizeChangedCallback(IntPtr userInfo,
		Int32 idealWidth, Int32 idealHeight);

	public delegate void OnLengthChangedCallback(IntPtr userInfo,
		UInt32 length);

	public delegate void OnTitleChangedCallback(IntPtr userInfo, 
		IntPtr pTitle);

	public delegate void OnGroupsChangedCallback(IntPtr userInfo);

	public delegate void OnGroupStartedCallback(IntPtr userInfo,
		UInt16 groupIndex);

	public delegate void OnContactingCallback(IntPtr userInfo, 
		IntPtr pHostName);
	
	public delegate void OnBufferingCallback(IntPtr userInfo, 
		HxBufferReason bufferingReason, UInt16 bufferPercent);

	public delegate void OnContentStateChangedCallback(IntPtr userInfo,
		HxContentState oldContentState, HxContentState newContentState);

	public delegate void OnContentConcludedCallback(IntPtr userInfo);

	public delegate void OnStatusChangedCallback(IntPtr userInfo,
		IntPtr pStatus);

	public delegate void OnVolumeChangedCallback(IntPtr userInfo,
		UInt16 volume);

	public delegate void OnMuteChangedCallback(IntPtr userInfo, 
		bool hasMuted);

	public delegate void OnClipBandwidthChangedCallback(IntPtr userInfo,
		Int32 clipBandwidth);

	public delegate void OnErrorOccurredCallback(IntPtr userInfo,
		UInt32 hxCode, UInt32 userCode, IntPtr pErrorString, 
		IntPtr pUserString, IntPtr pMoreInfoURL);
		
	public delegate bool GoToURLCallback(IntPtr userInfo, IntPtr pURL, 
		IntPtr pTarget, bool isPlayerURL, bool isAutoActivated);

	public delegate bool RequestAuthenticationCallback(IntPtr userInfo,
		IntPtr pServer, IntPtr pRealm, bool isProxyServer);

	public delegate bool RequestUpgradeCallback(IntPtr userInfo,
		IntPtr pURL, UInt32 numOfComponents, IntPtr componentNames,
		bool isBlocking);

	public delegate bool HasComponentCallback(IntPtr userInfo, 
		IntPtr componentName);

	public delegate void PrivateCallback(IntPtr userInfo);
}
