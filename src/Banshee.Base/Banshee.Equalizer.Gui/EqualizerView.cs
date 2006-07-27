/***************************************************************************
 *  Equalizer.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 *             Ivan N. Zlatev <contact i-nZ.net>
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
using System.Collections;

using Gtk;
using Gdk;

namespace Banshee.Equalizer.Gui
{
    public class EqualizerView : HBox
    {
        private HBox band_box;
        private uint [] bands;
        private EqualizerBandScale [] band_scales;
        private EqualizerBandScale amplifier_scale;
        private EqualizerSetting active_eq;
        
        public event EqualizerChangedEventHandler EqualizerChanged;
        public event AmplifierChangedEventHandler AmplifierChanged;
        
        public EqualizerView() : base()
        {
            BuildWidget();
        }
        
        private void BuildWidget()
        {
            Spacing = 10;
        
            amplifier_scale = new EqualizerBandScale(0, "Preamp");
            amplifier_scale.ValueChanged += OnAmplifierValueChanged;
            amplifier_scale.Show();
            PackStart(amplifier_scale, false, false, 0);
            
            EqualizerLevelsBox eq_levels = new EqualizerLevelsBox("-20 dB", "0 dB", "+20 dB");
            eq_levels.Show();
            PackStart(eq_levels, false, false, 0);
            
            band_box = new HBox();
            band_box.Homogeneous = true;
            band_box.Show();
            PackStart(band_box, true, true, 0);
            
            BuildBands();
        }
        
        private void BuildBands()
        {
            foreach(Widget widget in band_box.Children) {
                band_box.Remove(widget);
            }
            
            if(bands == null || bands.Length <= 0) {
                bands = new uint[0];
                band_scales = new EqualizerBandScale[0];
                return;
            }

            band_scales = new EqualizerBandScale[bands.Length];
            
            for(uint i = 0; i < bands.Length; i++) {
                string label = bands[i] < 1000 ? 
                    String.Format("{0} Hz", bands[i]) :
                    String.Format("{0} kHz", bands[i] / 1000);
                    
                band_scales[i] = new EqualizerBandScale(bands[i], label);
                band_scales[i].ValueChanged += OnEqualizerValueChanged;
                band_scales[i].Show();
                
                band_box.PackStart(band_scales[i], true, true, 0);
            }
        }

        private void OnEqualizerValueChanged(object o, EventArgs args)
        {
            EqualizerBandScale scale = o as EqualizerBandScale;
            
            if(scale.Value >= -13 && scale.Value <= 13) {
                scale.Value = 0;
                return;
            } else if(active_eq != null) {
                active_eq.SetBand(scale.Frequency, scale.Value);
            }
            
            if(EqualizerChanged != null) {
                EqualizerChanged(this, new EqualizerChangedEventArgs(scale.Frequency, scale.Value));
            }
        }

        private void OnAmplifierValueChanged(object o, EventArgs args)
        {
            EqualizerBandScale scale = o as EqualizerBandScale;
            if(scale.Value >= -13 && scale.Value <= 13) {
                scale.Value = 0;
                return;
            } else if(active_eq != null) {
                active_eq.Preamp = scale.Value;
            }
            
            if(AmplifierChanged != null) {
                AmplifierChanged(this, new AmplifierChangedEventArgs(scale.Value));
            }
        }    
            
        public uint [] Bands {
            get { return (uint [])bands.Clone(); }
            set { 
                bands = (uint [])value.Clone();
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
                if(value.Length == bands.Length) {                
                    for(int i = 0; i < value.Length; i++) {
                        if(value[i] < -100 || value[i] > 100) {
                            value[i] = 0;
                        }
                        
                        band_scales[i].Value = value[i];                   
                    }
                }
            }
        }
        
        public void SetBand(uint band, int value)
        {
            if(value < -100 || value > 100) {
                value = 0;
            }
        
            for(int i = 0; i < bands.Length; i++) {
                if(bands[i] == band) {
                    band_scales[i].Value = value;
                    return;
                }
            }
        }

        public int AmplifierLevel {
            get { return (int)amplifier_scale.Value; }
            set {
                if(value < -100 || value > 100) {
                    value = 0;
                }
                
                amplifier_scale.Value = value;
            }
        }
        
        public EqualizerSetting EqualizerSetting {
            get { return active_eq; }
            set { 
                active_eq = value; 
                
                if(active_eq == null) {
                    AmplifierLevel = 0;
                    
                    for(int i = 0; i < bands.Length; i++) {
                        SetBand(bands[i], 0);
                    }
                    
                    return;
                }
                
                AmplifierLevel = active_eq.Preamp;
                
                for(int i = 0; i < bands.Length; i++) {
                    SetBand(bands[i], 0);
                    
                    try {
                        SetBand(bands[i], active_eq[bands[i]]);
                    } catch {
                    }
                }
            }
        }
    }

    public delegate void EqualizerChangedEventHandler(object o, EqualizerChangedEventArgs args);
    public delegate void AmplifierChangedEventHandler(object o, AmplifierChangedEventArgs args);
    
    public sealed class EqualizerChangedEventArgs : EventArgs
    {
        private uint freq;
        private int value;

        public EqualizerChangedEventArgs(uint frequency, int value)
        {
            this.freq = frequency;
            this.value = value;
        }

        public uint Frequency {
            get { return this.freq; }
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