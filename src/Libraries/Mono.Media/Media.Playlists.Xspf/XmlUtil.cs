//
// XmlUtil.cs
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

namespace Media.Playlists.Xspf
{
    internal static class XmlUtil
    {
        internal static string ReadString(XmlNode parentNode, XmlNamespaceManager xmlns, string xpath)
        {
            XmlNode node = parentNode.SelectSingleNode(xpath, xmlns);
            
            if(node == null) {
                return null;
            }
            
            return node.InnerText == null ? null : node.InnerText.Trim();
        }
        
        internal static Uri ReadUri(XmlNode node, XmlNamespaceManager xmlns, Uri baseUri, string xpath)
        {
            string str = ReadString(node, xmlns, xpath);
            if(str == null) {
                return null;
            }
            
            return ResolveUri(baseUri, str);
        }
        
        internal static DateTime ReadDate(XmlNode node, XmlNamespaceManager xmlns, string xpath)
        {
            string str = ReadString(node, xmlns, xpath);
            if(str == null) {
                return DateTime.MinValue;
            }
            
            W3CDateTime datetime = W3CDateTime.Parse(str);
            return datetime.LocalTime;
        }       
        
        internal static uint ReadUInt(XmlNode node, XmlNamespaceManager xmlns, string xpath)
        {
            string str = ReadString(node, xmlns, xpath);
            if(str == null) {
                return 0;
            }
            
            return UInt32.Parse(str);
        }
        
        internal static string ReadRelPair(XmlNode node, Uri baseUri, out Uri rel)
        {
            XmlAttribute attr = node.Attributes["rel"];
            string rel_value = attr == null || attr.Value == null ? null : attr.Value.Trim();
            string value = node.InnerText == null ? null : node.InnerText.Trim();
            
            if(rel_value == null || value == null) {
                rel = null;
                return null;
            }
            
            rel = ResolveUri(baseUri, rel_value);
            return value;
        }
        
        internal static List<MetaEntry> ReadMeta(XmlNode parentNode, XmlNamespaceManager xmlns, 
            Uri baseUri, string xpath)
        {
            List<MetaEntry> meta_collection = new List<MetaEntry>();
            
            foreach(XmlNode node in parentNode.SelectNodes(xpath, xmlns)) {
                Uri rel;
                string value = ReadRelPair(node, baseUri, out rel);
                
                if(value == null || rel == null) {
                    continue;
                }
                
                meta_collection.Add(new MetaEntry(rel, value));
            }
            
            return meta_collection;
        }
        
        internal static List<LinkEntry> ReadLinks(XmlNode parentNode, XmlNamespaceManager xmlns, 
            Uri baseUri, string xpath)
        {
            List<LinkEntry> link_collection = new List<LinkEntry>();
            
            foreach(XmlNode node in parentNode.SelectNodes(xpath, xmlns)) {
                Uri rel;
                string value = ReadRelPair(node, baseUri, out rel);
                
                if(value == null || rel == null) {
                    continue;
                }

                link_collection.Add(new LinkEntry(rel, ResolveUri(baseUri, value)));
            }
            
            return link_collection;
        }
        
        internal static List<Uri> ReadUris(XmlNode parentNode, XmlNamespaceManager xmlns, 
            Uri baseUri, string xpath)
        {
            List<Uri> uri_collection = new List<Uri>();
            
            foreach(XmlNode node in parentNode.SelectNodes(xpath, xmlns)) {
                string value = node.InnerText == null ? null : node.InnerText.Trim();
                if(value != null) {
                    uri_collection.Add(ResolveUri(baseUri, value));
                }
            }
            
            return uri_collection;
        }
        
        internal static Uri ResolveUri(Uri baseUri, string str)
        {
            try {
                return baseUri == null ? new Uri(str) : new Uri(baseUri, str);
            } catch {
                return null;
            }
        }
    }
}
