/***************************************************************************
 *  HxCallbackDelegates.cs
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
	public delegate void OnErrorOccurredCallback(IntPtr player,
		uint hxCode, uint userCode, IntPtr pErrorString, IntPtr pUserString,
		IntPtr pMoreInfoURL);

	public delegate void OnVisualStateChangedCallback(IntPtr player,
		bool hasVisualContent);

	public delegate void OnContactingCallback(IntPtr player,
		IntPtr pHostName);

	public delegate void OnContentStateChangedCallback(IntPtr player,
		HxContentState oldContentState, HxContentState newContentState);

	public delegate void OnStatusChangedCallback(IntPtr player,
		IntPtr pStatus);

	public delegate void OnMuteChangedCallback(IntPtr player,
		bool hasMuted);

	public delegate void OnContentConcludedCallback(IntPtr player);

	public delegate void OnTitleChangedCallback(IntPtr player,
		IntPtr pTitle);

	public delegate void OnLengthChangedCallback(IntPtr player,
		 uint length);

	public delegate void OnVolumeChangedCallback(IntPtr player,
		 uint volume);

	public delegate void OnGroupsChangedCallback(IntPtr player);

	public delegate void OnIdealSizeChangedCallback(IntPtr player,
		 int idealWidth, int idealHeight);

	public delegate void OnBufferingCallback(IntPtr player,
		 HxBufferReason bufferingReason, uint bufferingPercent);

	public delegate void OnGroupStartedCallback(IntPtr player,
		 uint groupIndex);

	public delegate void OnClipBandwidthChangedCallback(IntPtr player,
		 int clipBandwidth);

	public delegate bool GoToURLCallback(IntPtr player, IntPtr url,
		 IntPtr target, bool isPlayerUrl, bool isAutoActivated);

	public delegate bool HasComponentCallback(IntPtr player,
		 IntPtr componentName);

	public delegate bool RequestUpgradeCallback(IntPtr player,
		 IntPtr pUrl, uint numOfComponents, IntPtr componentNamesArr,
		 bool isBlocking);

	public delegate bool RequestAuthenticationCallback(IntPtr player,
		 IntPtr pServer, IntPtr pRealm, bool isProxyServer);
}
