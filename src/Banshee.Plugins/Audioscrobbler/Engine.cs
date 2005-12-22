/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Engine.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Chris Toshok (toshok@ximian.com)
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
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Mono.Security.Cryptography;
using System.Collections;

using GLib;
using Banshee.MediaEngine;
using Banshee.Base;
using Banshee;

namespace Banshee.Plugins.Audioscrobbler {
	public class QueuedTrack {
		public QueuedTrack (TrackInfo track, DateTime start_time)
		{
			this.track = track;
			this.start_time = start_time.ToUniversalTime ();
		}

		public DateTime StartTime {
			get { return start_time; }
		}
		public TrackInfo Track {
			get { return track; }
		}

		TrackInfo track;
		DateTime start_time;
	}

	class Engine
	{
		enum State {
			IDLE,
			NEED_HANDSHAKE,
			NEED_TRANSMIT,
			WAITING_FOR_REQ_STREAM,
			WAITING_FOR_HANDSHAKE_RESP,
			WAITING_FOR_RESP
		};

		const int TICK_INTERVAL = 2000; /* 2 seconds */
		const int RETRY_SECONDS = 60; /* 60 second delay for transmission retries */
		const string CLIENT_ID = "bsh";
		const string CLIENT_VERSION = "0.1";
		const string SCROBBLER_URL = "http://post.audioscrobbler.com/";
		const string SCROBBLER_VERSION = "1.1";

		string username;
		string md5_pass;
		string post_url;
		string security_token;

		uint timeout_id;
		DateTime next_interval;
		
		bool queued; /* if current_track has been queued */
		TrackInfo current_track;
		ArrayList queue;

		WebRequest current_web_req;
		IAsyncResult current_async_result;
		State state;
		
		public Engine ()
		{
			queue = new ArrayList ();
			state = State.IDLE;
		}

		public void Start ()
		{
			if (timeout_id == 0) {
				timeout_id = Timeout.Add (TICK_INTERVAL, EngineTick);
			}
		}

		public void Stop ()
		{
			if (timeout_id != 0) {
				Source.Remove (timeout_id);
				timeout_id = 0;
			}

			/* XXX interrupt the current web requests somehow.. */
		}

		public void SetUserPassword (string username, string pass)
		{
			if (username == "" || pass == "")
				return;

			this.username = username;
			this.md5_pass = MD5Encode (pass);

			if (security_token != null) {
				security_token = null;
				state = State.NEED_HANDSHAKE;
			}
		}

		string MD5Encode (string pass)
		{
			MD5 md5 = MD5.Create ();

			byte[] hash = md5.ComputeHash (Encoding.ASCII.GetBytes (pass));

			return CryptoConvert.ToHex (hash).ToLower ();
		}

		bool EngineTick ()
		{
			IPlayerEngine player = PlayerEngineCore.ActivePlayer;

			/* check the currently playing track (if there is one) */
			if (player.Playing
			    && player.Length != 0) {

				TrackInfo track = player.Track;

				/* did the user switch tracks? */
				if (track != current_track) {
					current_track = track;
					queued = false;
				}

				/* Each song should be posted to the server
				   when it is 50% or 240 seconds complete,
				   whichever comes first. */
				if (!queued
				    && (player.Position > player.Length / 2
					|| player.Position > 240)) {
					queue.Add (new QueuedTrack (track,
												(DateTime.Now 
												 - TimeSpan.FromSeconds (player.Position))));
					queued = true;
				}
			}
			else {
				current_track = null;
				queued = false;
			}

			//			if (old_state != state)
			//				Console.WriteLine ("state change ({0} -> {1})", old_state, state);

			/* and address changes in our engine state */
			switch (state) {
			case State.IDLE:
				if (queue.Count > 0) {
					if (username != null && md5_pass != null && security_token == null)
						state = State.NEED_HANDSHAKE;
					else 
						state = State.NEED_TRANSMIT;
				}
				break;
			case State.NEED_HANDSHAKE:
				if (DateTime.Now > next_interval) {
					Handshake ();
				}
				break;
			case State.NEED_TRANSMIT:
				if (DateTime.Now > next_interval) {
					TransmitQueue ();
				}
				break;
			case State.WAITING_FOR_RESP:
			case State.WAITING_FOR_REQ_STREAM:
			case State.WAITING_FOR_HANDSHAKE_RESP:
				/* nothing here */
				break;
			}

			return true;
		}

		string UrlEncode (string data)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < data.Length; i ++) {
				char c = data[i];
				if (c == ' ')
					sb.Append ('+');
				else if (c == '&')
					sb.Append ("%26");
				else if (c == '+')
					sb.Append ("%2b");
				else if (c == '=')
					sb.Append ("%3d");
				else
					sb.Append (c);
				/* i know this is lacking.  sue me. */
			}
			return sb.ToString ();
		}


		//
		// Async code for transmitting the current queue of tracks
		//
		class TransmitState {
			public StringBuilder StringBuilder;
			public int Count;
		}

		void TransmitQueue ()
		{
			//			Console.WriteLine ("TransmitQueue ():");

			next_interval = DateTime.MinValue;

			StringBuilder sb = new StringBuilder ();

			sb.AppendFormat ("u={0}&s={1}", username, security_token);

			int i;
			for (i = 0; i < queue.Count; i ++) {
				/* we queue a maximum of 10 tracks per request */
				if (i == 9) break;

				QueuedTrack qtrack = (QueuedTrack)queue[i];
				TrackInfo track = qtrack.Track;

				sb.AppendFormat (
						 "&a[{6}]={0}&t[{6}]={1}&b[{6}]={2}&m[{6}]={3}&l[{6}]={4}&i[{6}]={5}",
						 UrlEncode (track.Artist),
						 UrlEncode (track.Title),
						 UrlEncode (track.Album),
						 "" /* musicbrainz id */,
						 ((int)track.Duration.TotalSeconds).ToString (),
						 UrlEncode (qtrack.StartTime.ToString ("yyyy-MM-dd HH:mm:ss")),
						 i);
			}

			//			Console.WriteLine ("data to post = {0}", sb.ToString ());

			current_web_req = WebRequest.Create (post_url);
			current_web_req.Method = "POST";
			current_web_req.ContentType = "application/x-www-form-urlencoded";
			current_web_req.ContentLength = sb.Length;

			TransmitState ts = new TransmitState ();
			ts.Count = i;
			ts.StringBuilder = sb;

			state = State.WAITING_FOR_REQ_STREAM;
			current_async_result = current_web_req.BeginGetRequestStream (TransmitGetRequestStream,
																		  ts);
			if (current_async_result == null) {
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				state = State.IDLE;
			}
		}

		void TransmitGetRequestStream (IAsyncResult ar)
		{
			Stream stream;

			try {
				stream = current_web_req.EndGetRequestStream (ar);
			}
			catch (Exception e) {
				Console.WriteLine ("Failed to get the request stream: {0}", e);

				state = State.IDLE;
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				return;
			}

			TransmitState ts = (TransmitState) ar.AsyncState;
			StringBuilder sb = ts.StringBuilder;

			StreamWriter writer = new StreamWriter (stream);
			writer.Write (sb.ToString ());
			writer.Close ();

			state = State.WAITING_FOR_RESP;
			current_async_result = current_web_req.BeginGetResponse (TransmitGetResponse, ts);
			if (current_async_result == null) {
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				state = State.IDLE;
			}
		}

		void TransmitGetResponse (IAsyncResult ar)
		{
			WebResponse resp;

			try {
				resp = current_web_req.EndGetResponse (ar);
			}
			catch (Exception e) {
				Console.WriteLine ("Failed to get the response: {0}", e);

				state = State.IDLE;
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				return;
			}

			TransmitState ts = (TransmitState) ar.AsyncState;

			Stream s = resp.GetResponseStream ();

			StreamReader sr = new StreamReader (s, Encoding.UTF8);

			string line;
			line = sr.ReadLine ();
			
			if (line.StartsWith ("FAILED")) {
				Console.WriteLine ("Failed with {0}", line.Substring ("FAILED".Length).Trim());
				/* retransmit the queue on the next interval */
				state = State.NEED_TRANSMIT;
			}
			else if (line.StartsWith ("BADAUTH")) {
				security_token = null;
				Console.WriteLine ("Failed with BADAUTH");
				/* attempt to re-handshake (and retransmit) on the next interval */
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				state = State.IDLE;
				return;
			}
			else if (line.StartsWith ("OK")) {
				/* we succeeded, pop the elements off our queue */
				queue.RemoveRange (0, ts.Count);
				state = State.IDLE;
			}
			else {
				Console.WriteLine ("Unrecognized response: {0}", line);
				state = State.IDLE;
			}

			/* now get the next interval */
			line = sr.ReadLine ();
			if (line.StartsWith ("INTERVAL")) {
				int interval_seconds = Int32.Parse (line.Substring ("INTERVAL".Length));
				next_interval = DateTime.Now + new TimeSpan (0, 0, interval_seconds);
			}
			else {
				Console.WriteLine ("expected INTERVAL..");
			}
		}

		//
		// Async code for handshaking
		//
		void Handshake ()
		{
			string uri = String.Format ("{0}?hs=true&p={1}&c={2}&v={3}&u={4}",
						    SCROBBLER_URL,
						    SCROBBLER_VERSION,
						    CLIENT_ID, CLIENT_VERSION,
						    username);

			current_web_req = WebRequest.Create (uri);

			state = State.WAITING_FOR_HANDSHAKE_RESP;
			current_async_result = current_web_req.BeginGetResponse (HandshakeGetResponse, null);
			if (current_async_result == null) {
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				state = State.IDLE;
			}
		}

		void HandshakeGetResponse (IAsyncResult ar)
		{
			bool success = false;
			WebResponse resp;

			try {
				resp = current_web_req.EndGetResponse (ar);
			}
			catch (Exception e) {
				Console.WriteLine ("failed to handshake: {0}", e);

				/* back off for a time before trying again */
				state = State.IDLE;
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				return;
			}

			Stream s = resp.GetResponseStream ();

			StreamReader sr = new StreamReader (s, Encoding.UTF8);

			string line;

			line = sr.ReadLine ();
			if (line.StartsWith ("FAILED")) {
				Console.WriteLine ("Audioscrobbler handshake failed with '{0}'",
								   line.Substring ("FAILED".Length).Trim());
			}
			else if (line.StartsWith ("BADUSER")) {
				Console.WriteLine (
							"Audioscrobbler handshake failed with 'unrecognized user/password'");
			}
			else if (line.StartsWith ("UPDATE")) {
				Console.WriteLine ("Succeeded (but client needs updating from url {0})",
								   line.Substring ("UPDATE".Length).Trim());
				success = true;
			}
			else if (line.StartsWith ("UPTODATE")) {
				Console.WriteLine ("Succeeded (client up-to-date)");
				success = true;
			}
			
			/* read the challenge string and post url, if
			 * this was a successful handshake */
			if (success == true) {
				string challenge = sr.ReadLine ().Trim ();
				post_url = sr.ReadLine ().Trim ();

				security_token = MD5Encode (md5_pass + challenge);
				Console.WriteLine ("security token = {0}", security_token);
			}

			/* read the trailing interval */
			line = sr.ReadLine ();
			if (line.StartsWith ("INTERVAL")) {
				int interval_seconds = Int32.Parse (line.Substring ("INTERVAL".Length));
				next_interval = DateTime.Now + new TimeSpan (0, 0, interval_seconds);
			}
			else {
				Console.WriteLine ("expected INTERVAL..");
			}

			/* XXX we shouldn't just try to handshake again for BADUSER */
			state = success ? State.IDLE : State.NEED_HANDSHAKE;
		}
	}
}
