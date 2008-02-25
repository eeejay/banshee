//
// LastfmCore.cs
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2008 Alexander Hixon
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

namespace Lastfm
{
    public static class LastfmCore
    {
        private static Account account;
        public static Account Account {
            get {
                if (account == null) {
                    account = new Account ();
                }
                
                return account;
            }
        }
        
        private static string user_agent;
        public static string UserAgent {
            get { return user_agent; }
            set { user_agent = value; }
        }
        
        private static RadioConnection radio;
        public static RadioConnection Radio {
            get {
                if (radio == null) {
                    radio = new RadioConnection (LastfmCore.Account);
                }
                
                return radio;
            }
        }
        
        private static IQueue queue;
        public static IQueue AudioscrobblerQueue {
            get { return queue; }
            set { queue = value; }
        }
        
        private static AudioscrobblerConnection audioscrobbler;
        public static AudioscrobblerConnection Audioscrobbler {
            get {
                if (audioscrobbler == null) {
                    if (queue == null) {
                        throw new ApplicationException
                            ("Queue instance must be defined before referencing Audioscrobbler.");
                    }
                    
                    audioscrobbler = new AudioscrobblerConnection (LastfmCore.Account,queue);
                }
                
                return audioscrobbler;
            }
        }
    }
}
