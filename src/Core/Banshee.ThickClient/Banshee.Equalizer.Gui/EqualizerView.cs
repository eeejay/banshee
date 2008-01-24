//
// EqualizerView.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Ivan N. Zlatev <contact i-nZ.net>
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
using System.Collections;

using Gtk;
using Gdk;

using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Equalizer.Gui
{
    public class EqualizerView : HBox
    {
        private HBox band_box;
        private uint [] frequencies;
        private EqualizerBandScale [] band_scales;
        private EqualizerBandScale amplifier_scale;
        private EqualizerSetting active_eq;
        private int [] range = new int[3];
        
        public event EqualizerChangedEventHandler EqualizerChanged;
        public event AmplifierChangedEventHandler AmplifierChanged;
        
        public EqualizerView() : base()
        {
            BuildWidget();
        }
        
        private void BuildWidget()
        {
            Spacing = 10;
        
            // amplifier_scale's values must be divisible by 100 to get *real* val. eg 100 == 1, 40 == 0.4, 1 == 0.01
            amplifier_scale = new EqualizerBandScale(0, 100, 5, 150, "Preamp");
            amplifier_scale.ValueChanged += OnAmplifierValueChanged;
            amplifier_scale.Show();
            PackStart(amplifier_scale, false, false, 0);
            
            int[] br = ((IEqualizer)ServiceManager.PlayerEngine.ActiveEngine).BandRange;
            int mid = (br[0] + br[1]) / 2;
            
            range[0] = br[0];
            range[1] = mid;
            range[2] = br[1];
            
            EqualizerLevelsBox eq_levels = new EqualizerLevelsBox(
                FormatDecibelString(range[2]),
                FormatDecibelString(range[1]),
                FormatDecibelString(range[0])
            );
            
            eq_levels.Show();
            PackStart(eq_levels, false, false, 0);
            
            band_box = new HBox();
            band_box.Homogeneous = true;
            band_box.Show();
            PackStart(band_box, true, true, 0);
            
            BuildBands();
        }
        
        /// <summary>
        /// Utility function to create a dB string for the levels box.
        /// </summary>
        private string FormatDecibelString(int db)
        {
            if (db > 0) { return String.Format("+{0} dB", db); } else { return String.Format("{0} dB", db); }
        }
        
        private void BuildBands()
        {
            foreach(Widget widget in band_box.Children) {
                band_box.Remove(widget);
            }
            
            if(frequencies == null || frequencies.Length <= 0) {
                frequencies = new uint[0];
                band_scales = new EqualizerBandScale[0];
                return;
            }
            
            band_scales = new EqualizerBandScale[10];
            
            for(uint i = 0; i < 10; i++) {
                string label = frequencies[i] < 1000 ? 
                    String.Format("{0} Hz", frequencies[i]) :
                    String.Format("{0} kHz", frequencies[i] / 1000);
                    
                //band_scales[i] = new EqualizerBandScale(bands[i], label);
                band_scales[i] = new EqualizerBandScale(i, range[1] * 10, range[0] * 10, range[2] * 10, label);
                band_scales[i].ValueChanged += OnEqualizerValueChanged;
                band_scales[i].Show();
                
                band_box.PackStart(band_scales[i], true, true, 0);
            }
        }

        private void OnEqualizerValueChanged(object o, EventArgs args)
        {
            EqualizerBandScale scale = o as EqualizerBandScale;
            
            if(active_eq != null) {
                active_eq.SetGain(scale.Band, (double)scale.Value / 10D);
            }
            
            if(EqualizerChanged != null) {
                EqualizerChanged(this, new EqualizerChangedEventArgs(scale.Band, scale.Value));
            }
        }

        private void OnAmplifierValueChanged(object o, EventArgs args)
        {
            EqualizerBandScale scale = o as EqualizerBandScale;
            if(active_eq != null) {
                double val = (double)scale.Value / 100D;
                active_eq.AmplifierLevel = val;
            }
            
            if(AmplifierChanged != null) {
                AmplifierChanged(this, new AmplifierChangedEventArgs(scale.Value));
            }
        }    
            
        public uint [] Frequencies {
            get { return (uint [])frequencies.Clone(); }
            set { 
                frequencies = (uint [])value.Clone();
                BuildBands();
            }
        }
        
        public int [] Preset {
            get {
                int [] result = new int[band_scales.Length];
                for(int i = 0; i < band_scales.Length; i++) {
                    result[i] = (int)band_scales[i].Value;
                }
                
                return result;
            }
            
            set {
                for(int i = 0; i < value.Length; i++) {
                    band_scales[i].Value = value[i];                   
                }
            }
        }
        
        public void SetBand(uint band, double value)
        {
            band_scales[band].Value = (int)(value * 10);
        }

        public double AmplifierLevel {
            get { return (double)amplifier_scale.Value / 100D; }
            set { amplifier_scale.Value = (int)(value * 100); }
        }
        
        public EqualizerSetting EqualizerSetting {
            get { return active_eq; }
            set { 
                active_eq = value; 
                
                if(active_eq == null) {
                    AmplifierLevel = 0;
                    
                    return;
                }
                
                AmplifierLevel = active_eq.AmplifierLevel;
                
                for(int i = 0; i < active_eq.BandCount; i++) {
                    uint x = (uint)i;
                    SetBand(x, active_eq[x]);
                }
            }
        }
    }

    public delegate void EqualizerChangedEventHandler(object o, EqualizerChangedEventArgs args);
    public delegate void AmplifierChangedEventHandler(object o, AmplifierChangedEventArgs args);
    
    public sealed class EqualizerChangedEventArgs : EventArgs
    {
        private uint band;
        private int value;

        public EqualizerChangedEventArgs(uint band, int value)
        {
            this.band = band;
            this.value = value;
        }

        public uint Band {
            get { return this.band; }
        }

        public int Value {
            get { return this.value; }
        }
    }

    public sealed class AmplifierChangedEventArgs : EventArgs
    {
        private int value;

        public AmplifierChangedEventArgs(int value)
        {
            this.value = value;
        }

        public int Value {
            get { return this.value; }
        }
    }
}
