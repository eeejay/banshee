/* ex: set ts=4: */
/***************************************************************************
 *  cd-rip.c
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

#include <gst/gst.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>
#include <string.h>
#include <glib.h>
#include <glib/gi18n.h>

#include <gst/gst.h>
#include <gst/gconf/gconf.h>
#include <gst/tag/tag.h>

#include "gst-cd-rip.h"
#include "gst-misc.h"

static GstElement *
gst_cd_ripper_build_encoder(GstCdRipper *ripper)
{
    GstElement *encoder = NULL;
    gchar *pipeline;
    
    pipeline = g_strdup_printf("audioconvert ! %s", ripper->encoder_pipeline);
    encoder = gst_gconf_render_bin_from_description(pipeline);
    g_free(pipeline);
    
    return encoder;
}    

static gboolean
gst_cd_ripper_gvfs_allow_overwrite_cb(GstElement *element, gpointer filename,
    gpointer user_data)
{
    return TRUE;
}

static void 
gst_cd_ripper_gst_error_cb(GstElement *elem, GstElement *arg1, GError *error, 
    gchar *str, GstCdRipper *ripper)
{
    ripper->error = g_strdup(str);
}

gboolean
gst_cd_ripper_build_pipeline(GstCdRipper *ripper)
{
    if(ripper == NULL)
        return FALSE;
        
    ripper->pipeline = gst_pipeline_new("pipeline");
    if(ripper->pipeline == NULL) {
        ripper->error = g_strdup(_("Could not create pipeline"));
        return FALSE;
    }

    g_signal_connect(G_OBJECT(ripper->pipeline), "error", 
        G_CALLBACK(gst_cd_ripper_gst_error_cb), ripper);

    ripper->cdparanoia = gst_element_factory_make("cdparanoia", "cdparanoia");
    if(ripper->cdparanoia == NULL) {
        ripper->error = g_strdup(_("Could not initialize cdparanoia"));
        return FALSE;
    }
  
    g_object_set(G_OBJECT(ripper->cdparanoia), "device", ripper->device, NULL);
    g_object_set(G_OBJECT(ripper->cdparanoia), "paranoia-mode", 
        ripper->paranoia_mode, NULL);
    
    ripper->track_format = gst_format_get_by_nick("track");
    ripper->source_pad = gst_element_get_pad(ripper->cdparanoia, "src");
    
    ripper->encoder = gst_cd_ripper_build_encoder(ripper);
    if(ripper->encoder == NULL) {
        ripper->error = g_strdup(_("Could not create encoder pipeline"));
        return FALSE;
    }
    
    ripper->filesink = gst_element_factory_make("gnomevfssink", "gnomevfssink");
    if(ripper->filesink == NULL) {
        ripper->error = g_strdup(_("Could not create GNOME VFS output plugin"));
        return FALSE;
    }
    
    g_signal_connect(G_OBJECT(ripper->filesink), "allow-overwrite",
        G_CALLBACK(gst_cd_ripper_gvfs_allow_overwrite_cb), ripper);
    
    gst_bin_add_many(GST_BIN(ripper->pipeline),
        ripper->cdparanoia,
        ripper->encoder,
        ripper->filesink,
        NULL);
        
    if(!gst_element_link_many(ripper->cdparanoia, ripper->encoder,
        ripper->filesink, NULL)) {
        ripper->error = g_strdup(_("Could not link pipeline elements"));
        return FALSE;
    }

    return TRUE;
}

/* Public Methods */

GstCdRipper *
gst_cd_ripper_new(gchar *device, gint paranoia_mode, gchar *encoder_pipeline)
{
    GstCdRipper *ripper = g_new0(GstCdRipper, 1);
    
    if(ripper == NULL)
        return NULL;
        
    gstreamer_initialize();
        
    ripper->device = g_strdup(device);
    ripper->paranoia_mode = paranoia_mode;
    ripper->encoder_pipeline = g_strdup(encoder_pipeline);
    
    ripper->cancel = FALSE;
    
    ripper->pipeline = NULL;
    ripper->cdparanoia = NULL;
    ripper->encoder = NULL;
    ripper->filesink = NULL;
    
    ripper->track_format = 0;
    ripper->source_pad = NULL;
    
    ripper->progress_callback = NULL;
    
    ripper->error = NULL;
    
    return ripper;
}

void
gst_cd_ripper_free(GstCdRipper *ripper)
{
    if(ripper == NULL)
        return;
        
    g_free(ripper->device);
    g_free(ripper->encoder_pipeline);
    
    g_free(ripper);
}

gboolean
gst_cd_ripper_rip_track(GstCdRipper *ripper, gchar *uri, gint track_number, 
    gchar *md_artist, gchar *md_album, gchar *md_title, gchar *md_genre,
    gint md_track_number, gint md_track_count, gpointer user_info)
{
    GstEvent *event;
    static GstFormat format = GST_FORMAT_TIME;
    gint64 nanoseconds;
    gint seconds;
    GList *elements = NULL;
    GList *element = NULL;
    gboolean can_tag = FALSE;
    gboolean has_started = FALSE;
    
    if(ripper == NULL || uri == NULL)
        return FALSE;
        
    if(ripper->error != NULL) {
        g_free(ripper->error);
        ripper->error = NULL;
    }
    
    ripper->cancel = FALSE;
        
    if(!gst_cd_ripper_build_pipeline(ripper))
        return FALSE;

    gst_element_set_state(ripper->filesink, GST_STATE_NULL);
    g_object_set(G_OBJECT(ripper->filesink), "location", uri, NULL);
    
    if(GST_IS_BIN(ripper->encoder)) {
        elements = (GList *)gst_bin_get_list(GST_BIN(ripper->encoder));
    } else {
        elements = g_list_append(elements, ripper->encoder);
    }
    
    for(element = elements; element != NULL; element = element->next) {
        if(!GST_IS_TAG_SETTER(element->data))
            continue;
            
        gst_tag_setter_add(GST_TAG_SETTER(element->data),
            GST_TAG_MERGE_REPLACE_ALL,
            GST_TAG_TITLE,  md_title,
            GST_TAG_ARTIST, md_artist,
            GST_TAG_ALBUM,  md_album,
            GST_TAG_TRACK_NUMBER, md_track_number,
            GST_TAG_TRACK_COUNT,  md_track_count,
            GST_TAG_ENCODER, _("Banshee"),
            GST_TAG_ENCODER_VERSION, VERSION,
            NULL);
            
        if(md_genre && strlen(md_genre) == 0) {
            gst_tag_setter_add(GST_TAG_SETTER(element->data),
                GST_TAG_MERGE_APPEND,
                GST_TAG_GENRE, md_genre);
        }
        
        can_tag = TRUE;        
    }        

    if(!can_tag) {
        g_warning(_("Encoding element does not support tagging!"));
    }
        
    gst_element_set_state(ripper->pipeline, GST_STATE_PAUSED);
    
    event = gst_event_new_segment_seek(
        ripper->track_format | GST_SEEK_METHOD_SET | GST_SEEK_FLAG_FLUSH,
        track_number - 1, track_number);
        
    if(!gst_pad_send_event(ripper->source_pad, event)) {
        ripper->error = g_strdup(_("Could not send seek event to cdparanoia"));
        return FALSE;
    }
    
    if(!gst_pad_query(ripper->source_pad, GST_QUERY_POSITION, 
        &format, &nanoseconds)) {
        ripper->error = g_strdup(_("Could not get track start position"));
        return FALSE;
    }    

    ripper->track_start = nanoseconds / GST_SECOND;
    
    gst_element_set_state(ripper->pipeline, GST_STATE_PLAYING);
              
    while(gst_bin_iterate(GST_BIN(ripper->pipeline))) {
        if(ripper->cancel == TRUE || ripper->error != NULL)
            break;

        if(!gst_pad_query(ripper->source_pad, GST_QUERY_POSITION, 
            &format, &nanoseconds)) {
            ripper->error = g_strdup(_("Could not get track position"));
            break;
        }
       
        if(gst_element_get_state(ripper->pipeline) != GST_STATE_PLAYING)
            break;
            
        seconds = nanoseconds / GST_SECOND;
        
        if(seconds != ripper->seconds) {
            ripper->seconds = seconds;
            
            if(ripper->progress_callback != NULL) {
                ripper->progress_callback((gpointer)ripper, 
                    ripper->seconds, user_info);
            }
        }
      
        /* nasty hack because gstreamer keeps iterating after completion */
        if(seconds > 0)
            has_started = TRUE;
        else if(has_started && seconds == 0)
            break;
    }            
            
    gst_element_set_state(GST_ELEMENT(ripper->pipeline), GST_STATE_NULL);
    g_object_unref(G_OBJECT(ripper->pipeline));

    ripper->cancel = FALSE;

    return ripper->error == NULL;
}

void
gst_cd_ripper_set_progress_callback(GstCdRipper *ripper, GstCdRipperProgressCallback cb)
{
    if(ripper == NULL)
        return;
        
    ripper->progress_callback = cb;
}

void
gst_cd_ripper_cancel(GstCdRipper *ripper)
{
    if(ripper == NULL)
        return;
        
    ripper->cancel = TRUE;
}

gchar *
gst_cd_ripper_get_error(GstCdRipper *ripper)
{
    if(ripper == NULL)
        return NULL;
        
    return ripper->error;
}
