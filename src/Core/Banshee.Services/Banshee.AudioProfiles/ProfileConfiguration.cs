/***************************************************************************
 *  ProfileConfiguration.cs
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

namespace Banshee.AudioProfiles
{
    public abstract class ProfileConfiguration : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> variable_values = new Dictionary<string, string>();
        private string id;
        private Profile profile;
        
        public static ProfileConfiguration Load(Profile profile, string id)
        {
            ProfileConfiguration configuration = new GConfProfileConfiguration(profile,
                Banshee.Configuration.GConfConfigurationClient.BaseKey + 
                    "audio_profiles/" + id + "/" + profile.ID + "/", id);
            configuration.Load();
            return configuration;
        }
        
        public static Profile LoadActiveProfile(ProfileManager manager, string id)
        {
            try {
                string profile_id = GConfProfileConfiguration.LoadActiveProfile(
                    Banshee.Configuration.GConfConfigurationClient.BaseKey + "audio_profiles/", id);
                
                if(profile_id == null) {
                    return null;
                }
            
                foreach(Profile profile in manager.GetAvailableProfiles()) {
                    if(profile.ID == profile_id) {
                        return profile;
                    }
                }
            } catch {
            }
            
            return null;
        }
        
        public static void SaveActiveProfile(Profile profile, string id)
        {
            GConfProfileConfiguration.SaveActiveProfile(profile,
                Banshee.Configuration.GConfConfigurationClient.BaseKey + "audio_profiles/", id);
        }
        
        public ProfileConfiguration(Profile profile, string id)
        {
            this.profile = profile;
            this.id = id;
        }

        protected abstract void Load();
        public abstract void Save();
        
        public void Add(string variable, string value)
        {
            if(variable_values.ContainsKey(variable)) {
                variable_values[variable] = value;
            } else {
                variable_values.Add(variable, value);
            }
        }
        
        public void Remove(string variable)
        {
            if(variable_values.ContainsKey(variable)) {
                variable_values.Remove(variable);
            }
        }
        
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return variable_values.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return variable_values.GetEnumerator();
        }
        
        public string this[string variable] {
            get { return variable_values[variable]; }
        }
        
        public string ID {
            get { return id; }
        }
    }
}
