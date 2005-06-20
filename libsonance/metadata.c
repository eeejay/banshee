/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

#include <libgnomevfs/gnome-vfs.h>
#include <vorbis/vorbisfile.h>
#include <FLAC/metadata.h>
#include <FLAC/stream_decoder.h>
#include <glib.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <config.h>

#if HAVE_FAAD
#include <faad.h>
#include <mp4ff.h>
#endif /* HAVE_FAAD */

#if HAVE_ID3TAG
#include <id3tag.h>
#include "id3-vfs/id3-vfs.h"
#endif /* HAVE_ID3TAG */

#include "ogg-helper.h"
#include "metadata.h"

struct _Metadata {
	char *title;

	char **artists;
	int artists_count;

	char **performers;
	int performers_count;

	char *album;

	int track_number;
	int total_tracks;
	int disc_number;

	char *year;

	int duration;

	char *mime_type;

	int mtime;

	double gain;
	double peak;

	GdkPixbuf *album_art;
};

static void
parse_raw_track_number (Metadata *metadata,
			const char *raw)
{
	char *part2;

	if (raw == NULL) {
		metadata->track_number = -1;
		metadata->total_tracks = -1;

		return;
	}

	part2 = strstr (raw, "/");

	if (part2 != NULL)
		metadata->total_tracks = atoi (part2 + 1);
	else
		metadata->total_tracks = -1;

	metadata->track_number = atoi (raw);
}

static void
ensure_track_number (Metadata *metadata,
		     const char *filename)
{
	char *basename;
	int num;

	if (metadata->track_number > 0)
		return;

	basename = g_path_get_basename (filename);
	num = atoi (basename);
	g_free (basename);

	if (num < 100) /* protection against strange filenames */
		metadata->track_number = num;
}

#if HAVE_ID3TAG

static int
get_mp3_duration (struct id3_tag *tag)
{
	struct id3_frame const *frame;
	union id3_field const *field;
	unsigned int nstrings;
	id3_latin1_t *latin1;
	int time;

	/* The following is based on information from the
	 * ID3 tag version 2.4.0 Native Frames informal standard.
	 */
	frame = id3_tag_findframe (tag, "TLEN", 0);
	if (frame == NULL)
		return -1;

	field = id3_frame_field (frame, 1);
	nstrings = id3_field_getnstrings (field);
	if (nstrings <= 0)
		return -1;

	latin1 = id3_ucs4_latin1duplicate (id3_field_getstrings (field, 0));
	if (latin1 == NULL)
		return -1;

	/* "The 'Length' frame contains the length
	 * of the audio file in milliseconds,
	 * represented as a numeric string."
	 */
	time = atol ((char *) latin1);
	g_free (latin1);

	if (time > 0)
		return (int) floor (time / 1000 + 0.5);

	return -1;
}

/*
 *      <Header for 'Attached picture', ID: "APIC">
 *      Text encoding      $xx
 *      MIME type          <text string> $00
 *      Picture type       $xx
 *      Description        <text string according to encoding> $00 (00)
 *      Picture data       <binary data>
 */
static GdkPixbuf *
get_mp3_picture_data (struct id3_tag *tag, const char *field_name)
{
	const struct id3_frame *frame;
	const union id3_field *field;
	id3_byte_t const *picture_data;
	id3_length_t length;
	GdkPixbuf *ret;
	gboolean bail = FALSE;

	/* Note: An MP3 can actualy have multiple embedded images. We should
	 * really check them all for one of "type" [field 2] 03, however with
	 * limited test data, plucking the first image out of the MP3 seems to
	 * work here. More testing will guage whether we need to go into detail 
	 */
	frame = id3_tag_findframe (tag, field_name, 0);
	if (frame == 0)
		return NULL;

	/* Picture data resides in frame 4 */
	field = id3_frame_field (frame, 4);

	if (field == NULL)
		return NULL;

	picture_data = id3_field_getbinarydata (field, &length);

	if (picture_data == NULL)
		return NULL;

	GdkPixbufLoader *pb_loader = gdk_pixbuf_loader_new ();
	if (!gdk_pixbuf_loader_write (pb_loader, picture_data, length, NULL))
		bail = TRUE;

	if (!gdk_pixbuf_loader_close (pb_loader, NULL))
		bail = TRUE;

	if (bail) {
		g_object_unref (pb_loader);

		return NULL;
	}

	ret = gdk_pixbuf_loader_get_pixbuf (pb_loader);
	if (ret != NULL &&
	    gdk_pixbuf_get_width (ret) >= 64 &&
	    gdk_pixbuf_get_height (ret) >= 64)
		ret = g_object_ref (ret);
	else
		ret = NULL;

	g_object_unref (pb_loader);

	return ret;
}

static double
get_mp3_gain (struct id3_tag *tag)
{
	const struct id3_frame *frame;
	id3_latin1_t const *id;
	id3_byte_t const *data;
	id3_length_t length;

	enum {
		CHANNEL_OTHER         = 0x00,
		CHANNEL_MASTER_VOLUME = 0x01,
		CHANNEL_FRONT_RIGHT   = 0x02,
		CHANNEL_FRONT_LEFT    = 0x03,
		CHANNEL_BACK_RIGHT    = 0x04,
		CHANNEL_BACK_LEFT     = 0x05,
		CHANNEL_FRONT_CENTRE  = 0x06,
		CHANNEL_BACK_CENTRE   = 0x07,
		CHANNEL_SUBWOOFER     = 0x08
	};

	/* get relative volume adjustment information */
	/* code taken from mad, player.c */
	frame = id3_tag_findframe (tag, "RVA2", 0);
	if (frame == 0)
		return 0.0;

	id = id3_field_getlatin1 (id3_frame_field (frame, 0));
	data = id3_field_getbinarydata (id3_frame_field (frame, 1), &length);

	if (!id || !data)
		return 0.0;

	/*
	* "The 'identification' string is used to identify the situation
	* and/or device where this adjustment should apply. The following is
	* then repeated for every channel
	*
	*   Type of channel         $xx
	*   Volume adjustment       $xx xx
	*   Bits representing peak  $xx
	*   Peak volume             $xx (xx ...)"
	*/

	while (length >= 4) {
		unsigned int peak_bytes;

		peak_bytes = (data[3] + 7) / 8;
		if (4 + peak_bytes > length)
			break;

		if (data[0] == CHANNEL_MASTER_VOLUME) {
			signed int voladj_fixed;

			/*
			 * "The volume adjustment is encoded as a fixed point decibel
			 * value, 16 bit signed integer representing (adjustment*512),
			 * giving +/- 64 dB with a precision of 0.001953125 dB."
			 */

			voladj_fixed  = (data[1] << 8) | (data[2] << 0);
			voladj_fixed |= -(voladj_fixed & 0x8000);

			return (double) voladj_fixed / 512;
		}

		data   += 4 + peak_bytes;
		length -= 4 + peak_bytes;
	}

	return 0.0;
}

static int
get_mp3_comment_count (struct id3_tag *tag,
		       const char *field_name)
{
	const struct id3_frame *frame;
	const union id3_field *field;

	frame = id3_tag_findframe (tag, field_name, 0);
	if (frame == 0)
		return 0;

	field = id3_frame_field (frame, 1);

	return id3_field_getnstrings (field);
}

static char *
get_mp3_comment_value (struct id3_tag *tag,
		       const char *field_name,
		       int index)
{
	const struct id3_frame *frame;
	const union id3_field *field;
	const id3_ucs4_t *ucs4 = NULL;
        id3_utf8_t *utf8 = NULL;

	frame = id3_tag_findframe (tag, field_name, 0);
	if (frame == 0)
		return NULL;

	field = id3_frame_field (frame, 1);

	if (index >= id3_field_getnstrings (field))
		return NULL;

	ucs4 = id3_field_getstrings (field, index);
	if (ucs4 == NULL)
		return NULL;

	utf8 = id3_ucs4_utf8duplicate (ucs4);
	if (utf8 == NULL)
		return NULL;

	if (!g_utf8_validate ((char *) utf8, -1, NULL)) {
		g_free (utf8);

		return NULL;
	}

	return (char *) utf8;
}

static Metadata *
assign_metadata_mp3 (const char *filename,
		     GnomeVFSFileInfo *info,
		     char **error_message_return)
{
	Metadata *metadata;
	struct id3_vfs_file *file;
	struct id3_tag *tag;
	int bitrate, samplerate, channels, version, vbr, count, i;
	int time, tag_time;
	char *track_number_raw;
	char *disc_number_raw;

	file = id3_vfs_open (filename, ID3_FILE_MODE_READONLY);
	if (file == NULL) {
		*error_message_return = g_strdup ("Failed to open file for reading");
		return NULL;
	}

	tag = id3_vfs_tag (file);

	if (id3_vfs_bitrate (file,
			     &bitrate,
			     &samplerate,
			     &time,
			     &version,
			     &vbr,
			     &channels) == 0) {
		id3_vfs_close (file);

		*error_message_return = g_strdup ("Failed to gather information about the file");
		return NULL;
	}

	metadata = g_new0 (Metadata, 1);

	tag_time = get_mp3_duration (tag);

	if (tag_time > 0)
		metadata->duration = tag_time;
	else if (time > 0)
		metadata->duration = time;
	else {
		if (bitrate > 0) {
			metadata->duration = ((double) info->size) / ((double) bitrate / 8.0f);
		} else {
			/* very rough guess */
			metadata->duration = ((double) info->size) / ((double) 128000 / 8.0f);
		}
	}

	metadata->title = get_mp3_comment_value (tag, ID3_FRAME_TITLE, 0);

	count = get_mp3_comment_count (tag, ID3_FRAME_ARTIST);
	metadata->artists = g_new (char *, count + 1);
	metadata->artists[count] = NULL;
	metadata->artists_count = count;
	for (i = 0; i < count; i++) {
		metadata->artists[i] = get_mp3_comment_value (tag, ID3_FRAME_ARTIST, i);
	}

	count = get_mp3_comment_count (tag, "TPE2");
	metadata->performers = g_new (char *, count + 1);
	metadata->performers[count] = NULL;
	metadata->performers_count = count;
	for (i = 0; i < count; i++) {
		metadata->performers[i] = get_mp3_comment_value (tag, "TPE2", i);
	}

	metadata->album = get_mp3_comment_value (tag, ID3_FRAME_ALBUM, 0);

	track_number_raw = get_mp3_comment_value (tag, ID3_FRAME_TRACK, 0);
	parse_raw_track_number (metadata, track_number_raw);
	g_free (track_number_raw);

        disc_number_raw = get_mp3_comment_value (tag, "TPOS", 0);
        if (disc_number_raw != NULL)
                metadata->disc_number = atoi (disc_number_raw);
        else
                metadata->disc_number = -1;
        g_free (disc_number_raw);

	metadata->year = get_mp3_comment_value (tag, ID3_FRAME_YEAR, 0);

	metadata->gain = get_mp3_gain (tag);

	metadata->album_art = get_mp3_picture_data (tag, "APIC");

	id3_vfs_close (file);

	*error_message_return = NULL;

	return metadata;
}

#endif /* HAVE_ID3TAG */

#if HAVE_FAAD

static uint32_t mp4_read_callback (void *user_data, void *buffer, uint32_t length);
static uint32_t mp4_seek_callback (void *user_data, uint64_t position);

static uint32_t
mp4_read_callback (void *user_data, void *buffer, uint32_t length)
{
	GnomeVFSFileSize read;
	gnome_vfs_read ((GnomeVFSHandle*)user_data, (gpointer *) buffer,  length, &read);
	return (uint32_t)read;
}

static uint32_t
mp4_seek_callback (void *user_data, uint64_t position)
{
	return (uint32_t) gnome_vfs_seek ((GnomeVFSHandle*) user_data, GNOME_VFS_SEEK_START, position);
}

static Metadata *
assign_metadata_mp4 (const char *filename,
		     char **error_message_return)
{
	Metadata *m = NULL;
	GnomeVFSHandle *fh;
	mp4ff_t *mp4f;
	mp4ff_callback_t *mp4cb = (mp4ff_callback_t*) malloc (sizeof (mp4ff_callback_t));
	gchar *value;
	gchar *item;
	unsigned char *buff = NULL;
        guint buff_size = 0;
	int j, k;
	mp4AudioSpecificConfig mp4ASC;
	long samples;
	float f = 1024.0;

	if (gnome_vfs_open (&fh, filename, GNOME_VFS_OPEN_READ) != GNOME_VFS_OK) {
                *error_message_return = g_strdup ("Failed to open file for reading");
                return NULL;
        }

	mp4cb->read = mp4_read_callback;
	mp4cb->seek = mp4_seek_callback;
	mp4cb->user_data = fh;

	mp4f = mp4ff_open_read (mp4cb);

	if (!mp4f) {
		*error_message_return = g_strdup ("Unable to open the AAC file");
		return NULL;
	}

	if (mp4ff_total_tracks (mp4f) > 1) {
		 *error_message_return = g_strdup ("Multi-track AAC files not supported");
		 return NULL;
	}

	m = g_new0 (Metadata, 1);

	j = mp4ff_meta_get_num_items (mp4f);

	for (k = 0; k < j; k++) {
		if (mp4ff_meta_get_by_index (mp4f, k, &item, &value)) {
			if (!strcmp (item, "title")) {
				if (g_utf8_validate (value, -1, NULL))
					m->title = g_strdup (value);
			} else if (!strcmp (item, "artist")) {
				if (g_utf8_validate (value, -1, NULL)) {
					m->artists = g_new (char *, 2);
					m->artists[0] = g_strdup (value);
					m->artists[1] = NULL;
					m->artists_count = 1;
				}
			} else if (!strcmp (item, "album")) {
				if (g_utf8_validate (value, -1, NULL))
					m->album = g_strdup (value);
			} else if (!strcmp (item, "track"))
				m->track_number = atoi (value);
			else if (!strcmp (item, "totaltracks"))
				m->total_tracks = atoi (value);
			else if (!strcmp (item, "disc"))
				m->disc_number = atoi (value);
			else if (!strcmp (item, "date")) {
				if (g_utf8_validate (value, -1, NULL))
					m->year = g_strdup (value);
			}
			free (item);
			free (value);
		}
	}

	/*
	 * duration code shameless based on code from frontend/main.c in faad2
	 */

	samples = mp4ff_num_samples (mp4f, 0);
	mp4ff_get_decoder_config (mp4f, 0, &buff, &buff_size);

	if (buff)
	{
		if (AudioSpecificConfig (buff, buff_size, &mp4ASC) >= 0)
		{
			free (buff);
		        if (mp4ASC.sbr_present_flag == 1) {
		            f = f * 2.0;
		        }

			m->duration = (float) samples * (float) (f - 1.0) / (float) mp4ASC.samplingFrequency;
		}
	}

	mp4ff_close (mp4f);
	free (mp4cb);
	gnome_vfs_close (fh);

	return m;
}

#endif /* HAVE_FAAD */

static ov_callbacks file_info_callbacks =
{
        ogg_helper_read,
        ogg_helper_seek,
        ogg_helper_close_dummy,
        ogg_helper_tell
};

static char *
get_vorbis_comment_value (vorbis_comment *comment,
			  const char *entry,
			  int index)
{
	const char *val;

	val = vorbis_comment_query (comment, (char *) entry, index);
	if (val == NULL)
		return NULL;

	/* vorbis comments should be in UTF-8 */
	if (!g_utf8_validate (val, -1, NULL))
		return NULL;

	return g_strdup (val);
}

static void
assign_metadata_vorbiscomment (Metadata *metadata,
			       vorbis_comment *comment)
{
	char *raw, *version, *title;
	int count, i;

	version = get_vorbis_comment_value (comment, "version", 0);

	title = get_vorbis_comment_value (comment, "title", 0);

	if (version != NULL && title != NULL) {
		metadata->title = g_strdup_printf ("%s (%s)", title, version);

		g_free (title);
		g_free (version);
	} else if (title != NULL) {
		metadata->title = title;
	} else if (version != NULL) {
		metadata->title = version;
	}

	count = vorbis_comment_query_count (comment, "artist");
	metadata->artists = g_new (char *, count + 1);
	metadata->artists[count] = NULL;
	metadata->artists_count = count;
	for (i = 0; i < count; i++) {
		metadata->artists[i] = get_vorbis_comment_value (comment, "artist", i);
	}

	count = vorbis_comment_query_count (comment, "performer");
	metadata->performers = g_new0 (char *, count + 1);
	metadata->performers[count] = NULL;
	metadata->performers_count = count;
	for (i = 0; i < count; i++) {
		metadata->performers[i] = get_vorbis_comment_value (comment, "performer", i);
	}

	metadata->album = get_vorbis_comment_value (comment, "album", 0);

	raw = vorbis_comment_query (comment, "tracknumber", 0);
	parse_raw_track_number (metadata, raw);

	if (metadata->total_tracks < 0) {
		raw = vorbis_comment_query (comment, "tracktotal", 0);
		if (raw != NULL)
			metadata->total_tracks = atoi (raw);
	}

	raw = vorbis_comment_query (comment, "discnumber", 0);
	if (raw != NULL)
		metadata->disc_number = atoi (raw);
	else
		metadata->disc_number = -1;

	metadata->year = get_vorbis_comment_value (comment, "date", 0);

	raw = vorbis_comment_query (comment, "replaygain_album_gain", 0);
	if (raw == NULL) {
		raw = vorbis_comment_query (comment, "replaygain_track_gain", 0);
		if (raw == NULL) {
			raw = vorbis_comment_query (comment, "rg_audiophile", 0);
			if (raw == NULL) {
				raw = vorbis_comment_query (comment, "rg_radio", 0);
			}
		}
	}

	if (raw != NULL)
		metadata->gain = atof (raw);
	else
		metadata->gain = 0.0;

	raw = vorbis_comment_query (comment, "replaygain_album_peak", 0);
	if (raw == NULL) {
		raw = vorbis_comment_query (comment, "replaygain_track_peak", 0);
		if (raw == NULL) {
			raw = vorbis_comment_query (comment, "rg_peak", 0);
		}
	}

	if (raw != NULL)
		metadata->peak = atof (raw);
	else
		metadata->peak = 0.0;
}

static Metadata *
assign_metadata_ogg (const char *filename,
		     char **error_message_return)
{
	Metadata *metadata = NULL;
	GnomeVFSResult res;
	GnomeVFSHandle *handle;
	int rc;
	OggVorbis_File vf;
	vorbis_comment *comment;

	res = gnome_vfs_open (&handle, filename, GNOME_VFS_OPEN_READ);
	if (res != GNOME_VFS_OK) {
		*error_message_return = g_strdup ("Failed to open file for reading");
		return NULL;
	}

	rc = ov_open_callbacks (handle, &vf, NULL, 0,
				file_info_callbacks);
	if (rc < 0) {
		ogg_helper_close (handle);
		*error_message_return = g_strdup ("Failed to open file as Ogg Vorbis");
		return NULL;
	}

	comment = ov_comment (&vf, -1);
	if (!comment) {
		*error_message_return = g_strdup ("Failed to read comments");
		goto out;
	}

	metadata = g_new0 (Metadata, 1);

	assign_metadata_vorbiscomment (metadata, comment);

	metadata->duration = ov_time_total (&vf, -1);

	*error_message_return = NULL;

out:
	ov_clear (&vf);
	ogg_helper_close (handle);

	return metadata;
}

typedef struct {
	GnomeVFSHandle *handle;

	vorbis_comment *comment;
	int duration;
} CallbackData;

static FLAC__StreamDecoderReadStatus
FLAC_read_callback (const FLAC__StreamDecoder *decoder, FLAC__byte buffer[], unsigned *bytes, void *client_data)
{
	CallbackData *data = (CallbackData *) client_data;
	GnomeVFSFileSize read;
	GnomeVFSResult result;

	result = gnome_vfs_read (data->handle, buffer, *bytes, &read);

	if (result == GNOME_VFS_OK) {
		*bytes = read;
		return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
	} else if (result == GNOME_VFS_ERROR_EOF) {
		return FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
	} else {
		return FLAC__STREAM_DECODER_READ_STATUS_ABORT;
	}
}

static FLAC__StreamDecoderWriteStatus
FLAC_write_callback (const FLAC__StreamDecoder *decoder, const FLAC__Frame *frame,
		     const FLAC__int32 *const buffer[], void *client_data)
{
	/* This callback should never be called, because we request that
	 * FLAC only decodes metadata, never actual sound data. */
	return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
}

static void
FLAC_metadata_callback (const FLAC__StreamDecoder *decoder, const FLAC__StreamMetadata *metadata, void *client_data)
{
	CallbackData *data = (CallbackData *) client_data;

	if (metadata->type == FLAC__METADATA_TYPE_STREAMINFO) {
		data->duration = metadata->data.stream_info.total_samples / metadata->data.stream_info.sample_rate;
	} else if (metadata->type == FLAC__METADATA_TYPE_VORBIS_COMMENT) {
		const FLAC__StreamMetadata_VorbisComment *vc_block = &metadata->data.vorbis_comment;
		vorbis_comment *comment = data->comment;
		int c;

		for (c = 0; c < vc_block->num_comments; c++) {
			FLAC__StreamMetadata_VorbisComment_Entry entry = vc_block->comments[c];
			char *null_terminated_comment = malloc (entry.length + 1);
			char **parts;

			memcpy (null_terminated_comment, entry.entry, entry.length);
			null_terminated_comment[entry.length] = '\0';
			parts = g_strsplit (null_terminated_comment, "=", 2);

			if (parts[0] == NULL || parts[1] == NULL)
				goto free_continue;

			vorbis_comment_add_tag (comment, parts[0], parts[1]);

		free_continue:
			g_strfreev (parts);
			free (null_terminated_comment);
		}
	}
}

static void
FLAC_error_callback (const FLAC__StreamDecoder *decoder, FLAC__StreamDecoderErrorStatus status, void *client_data)
{
}

static Metadata *
assign_metadata_flac (const char *filename,
		      char **error_message_return)
{
	Metadata *metadata = NULL;
	GnomeVFSResult res;
	GnomeVFSHandle *handle;
	vorbis_comment *comment;
	FLAC__StreamDecoder *flac_decoder;
	CallbackData *callback_data;

	res = gnome_vfs_open (&handle, filename, GNOME_VFS_OPEN_READ);
	if (res != GNOME_VFS_OK) {
		*error_message_return = g_strdup ("Failed to open file for reading");
		return NULL;
	}

	comment = g_new (vorbis_comment, 1);
	vorbis_comment_init (comment);

	flac_decoder = FLAC__stream_decoder_new ();

	FLAC__stream_decoder_set_read_callback (flac_decoder, FLAC_read_callback);
	FLAC__stream_decoder_set_write_callback (flac_decoder, FLAC_write_callback);
	FLAC__stream_decoder_set_metadata_callback (flac_decoder, FLAC_metadata_callback);
	FLAC__stream_decoder_set_error_callback (flac_decoder, FLAC_error_callback);

	callback_data = g_new0 (CallbackData, 1);
	callback_data->handle = handle;
	callback_data->comment = comment;
	FLAC__stream_decoder_set_client_data (flac_decoder, callback_data);

	/* by default, only the STREAMINFO block is parsed and passed to
	 * the metadata callback.  Here we instruct the decoder to also
	 * pass us the VORBISCOMMENT block if there is one. */
	FLAC__stream_decoder_set_metadata_respond (flac_decoder, FLAC__METADATA_TYPE_VORBIS_COMMENT);

	FLAC__stream_decoder_init (flac_decoder);

	/* this runs the decoding process, calling the callbacks as appropriate */
	if (FLAC__stream_decoder_process_until_end_of_metadata (flac_decoder) == 0) {
		*error_message_return = g_strdup ("Error decoding FLAC file");
		goto out;
	}

	metadata = g_new0 (Metadata, 1);

	assign_metadata_vorbiscomment (metadata, comment);

	metadata->duration = callback_data->duration;

	*error_message_return = NULL;

out:
	g_free (callback_data);

	FLAC__stream_decoder_finish (flac_decoder);
	FLAC__stream_decoder_delete (flac_decoder);
	gnome_vfs_close (handle);

	vorbis_comment_clear (comment);
	g_free (comment);

	return metadata;
}

Metadata *
metadata_load (const char *filename,
               char **error_message_return)
{
	Metadata *m = NULL;
	GnomeVFSFileInfo *info;
	char *escaped;

	g_return_val_if_fail (filename != NULL, NULL);

	escaped = gnome_vfs_escape_path_string (filename);

	info = gnome_vfs_file_info_new ();
	gnome_vfs_get_file_info (escaped, info,
				 GNOME_VFS_FILE_INFO_GET_MIME_TYPE | GNOME_VFS_FILE_INFO_FOLLOW_LINKS);

	if (!strcmp (info->mime_type, "application/x-ogg") ||
	    !strcmp (info->mime_type, "application/ogg"))
		m = assign_metadata_ogg (escaped, error_message_return);
#if HAVE_ID3TAG
	else if (!strcmp (info->mime_type, "audio/x-mp3") ||
	         !strcmp (info->mime_type, "audio/mpeg"))
		m = assign_metadata_mp3 (escaped, info, error_message_return);
#endif /* HAVE_ID3TAG */
	else if (!strcmp (info->mime_type, "application/x-flac") ||
		 !strcmp (info->mime_type, "audio/x-flac"))
		m = assign_metadata_flac (escaped, error_message_return);
#if HAVE_FAAD
	else if (!strcmp (info->mime_type, "application/x-m4a") ||
		 !strcmp (info->mime_type, "audio/x-m4a"))
		m = assign_metadata_mp4 (filename, error_message_return);
#endif /* HAVE_FAAD */
	else
		*error_message_return = g_strdup ("Unknown format");

	if (m != NULL) {
		ensure_track_number (m, filename);

		m->mime_type = g_strdup (info->mime_type);
		m->mtime = info->mtime;
	}

	gnome_vfs_file_info_unref (info);

	g_free (escaped);

	return m;
}

void
metadata_free (Metadata *metadata)
{
	g_return_if_fail (metadata != NULL);

	if (metadata->artists)
		g_strfreev (metadata->artists);
	if (metadata->performers)
		g_strfreev (metadata->performers);

	g_free (metadata->title);
	g_free (metadata->album);
	g_free (metadata->year);
	g_free (metadata->mime_type);

	g_object_unref (metadata->album_art);

	g_free (metadata);
}

const char *
metadata_get_title (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->title;
}

const char *
metadata_get_artist (Metadata *metadata, int index)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->artists[index];
}

int
metadata_get_artist_count (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->artists_count;
}

const char *
metadata_get_performer (Metadata *metadata, int index)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->performers[index];
}

int
metadata_get_performer_count (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->performers_count;
}

const char *
metadata_get_album (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->album;
}

GdkPixbuf *
metadata_get_album_art (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	if (metadata->album_art != NULL)
		return g_object_ref (metadata->album_art);
	else
		return NULL;
}

int
metadata_get_track_number (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->track_number;
}

int
metadata_get_total_tracks (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->total_tracks;
}

int
metadata_get_disc_number (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->disc_number;
}

const char *
metadata_get_year (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->year;
}

int
metadata_get_duration (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->duration;
}

const char *
metadata_get_mime_type (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->mime_type;
}

int
metadata_get_mtime (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->mtime;
}

double
metadata_get_gain (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->gain;
}

double
metadata_get_peak (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->peak;
}
