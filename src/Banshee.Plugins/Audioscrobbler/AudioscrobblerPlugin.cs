/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AudioscrobblerPlugin.cs
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

using GConf;

using Banshee.MediaEngine;
using Banshee.Base;

namespace Banshee.Plugins.Audioscrobbler 
{
    public class AudioscrobblerPlugin : Banshee.Plugins.Plugin
    {
        class GConfKeys {
            public const string BasePath = Banshee.Base.GConfKeys.BasePath + "Audioscrobbler";
            public const string Enabled = BasePath + "/Enabled";
            public const string Username = BasePath + "/Username";
            public const string Password = BasePath + "/Password";
        }

        Engine protocol_engine;
        GConf.Client gconf;
        IPlayerEngine player;

        public AudioscrobblerPlugin ()
        {
            gconf = Globals.Configuration;
            gconf.AddNotify (GConfKeys.BasePath, GConfNotifier);

            if (GetBoolPref (GConfKeys.Enabled, false)) {
                StartEngine ();
                SetPlayer ();
            }
        }

        void StartEngine ()
        {
            Console.WriteLine ("Audioscrobbler starting protocol engine");
            protocol_engine = new Engine ();
            protocol_engine.SetUserPassword (GetStringPref (GConfKeys.Username, null),
                             GetStringPref (GConfKeys.Password, null));
            if (player != null)
                protocol_engine.SetPlayer (player);
        }
        
        public override void Dispose()
        {
            StopEngine();
        }

        void StopEngine ()
        {
            if(protocol_engine != null) {
                Console.WriteLine ("Audioscrobbler stopping protocol engine");
                protocol_engine.Stop ();
                protocol_engine = null;
            }
        }

        public void SetPlayer ()
        {
            this.player = PlayerEngineCore.ActivePlayer;
            if (protocol_engine != null)
                protocol_engine.SetPlayer (player);
        }

        void GConfNotifier (object sender, NotifyEventArgs args)
        {
            Console.WriteLine ("key that changed: {0}", args.Key);
            if (args.Key == GConfKeys.Enabled) {
                if ((bool)args.Value == false)
                    StopEngine ();
                else
                    StartEngine ();
            }
            else if (args.Key == GConfKeys.Username
                 || args.Key == GConfKeys.Password)
            {
                if (protocol_engine != null) {
                    protocol_engine.SetUserPassword (GetStringPref (GConfKeys.Username, null),
                                     GetStringPref (GConfKeys.Password, null));
                }
            }
        }

        string GetStringPref (string key, string def)
        {
            try {
                return (string) gconf.Get(key);
            }
            catch {
                return def;
            }
        }

        bool GetBoolPref (string key, bool def)
        {
            try {
                return (bool) gconf.Get(key);
            }
            catch {
                return def;
            }
        }
    }
}
