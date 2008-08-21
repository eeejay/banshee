//
// EqualizerSetting.cs
//
// Author:
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;

using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Configuration;

namespace Banshee.Equalizer
{
    public class EqualizerSetting
    {
        private string name;
        private Dictionary<uint, double> bands = new Dictionary<uint, double> ();
        private double amp = 0;    // amplifier dB (0 dB == passthrough)
        private bool enabled = true;
        private const uint bandcount = 10;
        
        public event EventHandler Changed;
        
        public EqualizerSetting (string name)
        {
            this.name = name;
            
            // Fill in 0 dB for all bands at init.
            for (uint i = 0; i < bandcount; i++) {
                bands.Add(i, 0);
            }
        }
        
        /// <summary>
        /// Human-readable name of this equalizer instance.
        /// </summary>
        public string Name {
            get { return name; }
            set
            {
                name = value;
                PresetSchema.Set (this.name);
                OnChanged ();
            }
        }
        
        public bool Enabled {
            get { return enabled; }
            set
            {
                enabled = value;
                EnabledSchema.Set (value);
                
                // Make this the new default preset (last changed).
                PresetSchema.Set (this.name);
            }
        }
        
        public Dictionary<uint, double> Bands {
            get { return bands; }
        }
        
        public uint BandCount {
            get { return bandcount; }
        }
        
        /// <summary>
        /// Sets/gets the preamp gain.
        /// </summary>
        public double AmplifierLevel {
            get { return amp; }
            set
            {
                amp = value;
                if (enabled) {
                    ((IEqualizer) ServiceManager.PlayerEngine.ActiveEngine).AmplifierLevel = value;
                }
                OnChanged ();
            }
        }

        /// <summary>
        /// Sets the gain on a selected band.
        /// </summary>
        /// <param name="band">The band to adjust gain.</param>
        /// <param name="val">The value in dB to set the band gain to.</param>
        public void SetGain (uint band, double val)
        {
            if (bands.ContainsKey (band)) {
                bands[band] = val;
            } else {
                bands.Add (band, val);
            }
            
            // Tell engine that we've changed.
            if (enabled) {
                ((IEqualizer) ServiceManager.PlayerEngine.ActiveEngine).SetEqualizerGain (band, val);
            }
            
            OnChanged ();
        }
        
        public double this[uint band] {
            get { return bands[band]; }
            set { SetGain (band, value); }
        }
        
        protected virtual void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "player_engine", "equalizer_enabled",
            false,
            "Equalizer status",
            "Whether or not the equalizer is set to be enabled."
        );
        
        public static readonly SchemaEntry<string> PresetSchema = new SchemaEntry<string> (
            "player_engine", "equalizer_preset",
            "",
            "Equalizer preset",
            "Default preset to load into equalizer."
        );
    }
}
