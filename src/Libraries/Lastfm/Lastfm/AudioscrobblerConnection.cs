//
// AudioscrobblerConnection.cs
//
// Author:
//   Chris Toshok <toshok@ximian.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Security.Cryptography;
using Mono.Security.Cryptography;
using System.Web;

using Hyena;

namespace Lastfm
{
    public class AudioscrobblerConnection
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
        const int MAX_RETRY_SECONDS = 7200; /* 2 hours, as defined in the last.fm protocol */
        const int TIME_OUT = 5000; /* 5 seconds timeout for webrequests */
        const string CLIENT_ID = "bsh";
        const string CLIENT_VERSION = "0.1";
        const string SCROBBLER_URL = "http://post.audioscrobbler.com/";
        const string SCROBBLER_VERSION = "1.2";

        Account account;
        string user_agent;
        string post_url;
        string session_id = null;
        string now_playing_url;
        bool now_playing_submitted = false;
        bool connected = false;
        public bool Connected {
            get { return connected; }
        }
        
        bool started = false;
        public bool Started {
            get { return started; }
        }
        
        public string UserAgent {
            get { return user_agent; }
            set { user_agent = value; }
        }

        System.Timers.Timer timer;
        DateTime next_interval;
        DateTime last_upload_failed_logged;

        IQueue queue;
        
        int hard_failures = 0;
        int hard_failure_retry_sec = 60;
        
        HttpWebRequest now_playing_post;
        HttpWebRequest current_web_req;
        IAsyncResult current_async_result;
        State state;
        
        internal AudioscrobblerConnection (Account account, IQueue queue) : this (account, queue, "")
        {
        }
        
        internal AudioscrobblerConnection (Account account, IQueue queue, string user_agent)
        {
            this.account = account;
            
            account.Updated += AccountUpdated;
            
            state = State.IDLE;
            this.queue = queue;
            
            this.user_agent = user_agent;
        }
        
        private void AccountUpdated (object o, EventArgs args)
        {
            Stop ();
            session_id = null;
            Connect ();
        }
        
        public void Connect ()
        {
            if (!started) {
                Start ();
            }
        
            if (session_id == null && started) {
                if (connected) {
                    state = State.NEED_HANDSHAKE;
                    Handshake ();
                } else {
                    Hyena.Log.Debug ("Not connecting to Audioscrobbler", "Not connected to network.");
                }
            }
        }
        
        public void UpdateNetworkState (bool connected)
        {
            Log.DebugFormat ("Changing Audioscrobbler connected state: {0}", connected ? "connected" : "disconnected");
            this.connected = connected;
            if (connected) {
                Connect ();
            }
        }

        private void Start ()
        {
            started = true;
            queue.TrackAdded += delegate(object o, EventArgs args) {
                StartTransitionHandler ();
            };
            
            queue.Load ();
        }

        private void StartTransitionHandler ()
        {
            if (!started) {
                // Don't run if we're not actually connected.
                return;
            }
            
            if (timer == null) {
                timer = new System.Timers.Timer ();
                timer.Interval = TICK_INTERVAL;
                timer.AutoReset = true;
                timer.Elapsed += new ElapsedEventHandler (StateTransitionHandler);
                
                timer.Start ();
                //Console.WriteLine ("Timer started.");
            } else if (!timer.Enabled) {
                timer.Start ();
                //Console.WriteLine ("Restarting timer from stopped state.");
            }
        }

        public void Stop ()
        {
            StopTransitionHandler ();

            if (current_web_req != null) {
                current_web_req.Abort ();
            }

            queue.Save ();
            
            started = false;
        }

        private void StopTransitionHandler ()
        {
            if (timer != null) {
                timer.Stop ();
            }
        }

        private void StateTransitionHandler (object o, ElapsedEventArgs e)
        {
            Hyena.Log.DebugFormat ("State transition handler running; state: {0}", state);
            
            /* if we're not connected, don't bother doing anything
             * involving the network. */
            if (!connected) {
                return;
            }
                        
            if ((state == State.IDLE || state == State.NEED_TRANSMIT) && hard_failures > 2) {
                state = State.NEED_HANDSHAKE;
                hard_failures = 0;
            }

            /* and address changes in our engine state */
            switch (state) {
            case State.IDLE:
                if (account.UserName != null && account.CryptedPassword != null && session_id == null) {
                    state = State.NEED_HANDSHAKE;
                } else {
                    if (queue.Count > 0)
                        state = State.NEED_TRANSMIT;
                    else if (now_playing_submitted)
                        StopTransitionHandler ();
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
        }

        //
        // Async code for transmitting the current queue of tracks
        //
        class TransmitState
        {
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

            if (post_url == null || !connected) {
                return;
            }

            StringBuilder sb = new StringBuilder ();

            sb.AppendFormat ("s={0}", session_id);

            sb.Append (queue.GetTransmitInfo (out num_tracks_transmitted));

            current_web_req = (HttpWebRequest) WebRequest.Create (post_url);
            current_web_req.UserAgent = user_agent;
            current_web_req.Method = "POST";
            current_web_req.ContentType = "application/x-www-form-urlencoded";
            current_web_req.ContentLength = sb.Length;
            
            //Console.WriteLine ("Sending {0} ({1} bytes) to {2}", sb.ToString (), sb.Length, post_url);

            TransmitState ts = new TransmitState ();
            ts.Count = num_tracks_transmitted;
            ts.StringBuilder = sb;

            state = State.WAITING_FOR_REQ_STREAM;
            current_async_result = current_web_req.BeginGetRequestStream (TransmitGetRequestStream, ts);
            if (!(current_async_result.AsyncWaitHandle.WaitOne (TIME_OUT, false))) {
		        Hyena.Log.Warning ("Audioscrobbler upload failed", 
                                             "The request timed out and was aborted", false);
                next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
                hard_failures++;
                state = State.IDLE;
                
                current_web_req.Abort();
            }
        }

        void TransmitGetRequestStream (IAsyncResult ar)
        {
            Stream stream;

            try {
                stream = current_web_req.EndGetRequestStream (ar);
            }
            catch (Exception e) {
                Hyena.Log.Warning ("Failed to get the request stream", e.ToString (), false);

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
                hard_failures++;
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
                    Hyena.Log.Warning ("Audioscrobbler upload failed", line.Substring ("FAILED".Length).Trim(), false);
                    last_upload_failed_logged = now;
                }
                /* retransmit the queue on the next interval */
                hard_failures++;
                state = State.NEED_TRANSMIT;
            }
            else if (line.StartsWith ("BADSESSION")) {
                if (now - last_upload_failed_logged > TimeSpan.FromMinutes(FAILURE_LOG_MINUTES)) {
                    Hyena.Log.Warning ("Audioscrobbler upload failed", "session ID sent was invalid", false);
                    last_upload_failed_logged = now;
                }
                /* attempt to re-handshake (and retransmit) on the next interval */
                session_id = null;
                next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
                state = State.NEED_HANDSHAKE;
                return;
            }
            else if (line.StartsWith ("OK")) {
                /* if we've previously logged failures, be nice and log the successful upload. */
                if (last_upload_failed_logged != DateTime.MinValue) {
                    Hyena.Log.Debug ("Audioscrobbler upload succeeded");
                    last_upload_failed_logged = DateTime.MinValue;
                }
                /* we succeeded, pop the elements off our queue */
                queue.RemoveRange (0, ts.Count);
                queue.Save ();
                if (queue.Count == 0) {
                    // Don't wake up all the time - sleep for a while.
                    StopTransitionHandler ();
                }
                
                state = State.IDLE;
            }
            else {
                if (now - last_upload_failed_logged > TimeSpan.FromMinutes(FAILURE_LOG_MINUTES)) {
                    Hyena.Log.Warning ("Audioscrobbler upload failed", String.Format ("Unrecognized response: {0}", line));
                    last_upload_failed_logged = now;
                }
                state = State.IDLE;
            }
        }

        //
        // Async code for handshaking
        //
        
        private string UnixTime ()
        {
            return ((int) (DateTime.UtcNow - new DateTime (1970, 1, 1)).TotalSeconds).ToString ();
        }
        
        void Handshake ()
        {
            string timestamp = UnixTime();
            string security_token = Hyena.CryptoUtil.Md5Encode (account.CryptedPassword + timestamp);

            string uri = String.Format ("{0}?hs=true&p={1}&c={2}&v={3}&u={4}&t={5}&a={6}",
                                        SCROBBLER_URL,
                                        SCROBBLER_VERSION,
                                        CLIENT_ID, CLIENT_VERSION,
                                        HttpUtility.UrlEncode (account.UserName),
                                        timestamp,
                                        security_token);

            current_web_req = (HttpWebRequest) WebRequest.Create (uri);

            state = State.WAITING_FOR_HANDSHAKE_RESP;
            current_async_result = current_web_req.BeginGetResponse (HandshakeGetResponse, null);
            if (current_async_result == null) {
                next_interval = DateTime.Now + new TimeSpan (0, 0, hard_failure_retry_sec);
                hard_failures++;
                if (hard_failure_retry_sec < MAX_RETRY_SECONDS)
                    hard_failure_retry_sec *= 2;
                state = State.NEED_HANDSHAKE;
            }
        }

        void HandshakeGetResponse (IAsyncResult ar)
        {
            bool success = false;
            bool hard_failure = false;
            WebResponse resp;

            try {
                resp = current_web_req.EndGetResponse (ar);
            }
            catch (Exception e) {
                Hyena.Log.Warning ("Failed to handshake: {0}", e.ToString (), false);

                /* back off for a time before trying again */
                state = State.IDLE;
                next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
                return;
            }

            Stream s = resp.GetResponseStream ();

            StreamReader sr = new StreamReader (s, Encoding.UTF8);

            string line;

            line = sr.ReadLine ();
            if (line.StartsWith ("BANNED")) {
                Hyena.Log.Warning ("Audioscrobbler sign-on failed", "Player is banned", false);
                                   
            }
            else if (line.StartsWith ("BADAUTH")) {
                // FIXME: Show to user? :s
                Hyena.Log.Warning ("Audioscrobbler sign-on failed", "Unrecognized user/password");
            }
            else if (line.StartsWith ("BADTIME")) {
                Hyena.Log.Warning ("Audioscrobbler sign-on failed", 
                                                  "timestamp provided was not close enough to the current time", false);
            }
            else if (line.StartsWith ("FAILED")) {
                Hyena.Log.Warning ("Audioscrobbler sign-on failed",
                                                  String.Format ("Temporary server failure: {0}",
                                                                  line.Substring ("FAILED".Length).Trim()), false);
                hard_failure = true;
            }
            else if (line.StartsWith ("OK")) {
                success = true;
            } else {
                Hyena.Log.Error ("Audioscrobbler sign-on failed", 
                                                  String.Format ("Unknown error: {0}",
                                                                  line.Trim()), false);
                hard_failure = true;
            }
            
            if (success == true) {
                Hyena.Log.Debug ("Audioscrobbler sign-on succeeded", "Session ID received"); 
                session_id = sr.ReadLine ().Trim ();
                now_playing_url = sr.ReadLine ().Trim ();
                post_url = sr.ReadLine ().Trim ();
                
                hard_failures = 0;
                hard_failure_retry_sec = 60;
            }
            else {
                if (hard_failure == true) {
                    next_interval = DateTime.Now + new TimeSpan (0, 0, hard_failure_retry_sec);
                    hard_failures++;
                    if (hard_failure_retry_sec < MAX_RETRY_SECONDS)
                        hard_failure_retry_sec *= 2;
                }
            }

            /* XXX we shouldn't just try to handshake again for BADUSER */
            state = success ? State.IDLE : State.NEED_HANDSHAKE;
        }
        
        //
        // Async code for now playing
        
        public void NowPlaying (string artist, string title, string album, double duration,
            int tracknum)
        {
            NowPlaying (artist, title, album, duration, tracknum, "");
        }
        
        public void NowPlaying (string artist, string title, string album, double duration,
            int tracknum, string mbrainzid)

        {
            if (session_id != null && artist != "" && title != "") {
                
                string str_track_number = "";
                if (tracknum != 0)
                    str_track_number = tracknum.ToString();
                
                string uri = String.Format ("{0}?s={1}&a={2}&t={3}&b={4}&l={5}&n={6}&m={7}",
                                            now_playing_url,
                                            session_id,
                                            HttpUtility.UrlEncode(artist),
                                            HttpUtility.UrlEncode(title),
    			                            HttpUtility.UrlEncode(album),
                                            duration.ToString(),
                                            str_track_number,
    			                            mbrainzid);

                now_playing_post = (HttpWebRequest) WebRequest.Create (uri);
                now_playing_post.UserAgent = user_agent;
                now_playing_post.Method = "POST";
                now_playing_post.ContentType = "application/x-www-form-urlencoded";
                now_playing_post.ContentLength = uri.Length;
                now_playing_post.BeginGetResponse (NowPlayingGetResponse, null);
                now_playing_submitted = true;
            }
        }

        void NowPlayingGetResponse (IAsyncResult ar)
        {
            try {

                WebResponse my_resp = now_playing_post.EndGetResponse (ar);

                Stream s = my_resp.GetResponseStream ();
                StreamReader sr = new StreamReader (s, Encoding.UTF8);

                string line = sr.ReadLine ();
                if (line == null) {
                    Hyena.Log.Warning ("Audioscrobbler NowPlaying failed", "No response", false);
                }
                
                if (line.StartsWith ("BADSESSION")) {
                    Hyena.Log.Warning ("Audioscrobbler NowPlaying failed", "Session ID sent was invalid", false);
                    /* attempt to re-handshake on the next interval */
                    session_id = null;
                    next_interval = DateTime.Now + new TimeSpan (0, 0, RETRY_SECONDS);
                    state = State.NEED_HANDSHAKE;
                    return;
                }
                else if (line.StartsWith ("OK")) {
                    // NowPlaying submitted  
                    Hyena.Log.DebugFormat ("Submitted NowPlaying track to Audioscrobbler");
                }
                else {
                    Hyena.Log.Warning ("Audioscrobbler NowPlaying failed", "Unexpected or no response", false);       
                }
            }
            catch (Exception e) {
                Hyena.Log.Error ("Audioscrobbler NowPlaying failed", 
                              String.Format("Failed to post NowPlaying: {0}", e), false);
            }
        }
    }
}
