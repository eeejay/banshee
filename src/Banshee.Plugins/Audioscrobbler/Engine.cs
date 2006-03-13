
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
using System.Web;

using GLib;
using Banshee.MediaEngine;
using Banshee.Base;
using Banshee;

namespace Banshee.Plugins.Audioscrobbler {
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
		const int FAILURE_LOG_MINUTES = 5; /* 5 minute delay on logging failure to upload information */
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
		DateTime last_upload_failed_logged;

		Queue queue;

		bool song_started; /* if we were watching the current song from the beginning */
		bool queued; /* if current_track has been queued */
		bool sought; /* if the user has sought in the current playing song */

		WebRequest current_web_req;
		IAsyncResult current_async_result;
		State state;
		
		public Engine ()
		{
			timeout_id = 0;
			state = State.IDLE;
			queue = new Queue ();
		}

		public void Start ()
		{
			song_started = false;
			PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
			if (timeout_id == 0) {
				timeout_id = Timeout.Add (TICK_INTERVAL, StateTransitionHandler);
			}
			queue.Load ();
		}

		public void Stop ()
		{
			PlayerEngineCore.EventChanged -= OnPlayerEngineEventChanged;

			if (timeout_id != 0) {
				GLib.Source.Remove (timeout_id);
				timeout_id = 0;
			}

			if (current_web_req != null) {
				current_web_req.Abort ();
			}

			queue.Save ();
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
			if(pass == null || pass == String.Empty)
				return String.Empty;
				
			MD5 md5 = MD5.Create ();

			byte[] hash = md5.ComputeHash (Encoding.ASCII.GetBytes (pass));

			return CryptoConvert.ToHex (hash).ToLower ();
		}

		void OnPlayerEngineEventChanged(object o, PlayerEngineEventArgs args)
		{
			switch (args.Event) {
				/* Queue if we're watching this song from the beginning,
				 * it isn't queued yet and the user didn't seek until now,
				 * we're actually playing, song position and length are greater than 0
				 * and we already played half of the song or 240 seconds */
				case PlayerEngineEvent.Iterate:
					if (song_started && !queued && !sought && PlayerEngineCore.CurrentState == PlayerEngineState.Playing &&
						PlayerEngineCore.Length > 0 && PlayerEngineCore.Position > 0 &&
						(PlayerEngineCore.Position > PlayerEngineCore.Length / 2 || PlayerEngineCore.Position > 240)) {
							TrackInfo track = PlayerEngineCore.CurrentTrack;
							if (track == null) {
								queued = sought = false;
							} else {
								queue.Add (track, DateTime.Now - TimeSpan.FromSeconds (PlayerEngineCore.Position));
								queued = true;
							}
					}
					break;
				/* Start of Stream: new song started */
				case PlayerEngineEvent.StartOfStream:
					queued = sought = false;
					song_started = true;
					break;
				/* End of Stream: song finished */
				case PlayerEngineEvent.EndOfStream:
					song_started = queued = sought = false;
					break;
				/* Did the user seek? */
				case PlayerEngineEvent.Seek:
					sought = true;
					break;
			}
		}

		bool StateTransitionHandler()
		{
			/* if we're not connected, don't bother doing anything
			 * involving the network. */
			if (!Globals.Network.Connected)
				return true;

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

		//
		// Async code for transmitting the current queue of tracks
		//
		class TransmitState {
			public StringBuilder StringBuilder;
			public int Count;
		}

		void TransmitQueue ()
		{
			int num_tracks_transmitted;

			/* save here in case we're interrupted before we complete
			 * the request.  we save it again when we get an OK back
			 * from the server */
			queue.Save ();

			next_interval = DateTime.MinValue;

           		if(post_url == null) {
				return;
			}

			StringBuilder sb = new StringBuilder ();

			sb.AppendFormat ("u={0}&s={1}", HttpUtility.UrlEncode (username), security_token);

			sb.Append (queue.GetTransmitInfo (out num_tracks_transmitted));

			current_web_req = WebRequest.Create (post_url);
			current_web_req.Method = "POST";
			current_web_req.ContentType = "application/x-www-form-urlencoded";
			current_web_req.ContentLength = sb.Length;

			TransmitState ts = new TransmitState ();
			ts.Count = num_tracks_transmitted;
			ts.StringBuilder = sb;

			state = State.WAITING_FOR_REQ_STREAM;
			current_async_result = current_web_req.BeginGetRequestStream (TransmitGetRequestStream, ts);
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
			
			DateTime now = DateTime.Now;
			if (line.StartsWith ("FAILED")) {
				if (now - last_upload_failed_logged > TimeSpan.FromMinutes(FAILURE_LOG_MINUTES)) {
					LogCore.Instance.PushWarning ("Audioscrobbler upload failed", line.Substring ("FAILED".Length).Trim(), false);
					last_upload_failed_logged = now;
				}
				/* retransmit the queue on the next interval */
				state = State.NEED_TRANSMIT;
			}
			else if (line.StartsWith ("BADUSER")
					 || line.StartsWith ("BADAUTH")) {
				if (now - last_upload_failed_logged > TimeSpan.FromMinutes(FAILURE_LOG_MINUTES)) {
					LogCore.Instance.PushWarning ("Audioscrobbler upload failed", "invalid authentication", false);
					last_upload_failed_logged = now;
				}
				/* attempt to re-handshake (and retransmit) on the next interval */
				security_token = null;
				next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
				state = State.IDLE;
				return;
			}
			else if (line.StartsWith ("OK")) {
				/* if we've previously logged failures, be nice and log the successful upload. */
				if (last_upload_failed_logged != DateTime.MinValue) {
					LogCore.Instance.PushInformation ("Audioscrobbler upload succeeded", "", false);
					last_upload_failed_logged = DateTime.MinValue;
				}
				/* we succeeded, pop the elements off our queue */
				queue.RemoveRange (0, ts.Count);
				queue.Save ();
				state = State.IDLE;
			}
			else {
				if (now - last_upload_failed_logged > TimeSpan.FromMinutes(FAILURE_LOG_MINUTES)) {
					LogCore.Instance.PushDebug ("Audioscrobbler upload failed", String.Format ("Unrecognized response: {0}", line), false);
					last_upload_failed_logged = now;
				}
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
										HttpUtility.UrlEncode (username));

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
				LogCore.Instance.PushWarning ("Audioscrobbler sign-on failed", line.Substring ("FAILED".Length).Trim(), false);
								   
			}
			else if (line.StartsWith ("BADUSER")) {
				LogCore.Instance.PushWarning ("Audioscrobbler sign-on failed", "unrecognized user/password", false);
			}
			else if (line.StartsWith ("UPDATE")) {
				LogCore.Instance.PushInformation ("Audioscrobbler plugin needs updating",
											String.Format ("Fetch a newer version at {0}\nor update to a newer version of Banshee",
														   line.Substring ("UPDATE".Length).Trim()), false);
				success = true;
			}
			else if (line.StartsWith ("UPTODATE")) {
				success = true;
			}
			
			/* read the challenge string and post url, if
			 * this was a successful handshake */
			if (success == true) {
				string challenge = sr.ReadLine ().Trim ();
				post_url = sr.ReadLine ().Trim ();

				security_token = MD5Encode (md5_pass + challenge);
				//Console.WriteLine ("security token = {0}", security_token);
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
