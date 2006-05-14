/***************************************************************************
 *  gst-tagger.c
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
 
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <glib.h>
#include <glib/gstdio.h>

#include <gst/gst.h>

#include "gst-tagger.h"

const char *mimetype_whitelist [] = {
    "audio/",
    "application/",
    NULL
};

const char *mimetype_blacklist [] = {
    "application/xml",
    NULL
};

struct GstTagger {
    GstElement *pipeline;
    GstElement *source;
    GstElement *sink;

    gboolean audio_parsed;
    gboolean processing_stream;
    gboolean error_raised;
    gboolean queried_duration;
    
    guint typefind_id;

    GstTaggerTagFoundCallback tag_found_cb;
    GstTaggerErrorCallback error_cb;
    GstTaggerFinishedCallback finished_cb;
};

static gboolean gst_tagger_bus_callback(GstBus *bus, GstMessage *message, gpointer data);
static void gst_tagger_reset_pipeline(GstTagger *tagger);

static void
gst_tagger_destroy_pipeline(GstTagger *tagger)
{
    if(tagger->pipeline != NULL) {
        GstBus *bus = gst_element_get_bus(tagger->pipeline);
        gst_element_set_state(tagger->pipeline, GST_STATE_NULL);
        gst_bus_set_sync_handler(bus, NULL, NULL);
        gst_object_unref(bus);
        gst_object_unref(tagger->pipeline);
        tagger->pipeline = NULL;
    }
}

static void
gst_tagger_raise_error(GstTagger *tagger, const gchar *error, const gchar *debug)
{
    g_return_if_fail(tagger != NULL);

    tagger->processing_stream = FALSE;
    
    if(!tagger->error_raised && tagger->error_cb != NULL) {
        tagger->error_cb(tagger, error, debug);
    }
    
    tagger->error_raised = TRUE;
}

static void
gst_tagger_raise_finished(GstTagger *tagger)
{    
    if(tagger->finished_cb != NULL) {
        tagger->finished_cb(tagger);
    }
    
    tagger->processing_stream = FALSE;
}

static void
gst_tagger_new_decoded_pad(GstElement *decodebin, GstPad *pad,
    gboolean last, gpointer data)
{
    GstCaps *caps;
    GstStructure *structure;
    GstPad *audiopad;
    GstTagger *tagger = (GstTagger *)data;

    g_return_if_fail(tagger != NULL);

    audiopad = gst_element_get_pad(tagger->sink, "sink");

    if(GST_PAD_IS_LINKED(audiopad)) {
        g_object_unref(audiopad);
        return;
    }

    caps = gst_pad_get_caps(pad);

    if(gst_caps_is_empty(caps) || gst_caps_is_any(caps)) {
        gst_caps_unref(caps);
        gst_object_unref(audiopad);
        gst_tagger_reset_pipeline(tagger);
        return;
    }
    
    structure = gst_caps_get_structure(caps, 0);

    if(!g_strrstr(gst_structure_get_name(structure), "audio")) {
        gst_caps_unref(caps);
        gst_object_unref(audiopad);
        gst_tagger_reset_pipeline(tagger);
        return;
    }

    tagger->audio_parsed = TRUE;

    gst_caps_unref(caps);
    gst_pad_link(pad, audiopad);
}

static void
gst_tagger_typefind(GstElement *typefind, guint probability, GstCaps *caps, GstTagger *tagger)
{
    gint i;
    gboolean allowed = FALSE;
    
    g_return_if_fail(tagger != NULL);

    if(g_signal_handler_is_connected(typefind, tagger->typefind_id)) {
        g_signal_handler_disconnect(typefind, tagger->typefind_id);
    }
    
    tagger->typefind_id = 0;
    
    if(!tagger->processing_stream ||  caps == NULL || gst_caps_get_size(caps) <= 0) {
        return;
    }
    
    const gchar *mime = gst_structure_get_name(gst_caps_get_structure(caps, 0));

    // match against the whitelist
    for(i = 0; mimetype_whitelist[i] != NULL; i++) {
        if(g_str_has_prefix(mime, mimetype_whitelist[i])) {
            allowed = TRUE;
            break;
        }
    }
    
    // handle !allowed and match against the blacklist
    for(i = 0; mimetype_blacklist[i] != NULL; i++) {
        if(!allowed || g_str_has_prefix(mime, mimetype_blacklist[i])) {
            gst_tagger_raise_error(tagger, "Unsupported mimetype", mime);
            return;
        }
    }
     
    // raise a fake 'stream-type' tag event
    if(tagger->tag_found_cb == NULL) {
        return;
    }
    
    GValue *type_value = g_new0(GValue, 1);
    
    g_value_init(type_value, G_TYPE_STRING);
    g_value_set_string(type_value, mime);
    
    tagger->tag_found_cb("stream-type", type_value, tagger);
    
    g_value_unset(type_value);
}

static void
gst_tagger_unknown_type(GstElement *decodebin, GstPad *pad, GstCaps *caps, GstTagger *tagger)
{
    if(gst_caps_get_size(caps) > 0) {
        const gchar *type = gst_structure_get_name(gst_caps_get_structure(caps, 0));
        gchar *message = g_strdup_printf("Unsupported type: %s", type);
        gst_tagger_raise_error(tagger, message, type);
        g_free(message);
    } else {
        gst_tagger_raise_error(tagger, "Unsupported type", NULL);
    }
}

static GstBusSyncReply
gst_tagger_bus_sync_handler(GstBus *bus, GstMessage *message, gpointer userdata)
{
    gst_tagger_bus_callback(bus, message, userdata);
    gst_message_unref(message);
    return GST_BUS_DROP;
}

static void
gst_tagger_reset_pipeline(GstTagger *tagger)
{
}

static gboolean
gst_tagger_build_pipeline(GstTagger *tagger)
{
    GstElement *decodebin;
    GstElement *typefind;
    GstBus *bus;
    
    g_return_val_if_fail(tagger != NULL, FALSE);

    tagger->audio_parsed = FALSE;
    tagger->processing_stream = FALSE;
    tagger->error_raised = FALSE;

    tagger->pipeline = gst_pipeline_new("pipeline");
    if(tagger->pipeline == NULL) {
        return FALSE;
    }
    
    tagger->source = gst_element_factory_make("gnomevfssrc", "gnomevfssrc");
    if(tagger->source == NULL) {
        return FALSE;
    }
    
    decodebin = gst_element_factory_make("decodebin", "decodebin");
    if(decodebin == NULL) {
        return FALSE;
    }
    
    tagger->sink = gst_element_factory_make("fakesink", "fakesink");
    if(tagger->sink == NULL) {
        return FALSE;
    }

    gst_bin_add_many(GST_BIN(tagger->pipeline), tagger->source, decodebin, tagger->sink, NULL);
    gst_element_link(tagger->source, decodebin);

    g_signal_connect(decodebin, "new-decoded-pad", G_CALLBACK(gst_tagger_new_decoded_pad), tagger);
    g_signal_connect(decodebin, "unknown-type", G_CALLBACK(gst_tagger_unknown_type), tagger);
    
    bus = gst_element_get_bus(tagger->pipeline);
    gst_bus_set_sync_handler(bus, gst_tagger_bus_sync_handler, tagger);
    gst_object_unref(bus);    

    typefind = gst_bin_get_by_name(GST_BIN(decodebin), "typefind");
    if(typefind != NULL) {
        tagger->typefind_id = g_signal_connect(typefind, "have-type",
            G_CALLBACK(gst_tagger_typefind), tagger);
    }

    return TRUE;
}

static gboolean
gst_tagger_bus_callback(GstBus *bus, GstMessage *message, gpointer data)
{
    GstTagger *tagger = (GstTagger *)data;

    g_return_val_if_fail(tagger != NULL, FALSE);
    
    switch(GST_MESSAGE_TYPE(message)) {
        case GST_MESSAGE_ERROR: {
            GError *error;
            gchar *debug;

            gst_message_parse_error(message, &error, &debug);
            gst_tagger_raise_error(tagger, error->message, debug);
            g_error_free(error);
            g_free(debug);

            break;
        }
        
        case GST_MESSAGE_EOS:
            gst_tagger_raise_finished(tagger);
            break;

        case GST_MESSAGE_STATE_CHANGED: {
            GstState old, new, pending;
            GstFormat format = GST_FORMAT_TIME;
            gint64 duration;
            GValue *duration_value;

            gst_message_parse_state_changed(message, &old, &new, &pending);

            if(!tagger->queried_duration) {
                if(gst_element_query_duration(tagger->pipeline, &format, &duration)) {
                    duration_value = g_new0(GValue, 1);
                    g_value_init(duration_value, G_TYPE_UINT);
                    g_value_set_uint(duration_value, (guint)(duration / GST_MSECOND));
                    
                    // raise a fake 'duration' tag event
                    if(tagger->tag_found_cb != NULL) {
                        tagger->tag_found_cb("duration", duration_value, tagger);
                    }

                    g_value_unset(duration_value);
                    tagger->queried_duration = TRUE;
                }
            }

            if(old == GST_STATE_READY && new == GST_STATE_PAUSED && pending == GST_STATE_PLAYING) {
                gst_tagger_raise_finished(tagger);
            }
            break;
        }

        case GST_MESSAGE_TAG: {
            GstTagList *tags;
            GstTaggerInvoke invoke;

            if(tagger->tag_found_cb == NULL) {
                break;
            }

            memset(&invoke, 0, sizeof(GstTaggerInvoke));

            if(GST_MESSAGE_TYPE(message) != GST_MESSAGE_TAG) {
                break;
            }
            
            invoke.callback = tagger->tag_found_cb;
            invoke.user_data = tagger;

            gst_message_parse_tag(message, &tags);
            
            if(GST_IS_TAG_LIST(tags)) {
                gst_tag_list_foreach(tags, (GstTagForeachFunc)gst_tagger_process_tag, &invoke);
                gst_tag_list_free(tags);
            }
            
            break;
        }
        
        case GST_MESSAGE_APPLICATION:
            gst_tagger_destroy_pipeline(tagger);
            gst_tagger_raise_finished(tagger);
            break;
    
        default:
            break;
    }
    
    return FALSE;
}

GstTagger *
gst_tagger_new()
{
    GstTagger *tagger = g_new0(GstTagger, 1);

    if(tagger == NULL) {
        return NULL;
    }

    tagger->pipeline = NULL;
    tagger->source = NULL;
    tagger->sink = NULL;
    
    tagger->typefind_id = 0;

    tagger->audio_parsed = FALSE;
    tagger->processing_stream = FALSE;
    tagger->error_raised = FALSE;
    tagger->queried_duration = FALSE;
    
    tagger->tag_found_cb = NULL;
    tagger->error_cb = NULL;
    tagger->finished_cb = NULL;

    return tagger;
}

void
gst_tagger_free(GstTagger *tagger)
{
    g_return_if_fail(tagger != NULL);

    gst_tagger_destroy_pipeline(tagger);    
    g_free(tagger);
}

static gboolean
_gst_tagger_process_uri(GstTagger *tagger, const gchar *uri, gboolean block)
{
    GstStateChangeReturn state_return;
    gint timeout_cycle;

    g_return_val_if_fail(tagger != NULL, FALSE);
    g_return_val_if_fail(uri != NULL, FALSE);
   
    if(tagger->pipeline != NULL) {
        gst_tagger_destroy_pipeline(tagger);
    }
    
    if(!gst_tagger_build_pipeline(tagger)) {
        return FALSE;
    }
    
    g_object_set(tagger->source, "location", uri, NULL);

    tagger->audio_parsed = FALSE;
    tagger->processing_stream = TRUE;
    tagger->error_raised = FALSE;
    tagger->queried_duration = FALSE;
    
    state_return = gst_element_set_state(tagger->pipeline, GST_STATE_PAUSED);
    
    for(timeout_cycle = 0; timeout_cycle < 5; timeout_cycle++) {
        GstState current_state;
        if(state_return != GST_STATE_CHANGE_ASYNC || !tagger->processing_stream) {
            break;
        }
        
        state_return = gst_element_get_state(tagger->pipeline, 
            &current_state, NULL, GST_SECOND * 1);
    }
    
    if(state_return == GST_STATE_CHANGE_ASYNC && !tagger->audio_parsed) {
        gst_tagger_raise_error(tagger, "State change failed", NULL);
    } else if(state_return != GST_STATE_CHANGE_FAILURE) {
        GstBus *message_bus = gst_element_get_bus(tagger->pipeline);
        if(message_bus != NULL) {
            gst_bus_post(message_bus, gst_message_new_application(GST_OBJECT(tagger->pipeline), NULL));
            gst_object_unref(message_bus);
        }
        
        while(tagger->processing_stream);
    }
    
    gst_tagger_destroy_pipeline(tagger);
    
    return tagger->audio_parsed;
}

void
gst_tagger_process_tag(const GstTagList *tag_list, const gchar *tag_name, GstTaggerInvoke *invoke)
{
    const GValue *value;
    gint value_count;

    value_count = gst_tag_list_get_tag_size(tag_list, tag_name);
    if(value_count < 1) {
        return;
    }
    
    value = gst_tag_list_get_value_index(tag_list, tag_name, 0);

    if(invoke != NULL && invoke->callback != NULL) {
        invoke->callback(tag_name, value, invoke->user_data);
    }
}

gboolean
gst_tagger_process_uri(GstTagger *tagger, const gchar *uri)
{
    return _gst_tagger_process_uri(tagger, uri, FALSE);
}

gboolean
gst_tagger_process_uri_and_block(GstTagger *tagger, const gchar *uri)
{
    return _gst_tagger_process_uri(tagger, uri, TRUE);
}

void
gst_tagger_set_tag_found_callback(GstTagger *tagger, GstTaggerTagFoundCallback cb)
{
    g_return_if_fail(tagger != NULL);
    tagger->tag_found_cb = cb;
}

void
gst_tagger_set_error_callback(GstTagger *tagger, GstTaggerErrorCallback cb)
{
    g_return_if_fail(tagger != NULL);
    tagger->error_cb = cb;
}

void
gst_tagger_set_finished_callback(GstTagger *tagger, GstTaggerFinishedCallback cb)
{
    g_return_if_fail(tagger != NULL);
    tagger->finished_cb = cb;
}

gboolean
gst_tagger_check_audio_parsed(GstTagger *tagger)
{
    g_return_val_if_fail(tagger != NULL, FALSE);
    return tagger->audio_parsed;
}
