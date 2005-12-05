/***************************************************************************
 *  gst-encode.c
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
 
#include <gst/gst.h>
#include <gst/gconf/gconf.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>
#include <glib.h>
#include <glib/gi18n.h>
#include <glib/gstdio.h>

#include <libgnomevfs/gnome-vfs.h>

#include "gst-transcode.h"
#include "gst-misc.h"

static GstElement *
gst_file_encoder_build_encoder(const gchar *encoder_pipeline)
{
    GstElement *encoder = NULL;
    gchar *pipeline;
    
    pipeline = g_strdup_printf("audioconvert ! %s", encoder_pipeline);
    encoder = gst_gconf_render_bin_from_description(pipeline);
    g_free(pipeline);
    
    return encoder;
}    

GstFileEncoder *
gst_file_encoder_new()
{
    GstFileEncoder *encoder;
    
    gstreamer_initialize();
    
    encoder = g_new0(GstFileEncoder, 1);
    encoder->cancel = FALSE;
    encoder->error = NULL;
    
    return encoder;
}

void
gst_file_encoder_free(GstFileEncoder *encoder)
{
    if(encoder == NULL)
        return;
    
    encoder->cancel = TRUE;
    
    if(encoder->error != NULL)
       g_free(encoder->error);
    encoder->error = NULL;
    
    g_free(encoder);
    encoder = NULL;
}

static gboolean
gst_file_encoder_gvfs_allow_overwrite_cb(GstElement *element, gpointer filename,
    gpointer user_data)
{
    return TRUE;
}

static void 
gst_error_callback(GstElement *elem, GstElement *arg1, 
    GError *error, gchar *str, GstFileEncoder *encoder)
{
    encoder->error = g_strdup(str);
}

static GstElement *
gst_file_encoder_create_pipeline(GstFileEncoder *encoder, 
    const char *input_file, const char *output_file, 
    const gchar *encoder_pipeline, GstElement **out_sink)
{
    GstElement *pipeline;
    GstElement *source_elem;
    GstElement *decoder_elem;
    GstElement *encoder_elem;
    GstElement *sink_elem;

    if(encoder == NULL)
        return NULL;
    
    pipeline = gst_pipeline_new("pipeline");

    source_elem = gst_element_factory_make("gnomevfssrc", "source");
    if(source_elem == NULL) {
        encoder->error = g_strdup(_("Could not create 'gnomevfssrc' plugin"));
        return NULL;
    }

    decoder_elem = gst_element_factory_make("spider", "spider");
    if(decoder_elem == NULL) {
        encoder->error = g_strdup(_("Could not create 'spider' plugin"));
        return NULL;
    }

    encoder_elem = gst_file_encoder_build_encoder(encoder_pipeline);
    if(encoder_elem == NULL) {
         encoder->error = g_strdup_printf(
           _("Could not create encoding pipeline: %s"), encoder_pipeline);
         return NULL;
    }

    sink_elem = gst_element_factory_make("gnomevfssink", "sink");
    if(sink_elem == NULL) {
        encoder->error = g_strdup(_("Could not create 'filesink' plugin"));
        return NULL;
    }
    
    g_signal_connect(G_OBJECT(sink_elem), "allow-overwrite",
        G_CALLBACK(gst_file_encoder_gvfs_allow_overwrite_cb), encoder);
    
    gst_bin_add_many(GST_BIN(pipeline), 
        source_elem, 
        decoder_elem, 
        encoder_elem, 
        sink_elem, NULL);
    
    gst_element_link_many(source_elem,
        decoder_elem,
        encoder_elem, 
        sink_elem, NULL);
    
    g_signal_connect(pipeline, "error", G_CALLBACK(gst_error_callback),
        encoder);
    
    g_object_set(source_elem, "location", input_file, NULL);
    g_object_set(sink_elem, "location", output_file, NULL);

    *out_sink = sink_elem;

    return pipeline;
}

gboolean 
gst_file_encoder_encode_file(GstFileEncoder *encoder, const gchar *input_file, 
    const gchar *output_file, const gchar *encoder_pipeline, 
    GstFileEncoderProgressCallback progress_cb)
{
    GstElement *pipeline;
    GstElement *sink = NULL;
    GstFormat format = GST_FORMAT_BYTES;
    gint64 position, total;
    gdouble last_fraction = 0.0, fraction = 0.0;
    GnomeVFSFileInfo fileinfo;

    if(encoder == NULL)
        return FALSE;
    
    if(encoder->error != NULL) {
        g_free(encoder->error);
        encoder->error = NULL;
    }
    
    encoder->cancel = FALSE;
    
    pipeline = gst_file_encoder_create_pipeline(encoder, input_file,
        output_file, encoder_pipeline, &sink);
    
    if(pipeline == NULL)
        return FALSE;
    
    gst_element_set_state(GST_ELEMENT(pipeline), GST_STATE_PLAYING);
    
    while(gst_bin_iterate(GST_BIN(pipeline))) {
        if(encoder->cancel == TRUE || encoder->error != NULL)
            break;
        
        gst_element_query(sink, GST_QUERY_POSITION, &format, &position);
        gst_element_query(sink, GST_QUERY_TOTAL, &format, &total);
    
        if(total > position && total != 0 && progress_cb != NULL) {
            fraction = (double)position / (double)total;
            if(fraction - 0.01 > last_fraction) {
                last_fraction = fraction;
                progress_cb(encoder, fraction);
            }
        }
    }
    
    gst_element_set_state(GST_ELEMENT(pipeline), GST_STATE_NULL);
    g_object_unref(G_OBJECT(pipeline));

    if(encoder->error == NULL) {
        if(gnome_vfs_get_file_info(output_file, &fileinfo, 
            GNOME_VFS_FILE_INFO_DEFAULT) == GNOME_VFS_OK) {
            if(fileinfo.size < 100) {
                encoder->error = g_strdup(_("No decoder could be found "
                                "for source format."));
                g_remove(output_file);
            }
        } else {
            encoder->error = g_strdup(_("Could not stat encoded file"));
        }
    }
    
    encoder->cancel = FALSE;
    
    return encoder->error == NULL;
}

const gchar *
gst_file_encoder_get_error(GstFileEncoder *encoder)
{
    if(encoder == NULL)
        return NULL;
    
    return encoder->error;
}

void 
gst_file_encoder_encode_cancel(GstFileEncoder *encoder)
{
    if(encoder == NULL)
        return;
    
    encoder->cancel = TRUE;
}
