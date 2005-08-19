using System;

namespace Entagged.Audioformats.Util {
	
	[AttributeUsage (AttributeTargets.Class, AllowMultiple=true)]
	public class SupportedExtension : Attribute {
		private string extension;
		public string Extension {
			get { return extension; }
		}

		public SupportedExtension (string extension)
		{
			this.extension = extension;
		}
	}

	[AttributeUsage (AttributeTargets.Class, AllowMultiple=true)]
	public class SupportedMimeType : Attribute {
		private string mime_type;
		public string MimeType {
			get { return mime_type; }
		}

		public SupportedMimeType (string mime_type)
		{
			this.mime_type = mime_type;
		}
	}

}

