//
// EqualizerManager.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Equalizer
{
    public class EqualizerManager : IEnumerable<EqualizerSetting>, IEnumerable
    {
        private List<EqualizerSetting> equalizers = new List<EqualizerSetting> ();
        private string path;
        
        public event EqualizerSettingEventHandler EqualizerAdded;
        public event EqualizerSettingEventHandler EqualizerRemoved;
        public event EqualizerSettingEventHandler EqualizerChanged;
        
        private static EqualizerManager instance;
        public static EqualizerManager Instance {
            get {
                if (instance == null) {
                    instance = new EqualizerManager (System.IO.Path.Combine (
                        Paths.ApplicationData, "equalizers.xml"));
                }
                
                return instance;
            }
        }
        
        public EqualizerManager ()
        {
        }
        
        public EqualizerManager (string path)
        {
            this.path = path;
            
            try {
                Load ();
            } catch {
            }
        }
    
        public void Add (EqualizerSetting eq)
        {
            eq.Changed += OnEqualizerSettingChanged;
            equalizers.Add (eq);
            QueueSave ();
            OnEqualizerAdded (eq);
        }
        
        public void Remove (EqualizerSetting eq)
        {
            Remove (eq, true);
        }
        
        private void Remove (EqualizerSetting eq, bool shouldQueueSave)
        {
            if (eq == null) {
                return;
            }
            
            eq.Changed -= OnEqualizerSettingChanged;
            equalizers.Remove (eq);
            OnEqualizerRemoved (eq);
            
            if (shouldQueueSave) {
                QueueSave ();
            }
        }
        
        public void Clear ()
        {
            while (equalizers.Count > 0) {
                Remove (equalizers[0], false);
            }
            
            QueueSave ();
        }
        
        public void Enable (EqualizerSetting eq)
        {
            if (eq != null) {
                eq.Enabled = true;
                
                // Make a copy of the Dictionary first, otherwise it'll become out of sync
                // when we begin to change the gain on the bands.
                Dictionary<uint, double> bands = new Dictionary<uint, double> (eq.Bands);
                
                foreach(KeyValuePair<uint, double> band in bands)
                {
                    eq.SetGain (band.Key, band.Value);
                }
                
                // Reset preamp.
                eq.AmplifierLevel = eq.AmplifierLevel;
            }
        }
        
        public void Disable (EqualizerSetting eq)
        {
            if (eq != null) {
                eq.Enabled = false;
            
                // Set the actual equalizer gain on all bands to 0 dB,
                // but don't change the gain in the dictionary (we can use/change those values
                // later, but not affect the actual audio stream until we're enabled again).
                
                for (uint i = 0; i < eq.BandCount; i++)
                {
                    ((IEqualizer) ServiceManager.PlayerEngine.ActiveEngine).SetEqualizerGain (i, 0);
                }
                
                ((IEqualizer) ServiceManager.PlayerEngine.ActiveEngine).AmplifierLevel = 1D;
            }
        }
        
        public void Load ()
        {
            Load (path);
        }
        
        public void Load (string path)
        {
            XmlDocument doc = new XmlDocument ();
            doc.Load (path);
            
            Clear ();
            
            foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                if(node.Name != "equalizer") {
                    throw new XmlException ("equalizer node was expected");
                }
                
                EqualizerSetting eq = new EqualizerSetting (node.Attributes["name"].Value);
                
                foreach (XmlNode child in node) {
                    if (child.Name == "preamp") {
                        eq.AmplifierLevel = Convert.ToDouble (child.InnerText);
                    } else if (child.Name == "band") {
                        eq.SetGain (Convert.ToUInt32 (child.Attributes["num"].Value),
                            Convert.ToDouble (child.InnerText));
                    } else {
                        throw new XmlException ("Invalid node, expected one of preamp or band");
                    }
                }
                
                Add (eq);
            }
        }
        
        public void Save ()
        {
            Save (path);
        }
        
        public void Save (string path)
        {
            XmlDocument doc = new XmlDocument ();
            XmlNode root = doc.CreateNode (XmlNodeType.Element, "equalizers", null);
            doc.AppendChild (root);
            
            foreach (EqualizerSetting eq in this) {
                XmlNode root_node = doc.CreateNode (XmlNodeType.Element, "equalizer", null);
            
                XmlAttribute name_node = doc.CreateAttribute ("name");
                name_node.Value = eq.Name;
                XmlNode preamp_node = doc.CreateNode (XmlNodeType.Element, "preamp", null);
                XmlNode preamp_node_value = doc.CreateNode (XmlNodeType.Text, "value", null);
                preamp_node_value.Value = Convert.ToString (eq.AmplifierLevel);
                preamp_node.AppendChild (preamp_node_value);
                
                root_node.Attributes.Append (name_node);
                root_node.AppendChild (preamp_node);

                foreach (KeyValuePair<uint, double> band in eq.Bands) {
                    XmlNode band_node = doc.CreateNode (XmlNodeType.Element, "band", null);
                    XmlNode band_node_value = doc.CreateNode (XmlNodeType.Text, "value", null);
                    band_node_value.Value = Convert.ToString (band.Value);
                    band_node.AppendChild (band_node_value);
                    
                    XmlAttribute frequency_node = doc.CreateAttribute ("num");
                    frequency_node.Value = Convert.ToString (band.Key);
                    band_node.Attributes.Append (frequency_node);
                    
                    root_node.AppendChild (band_node);
                }
                
                root.AppendChild (root_node);
            }
            
            doc.Save (path);
        }
        
        protected virtual void OnEqualizerAdded (EqualizerSetting eq)
        {
            EqualizerSettingEventHandler handler = EqualizerAdded;
            if (handler != null) {
                handler (this, new EqualizerSettingEventArgs (eq));
            }
        }
        
        protected virtual void OnEqualizerRemoved (EqualizerSetting eq)
        {
            EqualizerSettingEventHandler handler = EqualizerRemoved;
            if (handler != null) {
                handler (this, new EqualizerSettingEventArgs (eq));
            }
        }
        
        protected virtual void OnEqualizerChanged (EqualizerSetting eq)
        {
            EqualizerSettingEventHandler handler = EqualizerChanged;
            if (handler != null) {
                handler (this, new EqualizerSettingEventArgs (eq));
            }
        }
        
        private void OnEqualizerSettingChanged (object o, EventArgs args)
        {
            OnEqualizerChanged (o as EqualizerSetting);
            QueueSave ();
        }
        
        private uint queue_save_id = 0;
        private void QueueSave ()
        {
            if (queue_save_id > 0) {
                return;
            }
            
            queue_save_id = GLib.Timeout.Add (2500, delegate {
                Save ();
                queue_save_id = 0;
                return false;
            });
        }
        
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return equalizers.GetEnumerator ();
        }
        
        public IEnumerator<EqualizerSetting> GetEnumerator ()
        {
            return equalizers.GetEnumerator ();
        }
        
        public string Path {
            get { return path; }
        }
    }
}
