//
// ProfileConfiguration.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using Banshee.Configuration;

namespace Banshee.MediaProfiles
{
    public class ProfileConfiguration : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> variable_values = new Dictionary<string, string>();
        private string id;
        private Profile profile;

        public static ProfileConfiguration Load(Profile profile, string id)
        {
            ProfileConfiguration configuration = new ProfileConfiguration(profile, id);
            configuration.Load();
            return configuration;
        }

        public static ProfileConfiguration LoadActive (MediaProfileManager manager, string id)
        {
            string profile_id = ConfigurationClient.Get<string>(MakeConfNamespace(id), "active_profile", string.Empty);

            if(profile_id == string.Empty) {
                return null;
            }

            foreach(Profile profile in manager.GetAvailableProfiles()) {
                if(profile.Id == profile_id) {
                    profile.LoadConfiguration (id);
                    return profile.Configuration;
                }
            }

            return null;
        }

        public static void SaveActiveProfile(Profile profile, string id)
        {
            ConfigurationClient.Set<string>(MakeConfNamespace(id), "active_profile", profile.Id);
        }

        public ProfileConfiguration(Profile profile, string id)
        {
            this.profile = profile;
            this.id = id;
        }

        protected virtual void Load()
        {
            foreach(string variable in ConfigurationClient.Get<string[]>(ConfNamespace, "variables", new string[0])) {
                Add(variable, ConfigurationClient.Get<string>(ConfNamespace, variable, string.Empty));
            }
        }

        public virtual void Save()
        {
            List<string> variable_names = new List<string>(Count);
            foreach(KeyValuePair<string, string> variable in this) {
                variable_names.Add(variable.Key);
                ConfigurationClient.Set<string>(ConfNamespace, variable.Key, variable.Value);
            }
            ConfigurationClient.Set<string[]>(ConfNamespace, "variables", variable_names.ToArray());
        }

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

        public string Id {
            get { return id; }
        }

        public int Count {
            get { return variable_values.Count; }
        }

        public Profile Profile {
            get {
                if (profile.Configuration != this)
                    profile.SetConfiguration (this);
                return profile;
            }
        }

        protected string ConfNamespace {
            get { return MakeConfNamespace(id); }
        }

        protected static string MakeConfNamespace(string id)
        {
            return String.Format("audio_profiles.{0}", id);
        }
    }
}