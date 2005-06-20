/***************************************************************************
 *  DecoderRegistry.cs
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
using System.Xml;
using Gst;

namespace Sonance
{
	public class DecoderInfo
	{
		public string Name;
		public string Longname;
		public string Filename;
		public string Mimetype;
		public string Version;
	}
	
	public class DecoderRegistry
	{
		public Hashtable DecoderTable;
		public Hashtable SynonymTable;
		public Hashtable MimeTable;
		public string registryPath;
		
		public DecoderRegistry()
		{
			registryPath = Paths.ApplicationData + "decoder-registry.xml";
			DecoderTable = new Hashtable();
			SynonymTable = new Hashtable();
			MimeTable = new Hashtable();
			
			if(!LoadRegistry() || DecoderTable.Count == 0) {
				BuildSupportedGstDecoderRegistry();
				BuildDefaultSynonymTable();
			}
				
			SaveRegistry();
			BuildMimeTable();
		}
		
		public bool LoadRegistry()
		{
			XmlDocument regDoc = new XmlDocument();
			
			try {
				regDoc.Load(registryPath);
			} catch(Exception) { 
				DebugLog.Add("Creating new Decoder Registry");
			}
			
			return true;
		}
		
		public void SaveRegistry()
		{
			XmlTextWriter writer = new XmlTextWriter(registryPath, 
				System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
				
			writer.WriteStartDocument();
			writer.WriteStartElement("decoderRegistry");
			
			writer.WriteStartElement("decoders");
			foreach(string key in DecoderTable.Keys) {
				DecoderInfo decoder = (DecoderInfo)DecoderTable[key];
				writer.WriteStartElement("decoder");
				writer.WriteElementString("name", decoder.Name);
				writer.WriteElementString("longname", decoder.Longname);
				writer.WriteElementString("filename", decoder.Filename);
				writer.WriteElementString("version", decoder.Version);
				writer.WriteElementString("mimetype", decoder.Mimetype);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			
			writer.WriteStartElement("mimeSynonyms");
			foreach(string name in SynonymTable.Keys) {
				Array array = (Array)SynonymTable[name];
				if(array == null)
					continue;
					
				writer.WriteStartElement("synonymGroup");
				writer.WriteAttributeString("name", name);
				foreach(string mime in array)
					writer.WriteElementString("mimetype", mime);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			
			writer.WriteEndElement();
  			writer.Close(); 
		}
		
		private bool AppendMimeTable(string mimetype)
		{
			try {
				MimeTable.Add(mimetype.ToLower(), true);
			} catch(Exception) {
				return false;
			}
			
			return true;
		}
		
		private void BuildMimeTable()
		{
			foreach(DecoderInfo decoder in DecoderTable.Values) {
				if(!AppendMimeTable(decoder.Mimetype))
					continue;
				
				foreach(Array synonyms in SynonymTable.Values) {
					if(Array.IndexOf(synonyms, decoder.Mimetype) < 0)
						continue;
						
					foreach(string mimetype in synonyms)
						AppendMimeTable(mimetype);
				}
			}
		}
		
		public void BuildDefaultSynonymTable()
		{
			DebugLog.Add("Configuring Default Mimetype Synonyms");
		
			string [] mp3Types = {
				"audio/x-mp3", 
				"application/x-id3", 
				"audio/mpeg", 
				"audio/x-mpeg", 
				"audio/x-mpeg-3", 
				"audio/mpeg3"
			};
			
			string [] oggTypes = {
				"application/x-ogg",
				"application/ogg",
				"audio/vorbis",
				"audio/x-vorbis",
				"audio/ogg",
				"audio/x-ogg"
			};
			
			string [] flacTypes = {
				"application/x-flac",
				"audio/x-flac",
				"audio/flac"
			};
			
			SynonymTable.Clear();
			SynonymTable.Add("MP3 Audio", mp3Types);
			SynonymTable.Add("OGG Vorbis Audio", oggTypes);
			SynonymTable.Add("FLAC Audio", flacTypes);
		}
		
		
		public bool AddUnique(DecoderInfo decoder)
		{
			try {
				DecoderTable.Add(decoder.Filename, decoder);
			} catch(Exception) {
				return false;
			}
			
			return true;
		}
		
		public void BuildSupportedGstDecoderRegistry()
		{
			GLib.List registryList = Registry.PoolList();
			
			foreach(XMLRegistry registry in registryList) {
				XmlDocument gstDoc = new XmlDocument();
				try {
					gstDoc.Load(registry.Location);
				} catch(Exception) {
					continue;
				}
			
				DebugLog.Add("Parsing GStreamer Registry '" + 
					registry.Location + "'");
			
				XmlNodeList pluginNodes = gstDoc.DocumentElement.
					GetElementsByTagName("plugin");

				foreach(XmlNode pluginNode in pluginNodes) {
					string filename = "unknown", version = "unknown";
					
					foreach(XmlNode featureNode in pluginNode.ChildNodes) {
						string mimetype = "unknown", name = "unknown", 
							longname = "unknown";
						XmlNodeList padTemplateNodes;
						
						if(featureNode.Name.Equals("filename")) 
							filename = featureNode.InnerText;
						else if(featureNode.Name.Equals("version"))
							version = featureNode.InnerText;
						else if(!featureNode.Name.Equals("feature"))
							continue;
						
						try {
							XmlNode classNode = 
								featureNode.SelectSingleNode("class");
							if(classNode != null && !classNode.InnerText.
								Equals("Codec/Decoder/Audio"))
								continue;
						} catch(Exception) {
							continue;
						}
						
						try {	
							padTemplateNodes = 
								featureNode.SelectNodes("padtemplate");
						} catch(Exception) {
							continue;
						}
						
						foreach(XmlNode padTemplateNode in padTemplateNodes) {
							XmlNode directionNode;
							
							try {
								directionNode = 
									padTemplateNode.
										SelectSingleNode("direction");
							} catch(Exception) {	
								continue;
							}
							
							if(!directionNode.InnerText.Equals("sink")) 
								continue;
								
							XmlNode capsNode = 
								padTemplateNode.SelectSingleNode("caps");
							
							mimetype = capsNode.InnerText.Split(',')[0];
							name = featureNode.
								SelectSingleNode("name").InnerText;
							longname = featureNode.
								SelectSingleNode("longname").InnerText;
								
							if(name.Equals("faad"))
								mimetype = "audio/x-m4a";
								
							DecoderInfo decoder = new DecoderInfo();
							decoder.Filename = filename;
							decoder.Mimetype = mimetype;
							decoder.Name = name;
							decoder.Longname = longname;
							decoder.Version = version;
							AddUnique(decoder);
						}
					}
				}
			}
		}
		
		public bool SupportedMimeType(string mimetype)
		{
			try {
				return MimeTable[mimetype.ToLower()] != null;
			} catch(Exception) {
				return false;
			}
		}
	}
}
