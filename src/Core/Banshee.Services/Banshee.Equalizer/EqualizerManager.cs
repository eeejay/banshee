/***************************************************************************
 *  EqualizerManager.cs
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
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using Banshee.Base;

namespace Banshee.Equalizer
{
    public class EqualizerManager : IEnumerable<EqualizerSetting>, IEnumerable
    {
        private List<EqualizerSetting> equalizers = new List<EqualizerSetting>();
        private string path;
        private uint [] supported_bands = new uint [] { 30, 60, 120, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        
        public event EqualizerSettingEventHandler EqualizerAdded;
        public event EqualizerSettingEventHandler EqualizerRemoved;
        public event EqualizerSettingEventHandler EqualizerChanged;
        
        private static EqualizerManager instance;
        public static EqualizerManager Instance {
            get {
                if(instance == null) {
                    instance = new EqualizerManager(System.IO.Path.Combine(
                        Paths.ApplicationData, "equalizers.xml"));
                }
                
                return instance;
            }
        }
        
        public EqualizerManager()
        {
        }
        
        public EqualizerManager(string path)
        {
            this.path = path;
            
            try {
                Load();
            } catch {
            }
        }
    
        public void Add(EqualizerSetting eq)
        {
            eq.Changed += OnEqualizerSettingChanged;
            equalizers.Add(eq);
            QueueSave();
            OnEqualizerAdded(eq);
        }
        
        public void Remove(EqualizerSetting eq)
        {
            Remove(eq, true);
        }
        
        private void Remove(EqualizerSetting eq, bool shouldQueueSave)
        {
            if(eq == null) {
                return;
            }
            
            eq.Changed -= OnEqualizerSettingChanged;
            equalizers.Remove(eq);
            OnEqualizerRemoved(eq);
            
            if(shouldQueueSave) {
                QueueSave();
            }
        }
        
        public void Clear()
        {
            while(equalizers.Count > 0) {
                Remove(equalizers[0], false);
            }
            
            QueueSave();
        }
        
        public void Load()
        {
            Load(path);
        }
        
        public void Load(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            
            Clear();
            
            foreach(XmlNode node in doc.DocumentElement.ChildNodes) {
                if(node.Name != "equalizer") {
                    throw new XmlException("equalizer node was expected");
                }
                
                EqualizerSetting eq = new EqualizerSetting(node.Attributes["name"].Value);
                
                foreach(XmlNode child in node) {
                    if(child.Name == "preamp") {
                        eq.Preamp = Convert.ToInt32(child.InnerText);
                    } else if(child.Name == "band") {
                        eq.AddBand(Convert.ToUInt32(child.Attributes["frequency"].Value),
                            Convert.ToInt32(child.InnerText));
                    } else {
                        throw new XmlException("Invalid node, expected one of preamp or band");
                    }
                }
                
                Add(eq);
            }
        }
        
        public void Save()
        {
            Save(path);
        }
        
        public void Save(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateNode(XmlNodeType.Element, "equalizers", null);
            doc.AppendChild(root);
            
            foreach(EqualizerSetting eq in this) {
                root.AppendChild(eq.SaveXml(doc));
            }
            
            doc.Save(path);
        }
        
        protected virtual void OnEqualizerAdded(EqualizerSetting eq)
        {
            EqualizerSettingEventHandler handler = EqualizerAdded;
            if(handler != null) {
                handler(this, new EqualizerSettingEventArgs(eq));
            }
        }
        
        protected virtual void OnEqualizerRemoved(EqualizerSetting eq)
        {
            EqualizerSettingEventHandler handler = EqualizerRemoved;
            if(handler != null) {
                handler(this, new EqualizerSettingEventArgs(eq));
            }
        }
        
        protected virtual void OnEqualizerChanged(EqualizerSetting eq)
        {
            EqualizerSettingEventHandler handler = EqualizerChanged;
            if(handler != null) {
                handler(this, new EqualizerSettingEventArgs(eq));
            }
        }
        
        private void OnEqualizerSettingChanged(object o, EventArgs args)
        {
            OnEqualizerChanged(o as EqualizerSetting);
            QueueSave();
        }
        
        private uint queue_save_id = 0;
        private void QueueSave()
        {
            if(queue_save_id > 0) {
                return;
            }
            
            queue_save_id = GLib.Timeout.Add(2500, delegate {
                Save();
                queue_save_id = 0;
                return false;
            });
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return equalizers.GetEnumerator();
        }
        
        public IEnumerator<EqualizerSetting> GetEnumerator()
        {
            return equalizers.GetEnumerator();
        }
        
        public string Path {
            get { return path; }
        }
        
        public uint [] SupportedBands {
            get { return supported_bands; }
            set { supported_bands = value; }
        }
    }
}
