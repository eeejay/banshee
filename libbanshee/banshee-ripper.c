//
// banshee-ripper.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <string.h>
#include <glib/gi18n.h>

#include "banshee-gst.h"
#include "banshee-tagger.h"

typedef struct BansheeRipper BansheeRipper;

typedef void (* BansheeRipperFinishedCallback) (BansheeRipper *ripper);
typedef void (* BansheeRipperProgressCallback) (BansheeRipper *ripper, gint msec, gpointer user_info);
typedef void (* BansheeRipperErrorCallback)    (BansheeRipper *ripper, const gchar *error, const gchar *debug);

struct BansheeRipper {
    gboolean is_ripping;
    guint iterate_timeout_id;
    
    gchar *device;
    gint paranoia_mode;
    const gchar *output_uri;
    gchar *encoder_pipeline;
    
    GstElement *pipeline;
    GstElement *cddasrc;
    GstElement *encoder;
    GstElement *filesink;
    
    GstFormat track_format;
    
    BansheeRipperProgressCallback progress_cb;
    BansheeRipperFinishedCallback finished_cb;
    BansheeRipperErrorCallback error_cb;
};

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static void
br_raise_error (BansheeRipper *ripper, const gchar *error, const gchar *debug)
{
    g_return_if_fail (ripper != NULL);
    g_return_if_fail (ripper->error_cb != NULL);
    
    if (ripper->error_cb != NULL) {
        ripper->error_cb (ripper, error, debug);
    }
}

static gboolean
br_iterate_timeout (BansheeRipper *ripper)
{
    GstFormat format = GST_FORMAT_TIME;
    GstState state;
    gint64 position;
    
    g_return_val_if_fail (ripper != NULL, FALSE);

    gst_element_get_state (ripper->pipeline, &state, NULL, 0);
    if (state != GST_STATE_PLAYING) {
        return TRUE;
    }

    if (!gst_element_query_position (ripper->cddasrc, &format, &position)) {
        return TRUE;
    }
    
    if (ripper->progress_cb != NULL) {
        ripper->progress_cb (ripper, position / GST_MSECOND, NULL);
    }

    return TRUE;
}

static void
br_start_iterate_timeout (BansheeRipper *ripper)
{
    g_return_if_fail (ripper != NULL);

    if (ripper->iterate_timeout_id != 0) {
        return;
    }
    
    ripper->iterate_timeout_id = g_timeout_add (200, (GSourceFunc)br_iterate_timeout, ripper);
}

static void
br_stop_iterate_timeout (BansheeRipper *ripper)
{
    g_return_if_fail (ripper != NULL);
    
    if (ripper->iterate_timeout_id == 0) {
        return;
    }
    
    g_source_remove (ripper->iterate_timeout_id);
    ripper->iterate_timeout_id = 0;
}

static gboolean
br_pipeline_bus_callback (GstBus *bus, GstMessage *message, gpointer data)
{
    BansheeRipper *ripper = (BansheeRipper *)data;

    g_return_val_if_fail (ripper != NULL, FALSE);

    switch (GST_MESSAGE_TYPE (message)) {
        case GST_MESSAGE_ERROR: {
            GError *error;
            gchar *debug;
            
            if (ripper->error_cb != NULL) {
                gst_message_parse_error (message, &error, &debug);
                br_raise_error (ripper, error->message, debug);
                g_error_free (error);
                g_free (debug);
            }
            
            ripper->is_ripping = FALSE;
            br_stop_iterate_timeout (ripper);
            break;
        }
            
        case GST_MESSAGE_EOS: {
            gst_element_set_state (GST_ELEMENT (ripper->pipeline), GST_STATE_NULL);
            
            ripper->is_ripping = FALSE;
            br_stop_iterate_timeout (ripper);
            
            if (ripper->finished_cb != NULL) {
                ripper->finished_cb (ripper);
            }
            break;
        }
        
        default: break;
    }
    
    return TRUE;
}

static GstElement *
br_pipeline_build_encoder (const gchar *pipeline, GError **error_out)
{
    GstElement *encoder;
    GError *error = NULL;
   
    encoder = gst_parse_bin_from_description (pipeline, TRUE, &error);
    
    if (error != NULL) {
        if (error_out != NULL) {
            *error_out = error;
        }
        return NULL;
    }
    
    return encoder;
}

static gboolean
br_pipeline_construct (BansheeRipper *ripper)
{
    GstElement *queue;
    GError *error = NULL;
    
    g_return_val_if_fail (ripper != NULL, FALSE);
        
    ripper->pipeline = gst_pipeline_new ("pipeline");
    if (ripper->pipeline == NULL) {
        br_raise_error (ripper, _("Could not create pipeline"), NULL);
        return FALSE;
    }

    ripper->cddasrc = gst_element_make_from_uri (GST_URI_SRC, "cdda://1", "cddasrc");
    if (ripper->cddasrc == NULL) {
        br_raise_error (ripper, _("Could not initialize element from cdda URI"), NULL);
        return FALSE;
    }
  
    g_object_set (G_OBJECT (ripper->cddasrc), "device", ripper->device, NULL);
    
    if (g_object_class_find_property (G_OBJECT_GET_CLASS (ripper->cddasrc), "paranoia-mode")) {
        g_object_set (G_OBJECT (ripper->cddasrc), "paranoia-mode", ripper->paranoia_mode, NULL);
    }
    
    ripper->track_format = gst_format_get_by_nick ("track");
    
    ripper->encoder = br_pipeline_build_encoder (ripper->encoder_pipeline, &error);
    if (ripper->encoder == NULL) {
        br_raise_error (ripper, _("Could not create encoder pipeline"), error->message);
        return FALSE;
    }
    
    queue = gst_element_factory_make ("queue", "queue");
    if (queue == NULL) {
        br_raise_error (ripper, _("Could not create queue plugin"), NULL);
        return FALSE;
    }
    
    g_object_set (G_OBJECT (queue), "max-size-time", 120 * GST_SECOND, NULL);
    
    ripper->filesink = gst_element_factory_make ("filesink", "filesink");
    if (ripper->filesink == NULL) {
        br_raise_error (ripper, _("Could not create filesink plugin"), NULL);
        return FALSE;
    }
    
    gst_bin_add_many (GST_BIN (ripper->pipeline), ripper->cddasrc, queue, ripper->encoder, ripper->filesink, NULL);
        
    if (!gst_element_link_many (ripper->cddasrc, queue, ripper->encoder, ripper->filesink, NULL)) {
        br_raise_error (ripper, _("Could not link pipeline elements"), NULL);
    }
    
    gst_bus_add_watch (gst_pipeline_get_bus (GST_PIPELINE (ripper->pipeline)), br_pipeline_bus_callback, ripper);

    return TRUE;
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

BansheeRipper *
br_new (gchar *device, gint paranoia_mode, gchar *encoder_pipeline)
{
    BansheeRipper *ripper = g_new0 (BansheeRipper, 1);
    
    if (ripper == NULL) {
        return NULL;
    }
        
    ripper->device = g_strdup (device);
    ripper->paranoia_mode = paranoia_mode;
    ripper->encoder_pipeline = g_strdup (encoder_pipeline);

    return ripper;
}

void 
br_cancel (BansheeRipper *ripper)
{
    g_return_if_fail (ripper != NULL);
    
    br_stop_iterate_timeout (ripper);
    
    if (ripper->pipeline != NULL && GST_IS_ELEMENT (ripper->pipeline)) {
        gst_element_set_state (GST_ELEMENT (ripper->pipeline), GST_STATE_NULL);
        gst_object_unref (GST_OBJECT (ripper->pipeline));
        ripper->pipeline = NULL;
    }
}

void
br_destroy (BansheeRipper *ripper)
{
    g_return_if_fail (ripper != NULL);
    
    br_cancel (ripper);
    
    if (ripper->device != NULL) {
        g_free (ripper->device);
    }
    
    if (ripper->encoder_pipeline != NULL) {
        g_free (ripper->encoder_pipeline);
    }
    
    g_free (ripper);
    ripper = NULL;
}

gboolean
br_rip_track (BansheeRipper *ripper, gint track_number, gchar *output_path, 
    GstTagList *tags, gboolean *tagging_supported)
{
    GstIterator *bin_iterator;
    GstElement *bin_element;
    gboolean can_tag = FALSE;
    gboolean iterate_done = FALSE;

    g_return_val_if_fail (ripper != NULL, FALSE);

    if (!br_pipeline_construct (ripper)) {
        return FALSE;
    }
    
    // initialize the pipeline, set the sink output location
    gst_element_set_state (ripper->filesink, GST_STATE_NULL);
    g_object_set (G_OBJECT (ripper->filesink), "location", output_path, NULL);
    
    // find an element to do the tagging and set tag data
    bin_iterator = gst_bin_iterate_all_by_interface (GST_BIN (ripper->encoder), GST_TYPE_TAG_SETTER);
    while (!iterate_done) {
        switch (gst_iterator_next (bin_iterator, (gpointer)&bin_element)) {
            case GST_ITERATOR_OK: {
                GstTagSetter *tag_setter = GST_TAG_SETTER (bin_element);
                if (tag_setter == NULL) {
                    break;
                }
                
                gst_tag_setter_add_tags (tag_setter, GST_TAG_MERGE_REPLACE_ALL,
                    GST_TAG_ENCODER, "Banshee " VERSION,
                    GST_TAG_ENCODER_VERSION, banshee_get_version_number (),
                    NULL);
                    
                if (tags != NULL) {
                    gst_tag_setter_merge_tags (tag_setter, tags, GST_TAG_MERGE_APPEND);
                }
                
                if (banshee_is_debugging ()) {
                    bt_tag_list_dump (gst_tag_setter_get_tag_list (tag_setter));
                }
                    
                can_tag = TRUE;    
                gst_object_unref (bin_element);
                break;
            }
            
            case GST_ITERATOR_RESYNC: {
                gst_iterator_resync (bin_iterator);
                break;
            }
            
            default: {
                iterate_done = TRUE;
                break;
            }
        }
    }
    
    gst_iterator_free (bin_iterator);
    
    // We'll warn the user in the UI if we can't tag the encoded audio files
    *tagging_supported = can_tag;
    
    // Begin the rip
    g_object_set (G_OBJECT (ripper->cddasrc), "track", track_number, NULL);
    gst_element_set_state (ripper->pipeline, GST_STATE_PLAYING);
    br_start_iterate_timeout (ripper);
    
    return TRUE;
}

void
br_set_progress_callback (BansheeRipper *ripper, BansheeRipperProgressCallback cb)
{
    g_return_if_fail (ripper != NULL);
    ripper->progress_cb = cb;
}

void
br_set_finished_callback (BansheeRipper *ripper, BansheeRipperFinishedCallback cb)
{
    g_return_if_fail (ripper != NULL);
    ripper->finished_cb = cb;
}

void
br_set_error_callback (BansheeRipper *ripper, BansheeRipperErrorCallback cb)
{
    g_return_if_fail (ripper != NULL);
    ripper->error_cb = cb;
}

gboolean
br_get_is_ripping (BansheeRipper *ripper)
{
    g_return_val_if_fail (ripper != NULL, FALSE);
    return ripper->is_ripping;
}
