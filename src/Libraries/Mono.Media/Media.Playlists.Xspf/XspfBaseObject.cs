//
// XspfBaseObject.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
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
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Media.Playlists.Xspf
{
    public abstract class XspfBaseObject
    {
        private string title;
        private string creator;
        private string annotation;        
        
        private Uri info;
        private Uri image;
        
        private List<LinkEntry> links;
        private List<MetaEntry> meta;

        public abstract Uri ResolvedBaseUri {
            get;
        }
        
        protected void LoadBase(XmlNode parentNode, XmlNamespaceManager xmlns)
        { 
            title = XmlUtil.ReadString(parentNode, xmlns, "xspf:title");
            creator = XmlUtil.ReadString(parentNode, xmlns, "xspf:creator");
            annotation = XmlUtil.ReadString(parentNode, xmlns, "xspf:annotation");
            
            info = XmlUtil.ReadUri(parentNode, xmlns, ResolvedBaseUri, "xspf:info");
            image = XmlUtil.ReadUri(parentNode, xmlns, ResolvedBaseUri, "xspf:image");
            
            meta = XmlUtil.ReadMeta(parentNode, xmlns, ResolvedBaseUri, "xspf:meta");
            links = XmlUtil.ReadLinks(parentNode, xmlns, ResolvedBaseUri, "xspf:link");
        }
        
        protected void SaveBase(XmlWriter writer)
        {
            if(title != null) {
                writer.WriteElementString("title", title);
            }
            
            if(creator != null) {
                writer.WriteElementString("creator", creator);
            }
            
            if(annotation != null) {
                writer.WriteElementString("annotation", annotation);
            }
            
            if(info != null) {
                writer.WriteElementString("info", info.AbsoluteUri);
            }
            
            if(image != null) {
                writer.WriteElementString("image", image.AbsoluteUri);
            }
        }
        
        public MetaEntry FindMetaEntry(string rel)
        {
            return FindMetaEntry(new Uri(ResolvedBaseUri, rel));
        }
        
        public MetaEntry FindMetaEntry(Uri uri)
        {
            if(meta == null) {
                return MetaEntry.Zero;
            }
            
            foreach(MetaEntry meta_entry in meta) {
                if(meta_entry.Rel == uri) {
                    return meta_entry;
                }
            }
            
            return MetaEntry.Zero;
        }
                
        public string Title {
            get { return title; }
            set { title = value; }
        }
        
        public string Creator {
            get { return creator; }
            set { creator = value; }
        }
        
        public string Annotation {
            get { return annotation; }
            set { annotation = value; }
        }
                
        public Uri Info {
            get { return info; }
            set { info = value; }
        }
        
        public Uri Image {
            get { return image; }
            set { image = value; }
        }
        
        public ReadOnlyCollection<MetaEntry> Meta {
            get { return new ReadOnlyCollection<MetaEntry>(meta); }
        }
        
        public ReadOnlyCollection<LinkEntry> Links {
            get { return new ReadOnlyCollection<LinkEntry>(links); }
        }
    }
}
