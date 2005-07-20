/***************************************************************************
 *  Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
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

/*
 * $Log$
 * Revision 1.2  2005/07/20 02:34:06  abock
 * Updates to entagged
 *
 * Revision 1.4  2005/02/13 17:22:17  kikidonk
 * Support for APIC
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.Collections;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mp3.Util.Id3Frames;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v23TagReader {
		
		private Hashtable conversion;
		
		public Id3v23TagReader() {
			InitConversionTable();
		}
		
		public Id3v2Tag Read(ByteBuffer data, bool[] ID3Flags, byte version)
		{
			int tagSize = data.Limit;
			byte[] b;
			Id3v2Tag tag = new Id3v2Tag();
			//----------------------------------------------------------------------------
			//Traitement en cas d'Header Etendu==true (A COMPLETER)
			if (version == Id3v2Tag.ID3V23 && ID3Flags[1])
				ProcessExtendedHeader(data);
			//----------------------------------------------------------------------------
			//Extraction des champs de texte
			int specSize = (version == Id3v2Tag.ID3V22) ? 3 : 4;
			for (int a = 0; a < tagSize; a++) {
				//Nom de la Frame
				b = new byte[specSize];
				
				if(data.Remaining <= specSize)
					break;
				
				data.Get(b);
				
				string field = new string(System.Text.Encoding.ASCII.GetChars(b));
				if (b[0] == 0)
					break;
				
				//La longueur du texte contenu dans la Frame
				int frameSize = ReadInteger(data, version);

				if ((frameSize > data.Remaining) || frameSize <= 0){
				//ignore empty frames
					break;
				}
				
				b = new byte[ frameSize + ((version == Id3v2Tag.ID3V23) ? 2 : 0) ];
				data.Get(b);
				
				if( "" != field) {
					Id3Frame f = CreateId3Frame(field, b, version);
					if(f != null)
					    tag.Add(f);
				}
			}
			
			return tag;
		}
		
		private void InitConversionTable()
		{

			//TODO: APIC frame must update the mime-type to be converted ??
			//TODO: LINK frame (2.3) has a frame ID of 3-bytes making
			//      it incompatible with 2.3 frame ID of 4bytes, WTF???
			
			this.conversion = new Hashtable();
			string[] v22 = {
					"BUF", "CNT", "COM", "CRA",
					"CRM", "ETC", "EQU", "GEO",
					"IPL", "LNK", "MCI", "MLL",
					"PIC", "POP", "REV", "RVA",
					"SLT", "STC", "TAL", "TBP",
					"TCM", "TCO", "TCR", "TDA",
					"TDY", "TEN","TFT", "TIM",
					"TKE", "TLA", "TLE", "TMT",
					"TOA", "TOF", "TOL", "TOR",
					"TOT", "TP1", "TP2", "TP3",
					"TP4", "TPA", "TPB", "TRC",
					"TRD", "TRK", "TSI", "TSS",
					"TT1", "TT2", "TT3", "TXT",
					"TXX", "TYE", "UFI", "ULT",
					"WAF", "WAR", "WAS", "WCM",
					"WCP", "WPB", "WXX"
				};
			string[] v23 = {
					"RBUF", "PCNT", "COMM", "AENC",
					"", "ETCO", "EQUA", "GEOB",
					"IPLS", "LINK", "MCDI", "MLLT",
					"APIC", "POPM", "RVRB", "RVAD",
					"SYLT", "SYTC", "TALB", "TBPM",
					"TCOM", "TCON", "TCOP", "TDAT",
					"TDLY", "TENC", "TFLT", "TIME",
					"TKEY", "TLAN", "TLEN", "TMED",
					"TOPE", "TOFN", "TOLY", "TORY",
					"TOAL", "TPE1", "TPE2", "TPE3",
					"TPE4", "TPOS", "TPUB", "TSRC",
					"TRDA", "TRCK", "TSIZ", "TSSE",
					"TIT1", "TIT2", "TIT3", "TEXT",
					"TXXX", "TYER", "UFID", "USLT",
					"WOAF", "WOAR", "WOAS", "WCOM",
					"WCOP", "WPUB", "WXXX"			
				};
			
			for(int i = 0; i<v22.Length; i++)
				this.conversion[v22[i]] = v23[i];
		}
		
		private Id3Frame CreateId3Frame(string field, byte[] data, byte version)
		{
			if(version == Id3v2Tag.ID3V22)
				field = ConvertFromId3v22(field);
			
			if("" == field)
				return null;
				
			//Text frames
			if (field.StartsWith("T") && !field.StartsWith("TX")) {
				return new TextId3Frame(field, data, version);
			}
			//Comment
			else if (field.StartsWith("COMM"))
				return new CommId3Frame(data, version);
			else if (field.StartsWith("APIC"))
			    return new ApicId3Frame(data, version);
			//Any other frame
			else
				return new GenericId3Frame(field, data, version);
		}
		
		private string ConvertFromId3v22(string field)
		{
			string s = this.conversion[field] as string;
			
			if(s == null)
				return "";
			
			return s;
		}

		//Process the Extended Header in the ID3v2 Tag, returns the number of bytes to skip
		private int ProcessExtendedHeader(ByteBuffer data)
		{
			//TODO Verify that we have an syncsfe int
			byte[] exthead = new byte [4];
			data.Get(exthead);
			int extsize = ReadInteger(data, Id3v2Tag.ID3V23);
			// The extended header size includes those first four bytes.
			data.Position = data.Position + extsize;
			return extsize;
		}

		private int ReadInteger(ByteBuffer bb, int version)
		{
			int value = 0;

			if(version == Id3v2Tag.ID3V23)
				value += (bb.Get()& 0xFF) << 24;
			value += (bb.Get()& 0xFF) << 16;
			value += (bb.Get()& 0xFF) << 8;
			value += (bb.Get()& 0xFF);

			return value;
		}
	}
}
