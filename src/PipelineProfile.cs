/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PipelineProfile.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using Mono.Posix;

namespace Banshee
{
    /* 
    A pipeline profile has the format:

    <lookup key>|<display name>|<file extension>|<gst element>|
        <bitrate property name>|<bps>|<pipeline properties>

    <display name> =          Name to show in the UI for the profile
    <file extension> =        What file extension to use for files generated 
                              by pipeline
    <gst element> =           GStreamer element name for pipeline
    <bitrate property name> = If element supports bitrate, the name of the 
                              property, otherwise leave empty or 'none'
    <bps> =                   boolean, whether the bitrate property needs bps 
                              (yes if true, no if bitrate takes kbps)
    <pipeline properties> =   Any other properties/values to be passed to 
                              <gst element>
    */
        
    public class PipelineProfileException : ApplicationException
    {
        public PipelineProfileException(string message) : base(message)
        {
        
        }
    }
        
    public class PipelineProfile
    {
        private static string [] defaultProfileDescriptions = {
            "xing|Xing MP3  |mp3 |xingenc  |bitrate|false|none",
            "lame|Lame MP3  |mp3 |lame     |bitrate|false|none",
            "ogg |Ogg Vorbis|ogg |vorbisenc|bitrate|true |none",
            "flac|Flac      |flac|flacenc  |none   |false|quality=6",
            "wave|Wave / PCM|wav |wavenc   |none   |false|none",
            "faac|Faac MP4  |mp4 |faac     |bitrate|false|none"
        };
        
        private static PipelineProfile [] loadedProfiles = null;
        
        private string key;
        private string name;
        private string extension;
        private string element;
        private string bitrateProperty;
        private bool bps;
        private string extraProperties;
        private int bitrate = 0;
        private bool useInternalBitrate;
        
        public PipelineProfile(PipelineProfile profile)
        {
            key = profile.Key;
            name = profile.Name;
            element = profile.Element;
            extension = profile.Extension;
            bitrateProperty = profile.BitrateProperty;
            bps = profile.Bps;
            extraProperties = profile.ExtraProperties;
            useInternalBitrate = true;
            Bitrate = profile.Bitrate;
        }
        
        public PipelineProfile(string profile)
        {
            useInternalBitrate = false;
            
            if(profile == null)
                throw new PipelineProfileException(Catalog.GetString(
                    "Pipeline profile is empty."));
            
            string [] components = profile.Split('|');
            
            if(components.Length != 7)
                throw new PipelineProfileException(Catalog.GetString(
                    "Pipeline profile does not have the correct " +
                    "number of components (7)"));
            
            key = components[0].Trim();   
            name = components[1].Trim();
            extension = components[2].Trim();
            element = components[3].Trim();
            
            if(key == String.Empty)
                throw new PipelineProfileException(Catalog.GetString(
                    "Pipeline profile does not have a lookup key"));
            
            if(name == String.Empty)
                throw new PipelineProfileException(Catalog.GetString(
                    "Pipeline profile does not have a display name"));
                    
            if(extension == String.Empty)
                throw new PipelineProfileException(
                    String.Format(Catalog.GetString(
                    "Pipeline profile '{0}' does not have a file extension"),
                    name));
                    
            if(element == String.Empty)
                throw new PipelineProfileException(
                    String.Format(Catalog.GetString(
                    "Pipeline profile '{0}' does not have a GStreamer element"),
                    name));

            TestPipeline(element);
                    
            bitrateProperty = components[4].Trim();
            if(bitrateProperty == String.Empty 
                || bitrateProperty.ToLower() == "none")
                bitrateProperty = null;
                
            bps = StringToBool(components[5]);
            
            extraProperties = components[6].Trim();
            if(extraProperties == String.Empty 
                || extraProperties.ToLower() == "none")
                extraProperties = null;
                
            if(extraProperties != null)
                TestPipeline(element + " " + extraProperties);
        }
        
        private bool StringToBool(string str)
        {
            string mstr = str.ToLower().Trim();
            return mstr == "yes" || mstr == "true" || mstr == "1";
        }
        
        private void TestPipeline(string pipeline)
        {
            if(!Gstreamer.TestEncoder(pipeline)) {
               throw new PipelineProfileException(String.Format(
                   Catalog.GetString("Pipeline profile '{0}' will be " +
                   "unavailable: GStreamer pipeline '{1}' could " +
                   "not be run"), name, pipeline));
            }
        }
        
        public string BuildPipeline(int bitrate)
        {
            int realBitrate = bps ? bitrate * 1000 : bitrate;
            string pipeline = element;
            
            if(bitrateProperty != null)
                pipeline += String.Format(" {0}={1}", bitrateProperty, 
                    realBitrate);
                    
            if(extraProperties != null)
                pipeline += " " + extraProperties;
                
            TestPipeline(pipeline);
                
            return pipeline;
        }
        
        public string Pipeline
        {
            get {
                if(!useInternalBitrate)
                    throw new PipelineProfileException(Catalog.GetString(
                        "Cannot use internal bitrate. Use " + 
                        "BuildPipeline(bitrate) instead."));
                        
                return BuildPipeline(Bitrate);
            }
        }

        public string Name            { get { return name;            } }
        public string Key             { get { return key;             } }
        public string Extension       { get { return extension;       } }
        public string Element         { get { return element;         } }
        public string BitrateProperty { get { return bitrateProperty; } }
        public bool Bps               { get { return bps;             } } 
        public string ExtraProperties { get { return extraProperties; } }
        
        public int Bitrate
        {
            set {
                if(!useInternalBitrate)
                    throw new PipelineProfileException(Catalog.GetString(
                        "Cannot set internal bitrate. Must copy profile and " + 
                        "set Bitrate on copy (new PipelineProfile(profile))"));
                        
                bitrate = value;
            }
            
            get {
                return bitrate;
            }
        }
        
        public static string BuildPipeline(string profileDesc, int bitrate)
        {
            PipelineProfile profile = new PipelineProfile(profileDesc);
            return profile.BuildPipeline(bitrate);
        }
        
        public static void ClearLoadedProfiles()
        {
            loadedProfiles = null;
        }
        
        public static string [] DefaultProfileDescriptions
        {
            get {
                return defaultProfileDescriptions;
            }
        }
        
        public static string [] ProfileDescriptions
        {
            get {
                string [] descriptions = null;

                GConf.Client gc = Core.IsInstantiated
                    ? Core.GconfClient
                    : new GConf.Client();

                try {
                    descriptions = gc.Get(GConfKeys.EncoderProfiles) 
                        as string [];
                } catch(Exception) { }
                
                if(descriptions == null || descriptions.Length == 0) {
                    descriptions = DefaultProfileDescriptions;
                    gc.Set(GConfKeys.EncoderProfiles, descriptions);
                }

                return descriptions;
            }
        }
        
        public static PipelineProfile [] Profiles
        {
            get {
                if(loadedProfiles != null)
                    return loadedProfiles;
                    
                string [] descriptions = ProfileDescriptions;
                ArrayList list = new ArrayList();
                
                for(int i = 0; i < descriptions.Length; i++) {
                    try {
                        list.Add(new PipelineProfile(descriptions[i]));
                    } catch(PipelineProfileException e) {
                        DebugLog.Add(e.Message);
                    }
                }
                
                if(list.Count == 0)
                    return null;
                
                loadedProfiles = list.ToArray(typeof(PipelineProfile)) 
                    as PipelineProfile [];
                return loadedProfiles;
            }
        }
        
        public static PipelineProfile GetConfiguredProfile(string gckey, 
            string extFilter)
        {
            string key = "default";
            int bitrate = 160;
            PipelineProfile profile;

            GConf.Client gc = Core.IsInstantiated
                ? Core.GconfClient
                : new GConf.Client();

            try {
                key = gc.Get(GConfKeys.BasePath + gckey + "Profile") as string;
            } catch(Exception) {}

            try {
                bitrate = (int)gc.Get(GConfKeys.BasePath + gckey + "Bitrate");
            } catch(Exception) {}

            if(Profiles == null || Profiles.Length == 0)
                return null;

            foreach(PipelineProfile cProfile in Profiles) {
                if(cProfile.Key == key) {
                    profile = new PipelineProfile(cProfile);
                    profile.Bitrate = bitrate;
                    return profile;
                }
            }

            if(extFilter != null) {
                string [] filters = extFilter.Split(',');
                
                foreach(string filter in filters) {
                    foreach(PipelineProfile cProfile in Profiles) {
                        if(cProfile.Extension.ToLower() == 
                            filter.Trim().ToLower()) {
                            profile = new PipelineProfile(cProfile);
                            profile.Bitrate = bitrate;
                            return profile;
                        }
                    }
                }
            }

            profile = new PipelineProfile(Profiles[0]);
            profile.Bitrate = bitrate;
            return profile;   
        }
        
        public static PipelineProfile GetConfiguredProfile(string gckey)
        {
            return GetConfiguredProfile(gckey, null);
        }
    }
    
    public class PipelineProfileSelector : HBox
    {
        private ComboBox profileCombo;
        private ComboBox bitrateCombo;
        private Label atLabel;
    
        private PipelineProfile [] profiles;
    
        private int [] bitrates = {
            320,
            192,
            160,
            128,
            96,
            48
        };
       
        public PipelineProfileSelector(string extFilter)
        {
            profileCombo = ComboBox.NewText();
            bitrateCombo = ComboBox.NewText();
            atLabel = new Label(Catalog.GetString("at"));

            profiles = PipelineProfile.Profiles;

            foreach(int bitrate in bitrates)
                bitrateCombo.AppendText(String.Format("{0} Kbps", bitrate));
            
            if(extFilter != null && profiles != null) {
                string [] filters = extFilter.Split(',');
                ArrayList filteredProfiles = new ArrayList();
                
                foreach(string filter in filters) {
                    foreach(PipelineProfile profile in profiles) {
                        if(profile.Extension.ToLower() == 
                            filter.Trim().ToLower())
                            filteredProfiles.Add(profile);
                    }
                }
                
                if(filteredProfiles.Count == 0)
                    profiles = null;
                else
                    profiles = filteredProfiles.ToArray(typeof(PipelineProfile)) 
                        as PipelineProfile [];
            }

            if(profiles != null) {
                foreach(PipelineProfile profile in profiles)
                    profileCombo.AppendText(profile.Name);
            }
            
            profileCombo.Changed += OnProfileChanged;
            bitrateCombo.Changed += OnBitrateChanged;
             
            if(profiles != null) {   
                Spacing = 10;
                PackStart(profileCombo, false, false, 0);
                PackStart(atLabel, false, false, 0);
                PackStart(bitrateCombo, false, false, 0);
            } else {
                Label label = new Label();
                label.Markup = "<i><small>" + 
                    Catalog.GetString("No iPod-compatible encoders available") +
                    "</small></i>";
                PackStart(label, false, false, 0);
                label.Show();
            }
            
            profileCombo.Show(); 
            ActiveProfileIndex = 0;
            Bitrate = -1;
        }
        
        public PipelineProfileSelector() : this(null)
        {
                        
        }
        
        private void OnProfileChanged(object o, EventArgs args)
        {
            SetBitrateVisibility();
        }
        
        private void OnBitrateChanged(object o, EventArgs args)
        {
        
        }
        
        private void SetBitrateVisibility()
        {
            bitrateCombo.Visible = profiles[ActiveProfileIndex].BitrateProperty 
                != null;
            atLabel.Visible = bitrateCombo.Visible;
        }
        
        private int ActiveProfileIndex 
        {
            set {
                if(profiles == null)
                    return;
                    
                profileCombo.Active = value;
                SetBitrateVisibility();
            }
            
            get {
                if(profiles == null)
                    return -1;
                    
                return profileCombo.Active;
            }
        }
        
        private int ActiveBitrateIndex
        {
            set {
                if(profiles == null)
                    return;
                    
                bitrateCombo.Active = value;
            }
            
            get {
                if(profiles == null)
                    return -1;
                    
                return bitrateCombo.Active;
            }
        } 
        
        public string ProfileKey
        {
            get {
                if(profiles == null)
                    return null;
                    
                return profiles[ActiveProfileIndex].Key;
            }
            
            set {
                if(profiles == null)
                    return;
            
                for(int i = 0; i < profiles.Length; i++) {
                    if(profiles[i].Key == value) {
                        ActiveProfileIndex = i;
                        return;
                    }
                }
                
                ActiveProfileIndex = 0;
            }
        }
        
        public int Bitrate
        {
            get {
                if(profiles == null)
                    return 0;
                    
                return bitrates[ActiveBitrateIndex];
            }
            
            set {
                if(profiles == null)
                    return;
                    
                for(int i = 0; i < bitrates.Length; i++) {
                    if(bitrates[i] == value) {
                        ActiveBitrateIndex = i;
                        return;
                    }
                }
                
                ActiveBitrateIndex = (bitrates.Length / 2) - 1;
            }
        }           
    }
}
