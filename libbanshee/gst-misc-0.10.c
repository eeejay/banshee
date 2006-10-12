/* ex: set ts=4: */
/***************************************************************************
 *  gst-misc-0.10.c
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

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>

#include <gst/gst.h>

#include "gst-mbtrm.h"

static gboolean gstreamer_initialized = FALSE;

static gboolean
gst_mbtrm_register_elements(GstPlugin *plugin)
{
    return gst_element_register(plugin, "mbtrm",
        GST_RANK_NONE, GST_TYPE_MBTRM);
}

static GstPluginDesc gst_mbtrm_plugin_desc = {
    GST_VERSION_MAJOR,
    GST_VERSION_MINOR,
    "mbtrm",
    "Private MusicBrainz TRM element",
    gst_mbtrm_register_elements,
    "0.10.10",
    "LGPL",
    "libbanshee",
    "Banshee",
    "http://banshee-project.org/",
    GST_PADDING_INIT
};

void gstreamer_initialize()
{
    if(gstreamer_initialized) {
        return;
    }

    gst_init(NULL, NULL);
    _gst_plugin_register_static(&gst_mbtrm_plugin_desc);
    
    gstreamer_initialized = TRUE;
}

gboolean
gstreamer_test_encoder(gchar *encoder_pipeline)
{
    GstElement *element = NULL;
    gchar *pipeline;
    GError *error = NULL;
    
    pipeline = g_strdup_printf("audioconvert ! %s", encoder_pipeline);
    element = gst_parse_launch(pipeline, &error);
    g_free(pipeline);
    
    if(element != NULL) {
        gst_object_unref(GST_OBJECT(element));
    }
    
    return error == NULL;
}

gboolean 
gstreamer_test_pipeline(gchar *pipeline)
{
    GstElement *element = NULL;
    GError *error = NULL;
    
    element = gst_parse_launch(pipeline, &error);
    
    if(element != NULL) {
        gst_object_unref(GST_OBJECT(element));
    }
    
    return error == NULL;
}

static void
gst_typefind_type_found_callback(GstElement *typefind, guint probability, 
    GstCaps *caps, gchar **type)
{
    *type = gst_caps_to_string(caps);    
}

gchar *
gstreamer_detect_mimetype(const gchar *uri)
{
    return NULL;
    /*GstElement *pipeline;
    GstElement *source;
    GstElement *typefind;
    GstElement *fakesink;
    gchar *mimetype = NULL;

    pipeline = gst_pipeline_new("new");
        
    source = gst_element_factory_make("gnomevfssrc", "source");
    typefind = gst_element_factory_make("typefind", "typefind");
    fakesink = gst_element_factory_make("fakesink", "fakesink");

    if(source == NULL || typefind == NULL) {
        gst_object_unref(pipeline);
        return NULL;
    }

    g_object_set(source, "location", uri, NULL);
    g_signal_connect(typefind, "have-type", 
        G_CALLBACK(gst_typefind_type_found_callback), &mimetype);

    gst_bin_add_many(GST_BIN(pipeline), source, typefind, fakesink, NULL);
    gst_element_link(source, typefind);
    gst_element_link(typefind, fakesink);
    
    if(gst_element_set_state(pipeline, GST_STATE_PLAYING) == GST_STATE_CHANGE_ASYNC) {
        if (gst_element_get_state(pipeline, NULL, NULL, -1) == GST_STATE_CHANGE_FAILURE) {
            gst_object_unref(pipeline);
            return NULL;
        }
    }

    while((mimetype == NULL) && (pipeline->current_state == GST_STATE_PLAYING));
    
    gst_element_set_state(pipeline, GST_STATE_NULL);
    gst_object_unref(pipeline);
        
    return mimetype;*/
}
