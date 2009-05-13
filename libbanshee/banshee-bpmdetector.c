//
// banshee-bpmdetector.c
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

typedef struct BansheeBpmDetector BansheeBpmDetector;

typedef void (* BansheeBpmDetectorFinishedCallback) ();
typedef void (* BansheeBpmDetectorProgressCallback) (double bpm);
typedef void (* BansheeBpmDetectorErrorCallback)    (const gchar *error, const gchar *debug);

// Only analyze 20 seconds of audio per song
#define BPM_DETECT_ANALYSIS_DURATION_MS 20*1000

struct BansheeBpmDetector {
    gboolean is_detecting;

    /*
     * You can run this pipeline on the cmd line with:
     * gst-launch -m filesrc location=/path/to/my.mp3 ! decodebin ! \
     *    audioconvert ! bpmdetect ! fakesink
     */

    GstElement *pipeline;
    GstElement *filesrc;
    GstElement *decodebin;
    GstElement *audioconvert;
    GstElement *bpmdetect;
    GstElement *fakesink;
    
    BansheeBpmDetectorProgressCallback progress_cb;
    BansheeBpmDetectorFinishedCallback finished_cb;
    BansheeBpmDetectorErrorCallback error_cb;
};

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static void
bbd_raise_error (BansheeBpmDetector *detector, const gchar *error, const gchar *debug)
{
    printf ("bpm_detect got error: %s %s\n", error, debug);
    g_return_if_fail (detector != NULL);

    if (detector->error_cb != NULL) {
        detector->error_cb (error, debug);
    }
}

static void
bbd_pipeline_process_tag (const GstTagList *tag_list, const gchar *tag_name, BansheeBpmDetector *detector)
{
    const GValue *value;
    gint value_count;
    double bpm;
    
    g_return_if_fail (detector != NULL);

    if (detector->progress_cb == NULL) {
        return;
    }

    if (strcmp (tag_name, GST_TAG_BEATS_PER_MINUTE)) {
        return;
    }

    value_count = gst_tag_list_get_tag_size (tag_list, tag_name);
    if (value_count < 1) {
        return;
    }
    
    value = gst_tag_list_get_value_index (tag_list, tag_name, 0);
    if (value != NULL && G_VALUE_HOLDS_DOUBLE (value)) {
        bpm = g_value_get_double (value);
        detector->progress_cb (bpm);
    }
}

static gboolean
bbd_pipeline_bus_callback (GstBus *bus, GstMessage *message, gpointer data)
{
    BansheeBpmDetector *detector = (BansheeBpmDetector *)data;

    g_return_val_if_fail (detector != NULL, FALSE);

    switch (GST_MESSAGE_TYPE (message)) {
        case GST_MESSAGE_TAG: {
            GstTagList *tags;
            gst_message_parse_tag (message, &tags);
            if (GST_IS_TAG_LIST (tags)) {
                gst_tag_list_foreach (tags, (GstTagForeachFunc)bbd_pipeline_process_tag, detector);
                gst_tag_list_free (tags);
            }
            break;
        }

        case GST_MESSAGE_ERROR: {
            GError *error;
            gchar *debug;
            
            gst_message_parse_error (message, &error, &debug);
            bbd_raise_error (detector, error->message, debug);
            g_error_free (error);
            g_free (debug);
            
            detector->is_detecting = FALSE;
            break;
        }

        case GST_MESSAGE_EOS: {
            detector->is_detecting = FALSE;
            gst_element_set_state (GST_ELEMENT (detector->pipeline), GST_STATE_NULL);

            if (detector->finished_cb != NULL) {
                detector->finished_cb ();
            }
            break;
        }
        
        default: break;
    }
    
    return TRUE;
}

static void
bbd_new_decoded_pad(GstElement *decodebin, GstPad *pad, 
    gboolean last, gpointer data)
{
    GstCaps *caps;
    GstStructure *str;
    GstPad *audiopad;
    BansheeBpmDetector *detector = (BansheeBpmDetector *)data;

    g_return_if_fail(detector != NULL);

    audiopad = gst_element_get_pad(detector->audioconvert, "sink");
    
    if(GST_PAD_IS_LINKED(audiopad)) {
        g_object_unref(audiopad);
        return;
    }

    caps = gst_pad_get_caps(pad);
    str = gst_caps_get_structure(caps, 0);
    
    if(!g_strrstr(gst_structure_get_name(str), "audio")) {
        gst_caps_unref(caps);
        gst_object_unref(audiopad);
        return;
    }
   
    gst_caps_unref(caps);
    gst_pad_link(pad, audiopad);
}

static gboolean
bbd_pipeline_construct (BansheeBpmDetector *detector)
{
    g_return_val_if_fail (detector != NULL, FALSE);

    if (detector->pipeline != NULL) {
        return TRUE;
    }
        
    detector->pipeline = gst_pipeline_new ("pipeline");
    if (detector->pipeline == NULL) {
        bbd_raise_error (detector, _("Could not create pipeline"), NULL);
        return FALSE;
    }

    detector->filesrc = gst_element_factory_make ("filesrc", "filesrc");
    if (detector->filesrc == NULL) {
        bbd_raise_error (detector, _("Could not create filesrc element"), NULL);
        return FALSE;
    }
  
    detector->decodebin = gst_element_factory_make ("decodebin", "decodebin");
    if (detector->decodebin == NULL) {
        bbd_raise_error (detector, _("Could not create decodebin plugin"), NULL);
        return FALSE;
    }

    detector->audioconvert = gst_element_factory_make ("audioconvert", "audioconvert");
    if (detector->audioconvert == NULL) {
        bbd_raise_error (detector, _("Could not create audioconvert plugin"), NULL);
        return FALSE;
    }

    detector->bpmdetect = gst_element_factory_make ("bpmdetect", "bpmdetect");
    if (detector->bpmdetect == NULL) {
        bbd_raise_error (detector, _("Could not create bpmdetect plugin"), NULL);
        return FALSE;
    }
    
    detector->fakesink = gst_element_factory_make ("fakesink", "bpmfakesink");
    if (detector->fakesink == NULL) {
        bbd_raise_error (detector, _("Could not create fakesink plugin"), NULL);
        return FALSE;
    }

    gst_bin_add_many (GST_BIN (detector->pipeline),
        detector->filesrc, detector->decodebin, detector->audioconvert,
        detector->bpmdetect, detector->fakesink, NULL);

    if (!gst_element_link (detector->filesrc, detector->decodebin)) {
        bbd_raise_error (detector, _("Could not link pipeline elements"), NULL);
        return FALSE;
    }

    // decodebin and audioconvert are linked dynamically when the decodebin creates a new pad
    g_signal_connect(detector->decodebin, "new-decoded-pad", 
        G_CALLBACK(bbd_new_decoded_pad), detector);

    if (!gst_element_link_many (detector->audioconvert, detector->bpmdetect, detector->fakesink, NULL)) {
        bbd_raise_error (detector, _("Could not link pipeline elements"), NULL);
        return FALSE;
    }
        
    gst_bus_add_watch (gst_pipeline_get_bus (GST_PIPELINE (detector->pipeline)), bbd_pipeline_bus_callback, detector);

    return TRUE;
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

BansheeBpmDetector *
bbd_new ()
{
    return g_new0 (BansheeBpmDetector, 1);
}

void 
bbd_cancel (BansheeBpmDetector *detector)
{
    g_return_if_fail (detector != NULL);
    
    if (detector->pipeline != NULL && GST_IS_ELEMENT (detector->pipeline)) {
        gst_element_set_state (GST_ELEMENT (detector->pipeline), GST_STATE_NULL);
        gst_object_unref (GST_OBJECT (detector->pipeline));
        detector->pipeline = NULL;
    }
}

void
bbd_destroy (BansheeBpmDetector *detector)
{
    g_return_if_fail (detector != NULL);
    
    bbd_cancel (detector);
    
    g_free (detector);
    detector = NULL;
}

gboolean
bbd_process_file (BansheeBpmDetector *detector, const gchar *path)
{
    //static GstFormat format = GST_FORMAT_TIME;
    //gint64 duration, duration_ms, start_ms, end_ms;

    g_return_val_if_fail (detector != NULL, FALSE);

    if (!bbd_pipeline_construct (detector)) {
        return FALSE;
    }
    
    detector->is_detecting = TRUE;
    gst_element_set_state (detector->fakesink, GST_STATE_NULL);
    g_object_set (G_OBJECT (detector->filesrc), "location", path, NULL);

    // TODO listen for transition to STATE_PLAYING, then
    // Determine how long the file is, and set the detector to base its analysis off the middle 30 seconds of the song

    /*if (gst_element_query_duration (detector->fakesink, &format, &duration)) {
        duration_ms = duration / GST_MSECOND;

        start_ms = CLAMP((duration_ms / 2) - (BPM_DETECT_ANALYSIS_DURATION_MS/2), 0, duration_ms);
        end_ms   = CLAMP(start_ms + BPM_DETECT_ANALYSIS_DURATION_MS, start_ms, duration_ms);
        printf("Analyzing song %s starting at %d ending at %d\n", path, start_ms/1000, end_ms/1000);

        if (gst_element_seek (detector->fakesink, 1.0, 
            //GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_SKIP | GST_SEEK_FLAG_KEY_UNIT,
            GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_KEY_UNIT,
            GST_SEEK_TYPE_SET, start_ms * GST_MSECOND, 
            GST_SEEK_TYPE_SET, end_ms * GST_MSECOND))
        {
        }
    }*/

    gst_element_set_state (detector->pipeline, GST_STATE_PLAYING);
    return TRUE;
}

void
bbd_set_progress_callback (BansheeBpmDetector *detector, BansheeBpmDetectorProgressCallback cb)
{
    g_return_if_fail (detector != NULL);
    detector->progress_cb = cb;
}

void
bbd_set_finished_callback (BansheeBpmDetector *detector, BansheeBpmDetectorFinishedCallback cb)
{
    g_return_if_fail (detector != NULL);
    detector->finished_cb = cb;
}

void
bbd_set_error_callback (BansheeBpmDetector *detector, BansheeBpmDetectorErrorCallback cb)
{
    g_return_if_fail (detector != NULL);
    detector->error_cb = cb;
}

gboolean
bbd_get_is_detecting (BansheeBpmDetector *detector)
{
    g_return_val_if_fail (detector != NULL, FALSE);
    return detector->is_detecting;
}
