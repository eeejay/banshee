/***************************************************************************
 *  HxUnmanaged.cs
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
	public class HxUnmanaged
	{
		// HxPlayer Core Functions
	
		[DllImport("hxplayer")]
		public static extern IntPtr HXPlayerCreate();

		[DllImport("hxplayer")]
		public static extern bool HXPlayerInit(HandleRef player);

		[DllImport("hxplayer")]
		public static extern void HXPlayerShutdown(HandleRef player);

		[DllImport("hxplayer")]
		public static extern void HXPlayerFree(HandleRef player);

		[DllImport("hxplayer")]
		public static extern int HXPlayerIterate(HandleRef player);

		[DllImport("hxplayer")]
		public static extern uint HXPlayerGetHandle(HandleRef player);

		// HxPlayer Callback Registration Functions
			
		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnErrorOccurredCallback(
			HandleRef player, OnErrorOccurredCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnVisualStateChangedCallback(
			HandleRef player, OnVisualStateChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnContactingCallback(
			HandleRef player, OnContactingCallback cb);

		[DllImport("hxplayer")]
		public static extern void 
			HXPlayerRegisterOnContentStateChangedCallback(
				HandleRef player, OnContentStateChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnStatusChangedCallback(
			HandleRef player, OnStatusChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnMuteChangedCallback(
			HandleRef player, OnMuteChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnContentConcludedCallback(
			HandleRef player, OnContentConcludedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnTitleChangedCallback(
			HandleRef player, OnTitleChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnLengthChangedCallback(
			HandleRef player, OnLengthChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnVolumeChangedCallback(
			HandleRef player, OnVolumeChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnGroupsChangedCallback(
			HandleRef player, OnGroupsChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnIdealSizeChangedCallback(
			HandleRef player, OnIdealSizeChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnBufferingCallback(
			HandleRef player, OnBufferingCallback cb);
			
		[DllImport("hxplayer")]
		public static extern void HXPlayerRegisterOnGroupStartedCallback(
			HandleRef player, OnGroupStartedCallback cb);

		[DllImport("hxplayer")]
		public static extern void 
			HXPlayerRegisterOnClipBandwidthChangedCallback(
				HandleRef player, OnClipBandwidthChangedCallback cb);

		[DllImport("hxplayer")]
		public static extern bool HXPlayerRegisterGoToURLCallback(
			HandleRef player, GoToURLCallback cb);
			
		[DllImport("hxplayer")]
		public static extern bool 
			HXPlayerRegisterRequestAuthenticationCallback(
				HandleRef player, RequestAuthenticationCallback cb);	
		
		[DllImport("hxplayer")]
		public static extern bool HXPlayerRegisterHasComponentCallback(
			HandleRef player, HasComponentCallback cb);
		
		[DllImport("hxplayer")]
		public static extern bool HXPlayerRegisterRequestUpgradeCallback(
			HandleRef player, RequestUpgradeCallback cb);
	
		// hxclientkit Core Functions

		[DllImport("hxclient")]
		public static extern void ClientPlayerSetVolume(uint handle, 
			uint volume);

		/*[DllImport("hxclient")]
		public static extern bool ClientPlayerOpenURL(uint handle,
			string url, IntPtr ptr);

		[DllImport("hxclient")]
		public static extern void ClientPlayerPlay(uint handle);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerPause(uint handle);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerStop(uint handle);
		
		[DllImport("hxclient")]
		public static extern int ClientPlayerGetPosition(uint handle);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerSetPosition(uint handle, 
			int position);*/
			
		[DllImport("hxplayer")]
		public static extern bool HXPlayerOpenUrl(HandleRef player,
			string url);
			
		[DllImport("hxplayer")]
		public static extern void HXPlayerSetVolume(HandleRef player,
			uint volume);
		
		[DllImport("hxplayer")]
		public static extern uint HXPlayerGetVolume(HandleRef player);
		
		[DllImport("hxplayer")]
		public static extern void HXPlayerPlay(HandleRef player);
		
		[DllImport("hxplayer")]
		public static extern void HXPlayerPause(HandleRef player);
		
		[DllImport("hxplayer")]
		public static extern void HXPlayerStop(HandleRef player);
		
		[DllImport("hxplayer")]
		public static extern int HXPlayerGetPosition(HandleRef player);
		
		[DllImport("hxplayer")]
		public static extern void HXPlayerSetPosition(HandleRef player,
			int position);
	}
}
