/***************************************************************************
 *  Copyright 2005 Novell, Inc.
 *  Aaron Bockover <aaron@aaronbock.net>
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

using Entagged.Audioformats.Util;

namespace Entagged.Audioformats.M4a.Util
{
	public class M4aTagField : TagTextField 
	{
		private bool common;
		private string content;
		private string id;

		public M4aTagField(string fieldId, string fieldContent) 
		{
			id = fieldId.ToUpper();
			content = fieldContent;
		}

		public void CopyContent(TagField field) 
		{
			if(field is TagTextField)
				content = (field as TagTextField).Content;
		}

		public string Content 
		{
			get { 
				return content; 
			}

			set { 
				content = value; 
			}
		}

		public string Encoding 
		{
			get { 
				return "UTF-8"; 
			}

			set { 
				/* NA */ 
			}
		}

		public string Id 
		{
			get { 
				return id; 
			}
		}

		public byte[] RawContent
		{
			get {
				return null;
			}
		}

		public bool IsBinary 
		{
			get { 
				return false; 
			}

			set { 
				/* NA */ 
			}
		}

		public bool IsCommon
		{
			get { 
				return common; 
			}
		}

		public bool IsEmpty 
		{
			get { 
				return content.Equals(string.Empty); 
			}
		}

		public override string ToString() 
		{
			return Content;
		}
	}
}
