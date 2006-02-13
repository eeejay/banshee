/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DapMisc.cs
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
using Banshee.Base;

namespace Banshee.Dap
{
    public enum DapType {
        Generic,
        NonGeneric
    }
    
    public sealed class CodecType
    {
        public const string Mp3 = "mp3";
        public const string Mp4 = "mp4";
        public const string Wma = "wma";
        public const string Ogg = "ogg";
        
        public static string [] GetExtensions(string codec)
        {
            switch(codec) {
                case Mp3:
                    return new string [] { "mp3" };
                case Mp4:
                    return new string [] { "mp4", "m4a", "m4p", "aac" };
                case Wma:
                    return new string [] { "wma", "asf" };
                case Ogg:
                    return new string [] { "ogg" };
             }
             
             return null;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DapProperties : Attribute 
    {
        private DapType dap_type;
        private string pipeline_name;
        
        public DapType DapType {
            get {
                return dap_type;
            }
            
            set {
                dap_type = value;
            }
        }
        
        public string PipelineName {
            get {
                return pipeline_name;
            }
            
            set {
                pipeline_name = value;
            }
        }
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedCodec : Attribute 
    {
        private string codec_type;
        
        public SupportedCodec(string codecType)
        {
            CodecType = codecType;
        }
        
        public string CodecType {
            get {
                return codec_type;
            }
            
            set {
                codec_type = value;
            }
        }
    }
    
    public enum InitializeResult {
        Valid,
        Invalid,
        WaitForPropertyChange
    }

    public delegate void DapTrackListUpdatedHandler(object o, DapTrackListUpdatedArgs args);

    public class DapTrackListUpdatedArgs : EventArgs
    {
        public TrackInfo Track;

        public DapTrackListUpdatedArgs(TrackInfo track)
        {
            Track = track;
        }
    }
}
