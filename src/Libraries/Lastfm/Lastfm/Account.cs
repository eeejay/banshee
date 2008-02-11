//
// Account.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.Collections;
using System.Text;
using System.Security.Cryptography;

namespace Lastfm
{
    public abstract class Account
    {
        private string username;
        public string Username {
            get { return username; }
            set { username = value; }
        }

        private string password; 
        public string Password {
            get { return password; }
            set { password = value; }
        }

        public string Md5Password {
            get { return password == null ? null : Md5Encode (password); }
            set { password = value; }
        }

        public void SignUp ()
        {
            //Browser.Open ("http://www.last.fm/join");
        }
        
        public void VisitUserProfile (string username)
        {
            //Browser.Open (String.Format ("http://last.fm/user/{0}", username));
        }
        
        public void VisitHomePage ()
        {
            //Browser.Open ("http://last.fm/");
        }
        
        public static string Md5Encode (string text)
        {
            if (text == null || text == String.Empty)
                return String.Empty;
                
            MD5 md5 = MD5.Create ();
            byte[] hash = md5.ComputeHash (Encoding.ASCII.GetBytes (text));

            StringBuilder shash = new StringBuilder ();
            for (int i = 0; i < hash.Length; ++i) {
                shash.Append (hash[i].ToString ("x2"));
            }

            return shash.ToString ();
        }

        public abstract void Save ();
    }
}

