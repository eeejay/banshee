/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  CddbSlaveClient.cs
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

namespace Banshee
{
	[StructLayout(LayoutKind.Sequential)]
	public struct CddbSlaveClientTrackInfoRaw
	{
		public IntPtr name;
		public int length;
		public IntPtr comment;
	}

	public class CddbSlaveClientTrackInfo
	{
		private string name;
		private int length;
		private string comment;
		
		public CddbSlaveClientTrackInfo(IntPtr structPtr) : this(
			(CddbSlaveClientTrackInfoRaw)Marshal.PtrToStructure(
				structPtr, typeof(CddbSlaveClientTrackInfoRaw)))
		{
		
		}
		
		public CddbSlaveClientTrackInfo(CddbSlaveClientTrackInfoRaw raw)
		{
			name = GLib.Marshaller.Utf8PtrToString(raw.name);
			length = raw.length;
			comment = GLib.Marshaller.Utf8PtrToString(raw.comment);
			
			if(name == null)
				name = String.Empty;
				
			if(comment == null)
				comment = String.Empty;
		}
		
		public string Name    { get { return name.Trim();    } }
		public int Length     { get { return length;         } }
		public string Comment { get { return comment.Trim(); } } 
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CORBA_any_struct
	{
		public IntPtr _type;
		public IntPtr _value;
		public bool _release;
	}

	public enum CddbSlaveResultCode 
	{
		Ok,
		RequestPending,
		ErrorContactingServer,
		ErrorRetrievingData,
		MalformedData,
		IoError,
		UnknownEntry
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct CddbSlaveResultRaw
	{
		public IntPtr discid;
		public CddbSlaveResultCode result;
	}
	
	public delegate void CddbSlaveListenerEventCallback(IntPtr listenerPtr,
		IntPtr name, IntPtr bonoboArg, IntPtr corbaEnv, IntPtr data); 
	
	public delegate void CddbSlaveClientEventNotifyHandler(object o,
		CddbSlaveClientEventNotifyArgs args);
		
	public class CddbSlaveClientEventNotifyArgs : EventArgs
	{
		public string DiscId;
		public CddbSlaveResultCode Result;
		public CddbSlaveClientDiscInfo DiscInfo;
		
		public CddbSlaveClientEventNotifyArgs(string discid, 
			CddbSlaveResultCode result, CddbSlaveClientDiscInfo discInfo)
		{
			DiscId = discid;
			Result = result;
			DiscInfo = discInfo;
		}
	}
	
	public class CddbSlaveClient : IDisposable
	{
		private HandleRef clientHandle;
		private HandleRef listenerHandle;
		private CddbSlaveListenerEventCallback eventNotifyCallback;
		
		public event CddbSlaveClientEventNotifyHandler EventNotify;
		
		[DllImport("libbanshee")]
		private static extern IntPtr cddb_slave_client_new();
		
		[DllImport("libbanshee")]
		private static extern void cddb_slave_client_add_listener(
			HandleRef client, HandleRef bonoboListener);
			
		[DllImport("libbanshee")]
		private static extern void cddb_slave_client_remove_listener(
			HandleRef client, HandleRef bonoboListener);
		
		[DllImport("libbonobo-2.so")]
		private static extern bool bonobo_is_initialized();
		
		[DllImport("libbonobo-2.so")]
		private static extern void bonobo_init(int argc, IntPtr argv);
		
		[DllImport("libbonobo-2.so")]
		private static extern IntPtr bonobo_listener_new(IntPtr a, IntPtr b);
	
		[DllImport("libgobject-2.0-0.dll")]
		private static extern void g_signal_connect_data(HandleRef handle, 
			string signal, CddbSlaveListenerEventCallback cb, IntPtr data,
			IntPtr destroyFunc, int flags);
	
		public CddbSlaveClient()
		{
			if(!bonobo_is_initialized())
				bonobo_init(0, IntPtr.Zero);
		
			IntPtr clientPtr = cddb_slave_client_new();
			IntPtr listenerPtr = bonobo_listener_new(IntPtr.Zero, IntPtr.Zero);
			clientHandle = new HandleRef(this, clientPtr);
			listenerHandle = new HandleRef(this, listenerPtr);
			
			eventNotifyCallback = 
				new CddbSlaveListenerEventCallback(OnEventNotify);
			g_signal_connect_data(listenerHandle, "event-notify", 
				eventNotifyCallback, IntPtr.Zero, IntPtr.Zero, 1 << 0);
			cddb_slave_client_add_listener(clientHandle, listenerHandle);
		}

		public void Dispose()
		{
			cddb_slave_client_remove_listener(clientHandle, listenerHandle);
		}
		
		private void OnEventNotify(IntPtr listenerPtr, IntPtr name, 
			IntPtr bonoboArg, IntPtr corbaEnv, IntPtr data)
		{
			CORBA_any_struct arg = 
				(CORBA_any_struct)Marshal.PtrToStructure(bonoboArg, 
					typeof(CORBA_any_struct));
				
			CddbSlaveResultRaw result = 
				(CddbSlaveResultRaw)Marshal.PtrToStructure(arg._value,
					typeof(CddbSlaveResultRaw));
					
			CddbSlaveClientEventNotifyHandler handler = EventNotify;
			if(handler != null) {
				string discid = GLib.Marshaller.Utf8PtrToString(result.discid);
				
				CddbSlaveClientDiscInfo discInfo = null;
				if(result.result == CddbSlaveResultCode.Ok)
					discInfo = new CddbSlaveClientDiscInfo(this, discid);
					
				handler(this, new CddbSlaveClientEventNotifyArgs(discid,
					result.result, discInfo));
			}
		}
		
		[DllImport("libbanshee")]
		private static extern bool cddb_slave_client_query(HandleRef client,
			string discid, int ntrks, string offsets, int nsecs,
			string name, string version);
	
		public bool Query(string discId, int nTracks, string offsets, 
			int nSecs, string name, string version)
		{
			return cddb_slave_client_query(clientHandle, discId, nTracks,
				offsets, nSecs, name, version);
		}
	
		[DllImport("libbanshee")]
		private static extern bool cddb_slave_client_is_valid(HandleRef client,
			string discid);
			
		public bool IsValid(string discId)
		{
			return cddb_slave_client_is_valid(clientHandle, discId);
		}
		
		[DllImport("libbanshee")]
		private static extern IntPtr cddb_slave_client_get_disc_title(
			HandleRef client, string discId);
		
		public string GetDiscTitle(string discId)
		{
			IntPtr ptr = cddb_slave_client_get_disc_title(clientHandle, discId);
			return GLib.Marshaller.Utf8PtrToString(ptr);
		}
		
		[DllImport("libbanshee")]
		private static extern IntPtr cddb_slave_client_get_artist(
			HandleRef client, string discId);
		
		public string GetArtist(string discId)
		{
			IntPtr ptr = cddb_slave_client_get_artist(clientHandle, discId);
			return GLib.Marshaller.Utf8PtrToString(ptr);
		}
		
		[DllImport("libbanshee")]
		private static extern int cddb_slave_client_get_ntrks(
			HandleRef client, string discId);
		
		public int GetTrackCount(string discId)
		{
			return cddb_slave_client_get_ntrks(clientHandle, discId);
		}
		
		[DllImport("libbanshee")]
		private static extern int cddb_slave_client_get_year(
			HandleRef client, string discId);
		
		public int GetYear(string discId)
		{
			return cddb_slave_client_get_year(clientHandle, discId);
		}
		
		[DllImport("libbanshee")]
		private static extern IntPtr cddb_slave_client_get_comment(
			HandleRef client, string discId);
		
		public string GetComment(string discId)
		{
			IntPtr ptr = cddb_slave_client_get_comment(clientHandle, discId);
			return GLib.Marshaller.Utf8PtrToString(ptr);
		}
		
		[DllImport("libbanshee")]
		private static extern IntPtr cddb_slave_client_get_genre(
			HandleRef client, string discId);
		
		public string GetGenre(string discId)
		{
			IntPtr ptr = cddb_slave_client_get_genre(clientHandle, discId);
			return GLib.Marshaller.Utf8PtrToString(ptr);
		}
		
		[DllImport("libbanshee")]
		private static extern IntPtr cddb_slave_client_get_tracks(
			HandleRef client, string discId);
		
		[DllImport("libbanshee")]
		private static extern void cddb_slave_client_free_track_info(
			IntPtr track_info_array);
			
		public CddbSlaveClientTrackInfo [] GetTracks(string discId)
		{
			IntPtr ptr = cddb_slave_client_get_tracks(clientHandle, discId);
			int trackArraySize = 0;
			
			if(ptr == IntPtr.Zero)
				return null;
			
			while(Marshal.ReadIntPtr(ptr, trackArraySize * IntPtr.Size)
				!= IntPtr.Zero)
				trackArraySize++;
				
			CddbSlaveClientTrackInfo [] tracks = 
				new CddbSlaveClientTrackInfo[trackArraySize];
			
			for(int i = 0; i < trackArraySize; i++) {
				IntPtr rawPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
				tracks[i] = new CddbSlaveClientTrackInfo(rawPtr);
			}
			
			cddb_slave_client_free_track_info(ptr);
			
			return tracks;
		}
	}
	
	public class CddbSlaveClientDiscInfo
	{
		private CddbSlaveClient client;
		private string discid;
		
		public CddbSlaveClientDiscInfo(CddbSlaveClient client, string discid)
		{
			this.client = client;
			this.discid = discid;
		}
		
		public bool IsValid     { get { return client.IsValid(discid);       } }
		public string DiscTitle { get { return client.GetDiscTitle(discid);  } } 
		public string Artist    { get { return client.GetArtist(discid);     } }
		public int TrackCount   { get { return client.GetTrackCount(discid); } }
		public int Year         { get { return client.GetYear(discid);       } }
		public string Comment   { get { return client.GetComment(discid);    } }
		public string Genre     { get { return client.GetGenre(discid);      } }
		public string DiscId    { get { return discid;                       } }
		
		public CddbSlaveClient Client 
		{
			get {
				return client;
			}
		}
		
		public CddbSlaveClientTrackInfo [] Tracks 
		{ 
			get { 
				return client.GetTracks(discid); 
			} 
		}
	}
}
