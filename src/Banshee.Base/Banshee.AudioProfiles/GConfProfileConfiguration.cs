/***************************************************************************
 *  GConfProfileConfiguration.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using GConf;

namespace Banshee.AudioProfiles
{
    public class GConfProfileConfiguration : ProfileConfiguration
    {
        private string gconf_root;
        private GConf.Client client;
        
        public GConfProfileConfiguration(Profile profile, string gconfRoot, string id) : base(profile, id)
        {
            client = new GConf.Client();
            this.gconf_root = gconfRoot;
        }
        
        internal static string LoadActiveProfile(string gconfRoot, string id)
        {
            GConf.Client client = new GConf.Client();
            return (string)client.Get(gconfRoot + id + "/active_profile");
        }
        
        internal static void SaveActiveProfile(Profile profile, string gconfRoot, string id)
        {
            GConf.Client client = new GConf.Client();
            client.Set(gconfRoot + id + "/active_profile", profile.ID);
        }

        protected override void Load()
        {
            try {
                foreach(string variable in (string [])client.Get(gconf_root + "variables")) {
                    Add(variable, (string)client.Get(gconf_root + variable));
                }
            } catch {
            }
        }
        
        public override void Save()
        {
            List<string> variable_names = new List<string>();
            foreach(KeyValuePair<string, string> variable in this) {
                variable_names.Add(variable.Key);
                client.Set(gconf_root + variable.Key, variable.Value);
            }
            client.Set(gconf_root + "variables", variable_names.ToArray());
        }
    }
}
