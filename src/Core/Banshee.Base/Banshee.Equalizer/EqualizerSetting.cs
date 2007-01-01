/***************************************************************************
 *  EqualizerSetting.cs
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
using System.Xml;
using System.Collections.Generic;

namespace Banshee.Equalizer
{
    public class EqualizerSetting
    {
        private string name;
        private int preamp = 0;
        private Dictionary<uint, int> bands = new Dictionary<uint, int>();
        
        public event EventHandler Changed;
        
        public EqualizerSetting()
        {
        }
        
        public EqualizerSetting(string name)
        {
            this.name = name;
        }
        
        public void AddBand(uint band, int value)
        {
            if(bands.ContainsKey(band)) {
                bands[band] = value;
            } else {
                bands.Add(band, value);
            }
            
            OnChanged();
        }
        
        public void SetBand(uint band, int value)
        {
            AddBand(band, value);
        }
        
        public void RemoveBand(uint band)
        {
            if(bands.ContainsKey(band)) {
                bands.Remove(band);
            }
            
            OnChanged();
        }
        
        public XmlNode SaveXml(XmlDocument document)
        {
            XmlNode root_node = document.CreateNode(XmlNodeType.Element, "equalizer", null);
            
            XmlAttribute name_node = document.CreateAttribute("name");
            name_node.Value = name;
            
            XmlNode preamp_node = document.CreateNode(XmlNodeType.Element, "preamp", null);
            XmlNode preamp_node_value = document.CreateNode(XmlNodeType.Text, "value", null);
            preamp_node_value.Value = Convert.ToString(preamp);
            preamp_node.AppendChild(preamp_node_value);
            
            root_node.Attributes.Append(name_node);
            root_node.AppendChild(preamp_node);
            
            foreach(KeyValuePair<uint, int> band in bands) {
                XmlNode band_node = document.CreateNode(XmlNodeType.Element, "band", null);
                XmlNode band_node_value = document.CreateNode(XmlNodeType.Text, "value", null);
                band_node_value.Value = Convert.ToString(band.Value);
                band_node.AppendChild(band_node_value);
                
                XmlAttribute frequency_node = document.CreateAttribute("frequency");
                frequency_node.Value = Convert.ToString(band.Key);
                band_node.Attributes.Append(frequency_node);
                
                root_node.AppendChild(band_node);
            }
            
            return root_node;
        }
        
        protected virtual void OnChanged()
        {
            EventHandler handler = Changed;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public int this[uint band] {
            get { return bands[band]; }
            set { SetBand(band, value); }
        
        }
        
        public string Name {
            get { return name; }
            set { 
                name = value; 
                OnChanged();
            }
        }
        
        public int Preamp {
            get { return preamp; }
            set { 
                preamp = value; 
                OnChanged();
            }
        }

        public uint [] Bands {
            get { 
                uint [] bands_array = new uint[bands.Count];
                bands.Keys.CopyTo(bands_array, 0);
                return bands_array;
            }
        }
        
        public int [] Values {
            get { 
                int [] values_array = new int[bands.Count];
                bands.Values.CopyTo(values_array, 0);
                return values_array;
            }
        }
    }
    
    
    public delegate void EqualizerSettingEventHandler(object o, EqualizerSettingEventArgs args);
    
    public class EqualizerSettingEventArgs : EventArgs
    {
        private EqualizerSetting eq;
        
        public EqualizerSettingEventArgs(EqualizerSetting eq)
        {
            this.eq = eq;
        }
        
        public EqualizerSetting EqualizerSetting {
            get { return eq; }
        }
    }
}
