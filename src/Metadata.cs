/***************************************************************************
 *  Metadata.cs
 *
 *  Copyright (C) 2004, 2005 Jorn Baayen
 *  jbaayen@gnome.org
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
using System.Runtime.InteropServices;
using System.Collections;

using Gdk;

using Mono.Posix;

namespace Sonance
{
	public class Metadata
	{
		// Strings
		private static readonly string string_error_load =
			"Failed to load metadata: {0}";

		// Objects
		private IntPtr raw = IntPtr.Zero;
		private Pixbuf album_art = null;

		// Constructor
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_load (string filename,
					                    out IntPtr error_message_return);
		
		public Metadata (string filename)
		{
			IntPtr error_ptr;
			
			raw = metadata_load (filename, out error_ptr);

			if (error_ptr != IntPtr.Zero) {
				string error = GLib.Marshaller.PtrToStringGFree (error_ptr);
				throw new Exception (String.Format (string_error_load, error));
			}
		}

		// Properties
		// Properties :: Title (get;)
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_title (IntPtr metadata);

		public string Title {
			get {
				IntPtr p = metadata_get_title (raw);

				return (p == IntPtr.Zero) ? "" : Marshal.PtrToStringAnsi (p).Trim ();
			}
		}

		// Properties :: Artists (get;)
		//	FIXME: Refactor Artists and Performers properties
		[DllImport ("libsonance")]
		private static extern int metadata_get_artist_count (IntPtr metadata);

		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_artist (IntPtr metadata, int index);

		public string [] Artists {
			get {
				ArrayList strings = new ArrayList ();

				int count = metadata_get_artist_count (raw);

				for (int i = 0; i < count; i++) {
					string tmp = Marshal.PtrToStringAnsi (metadata_get_artist (raw, i)).Trim ();

					if (tmp.Length <= 0)
						continue;

					strings.Add (tmp);
				}

				return (string []) strings.ToArray (typeof (string));
			}
		}

		// Properties :: Performers (get;)
		//	FIXME: Refactor Artists and Performers properties
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_performer (IntPtr metadata, int index);

		[DllImport ("libsonance")]
		private static extern int metadata_get_performer_count (IntPtr metadata);

		public string [] Performers {
			get {
				ArrayList strings = new ArrayList ();

				int count = metadata_get_performer_count (raw);

				for (int i = 0; i < count; i++) {
					string tmp = Marshal.PtrToStringAnsi (metadata_get_performer (raw, i)).Trim ();

					if (tmp.Length <= 0)
						continue;

					strings.Add (tmp);
				}

				return (string []) strings.ToArray (typeof (string));
			}			
		}

		// Properties :: Album (get;)
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_album (IntPtr metadata);

		public string Album {
			get { 
				IntPtr p = metadata_get_album (raw);
				
				return (p == IntPtr.Zero) ? "" : Marshal.PtrToStringAnsi (p).Trim ();
			}
		}

		// Properties :: AlbumArt (get;)
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_album_art (IntPtr metadata);

		public Pixbuf AlbumArt {
			get { 
				if (album_art != null)
					return album_art;
					
				IntPtr p = metadata_get_album_art (raw);

				if (p != IntPtr.Zero)
					album_art = new Pixbuf (p);

				return album_art;
			}
		}

		// Properties :: TrackNumber (get;)
		[DllImport ("libsonance")]
		private static extern uint metadata_get_track_number (IntPtr metadata);
		
		public uint TrackNumber {
			get { return metadata_get_track_number (raw); }
		}

		// Properties :: TotalTracks (get;)
		[DllImport ("libsonance")]
		private static extern uint metadata_get_total_tracks (IntPtr metadata);
		
		public uint TotalTracks {
			get { return metadata_get_total_tracks (raw); }
		}

		// Properties :: DiscNumber (get;)
		[DllImport ("libsonance")]
		private static extern uint metadata_get_disc_number (IntPtr metadata);

		public uint DiscNumber {
			get { return metadata_get_disc_number (raw); }
		}

		// Properties :: Year (get;)
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_year (IntPtr metadata);

		public string Year {
			get {
				IntPtr p = metadata_get_year (raw);
				
				return (p == IntPtr.Zero) ? "" : Marshal.PtrToStringAnsi (p);
			}
		}

		// Properties :: Duration (get;)
		[DllImport ("libsonance")]
		private static extern uint metadata_get_duration (IntPtr metadata);

		public uint Duration {
			get { return metadata_get_duration (raw); }
		}

		// Properties :: MimeType (get;)
		[DllImport ("libsonance")]
		private static extern IntPtr metadata_get_mime_type (IntPtr metadata);

		public string MimeType {
			get {
				IntPtr p = metadata_get_mime_type (raw);
				
				return (p == IntPtr.Zero) ? "" : Marshal.PtrToStringAnsi (p);
			}
		}

		// Properties :: MTime (get;)
		[DllImport ("libsonance")]
		private static extern uint metadata_get_mtime (IntPtr metadata);

		public uint MTime {
			get { return metadata_get_mtime (raw); }
		}

		// Properties :: Gain (get;)
		[DllImport ("libsonance")]
		private static extern double metadata_get_gain (IntPtr metadata);

		public double Gain {
			get { return metadata_get_gain (raw); }
		}

		// Properties :: Peak (get;)
		[DllImport ("libsonance")]
		private static extern double metadata_get_peak (IntPtr metadata);
		
		public double Peak {
			get { return metadata_get_peak (raw); }
		}
	}
}
