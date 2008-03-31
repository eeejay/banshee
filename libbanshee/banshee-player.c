/***************************************************************************
 *  gst-playback-0.10.c
 *
 *  Copyright (C) 2005-2007 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 *  Contributions by Alexander Hixon <hixon.alexander@mediati.org>
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

#define _BANSHEE_PLAYER_C
 
#include "banshee-player.h"
#include "banshee-player-cdda.h"

#ifdef GDK_WINDOWING_X11
static gboolean bp_find_xoverlay (BansheePlayer *player);
#endif

static void
bp_process_tag(const GstTagList *tag_list, const gchar *tag_name, BansheePlayer *player)
{
    const GValue *value;
    gint value_count;

    value_count = gst_tag_list_get_tag_size(tag_list, tag_name);
    if(value_count < 1) {
        return;
    }
    
    value = gst_tag_list_get_value_index(tag_list, tag_name, 0);

    if(player != NULL && player->tag_found_cb != NULL) {
        player->tag_found_cb(player, tag_name, value);
    }
}

// private methods

static void
bp_nuke_slist(GSList *list)
{   
    GSList *node = list;
    
    for(; node != NULL; node = node->next) {
        g_free(node->data);
    }
    
    g_slist_free(list);
}

static void
bp_destroy_pipeline(BansheePlayer *player)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    
    if(player->playbin == NULL) {
        return;
    }
    
    if(GST_IS_ELEMENT(player->playbin)) {
        player->target_state = GST_STATE_NULL;
        gst_element_set_state(player->playbin, GST_STATE_NULL);
        gst_object_unref(GST_OBJECT(player->playbin));
    }
    
    player->playbin = NULL;
}

#ifdef HAVE_GST_PBUTILS
static gchar **
bp_missing_element_details_vectorize(const GSList *elements)
{
    GPtrArray *vector = g_ptr_array_new();
    
    while(elements != NULL) {
        g_ptr_array_add(vector, g_strdup(elements->data));
        elements = elements->next;
    }
    
    g_ptr_array_add(vector, NULL);
    return (gchar **)g_ptr_array_free(vector, FALSE);
}

static void
bp_handle_missing_elements_failed(BansheePlayer *player)
{
    bp_nuke_slist(player->missing_element_details);
    player->missing_element_details = NULL;
    gst_element_set_state(player->playbin, GST_STATE_READY);
    
    if(player->error_cb != NULL) {
       player->error_cb(player, GST_CORE_ERROR, GST_CORE_ERROR_MISSING_PLUGIN, NULL, NULL);
    }
}

static void
bp_handle_missing_elements_installer_result(GstInstallPluginsReturn result, gpointer data)
{
    BansheePlayer *player = (BansheePlayer *)data;
    
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    
    // TODO: Actually handle a successful plugin installation
    // if(result == GST_INSTALL_PLUGINS_SUCCESS) {
    // }
    
    player->install_plugins_noprompt = TRUE;
    
    bp_handle_missing_elements_failed(player);
    
    gst_install_plugins_context_free(player->install_plugins_context);
    player->install_plugins_context = NULL;
}

static void
bp_handle_missing_elements(BansheePlayer *player)
{
    GstInstallPluginsReturn install_return;
    gchar **details;
    
    if(player->install_plugins_context != NULL) {
        return;
    } else if(player->install_plugins_noprompt) {
        bp_handle_missing_elements_failed(player);
        return;
    }
    
    details = bp_missing_element_details_vectorize(player->missing_element_details);
    player->install_plugins_context = gst_install_plugins_context_new();
    
    #ifdef GDK_WINDOWING_X11
    if(player->window != NULL) {
        gst_install_plugins_context_set_xid(player->install_plugins_context, 
        GDK_WINDOW_XWINDOW(player->window));
    }
    #endif
    
    install_return = gst_install_plugins_async(details, player->install_plugins_context, 
        bp_handle_missing_elements_installer_result, player);
    
    if(install_return != GST_INSTALL_PLUGINS_STARTED_OK) {
        bp_handle_missing_elements_failed(player);
        
        gst_install_plugins_context_free(player->install_plugins_context);
        player->install_plugins_context = NULL;
    } 
    
    g_strfreev(details);
}
#endif

static gboolean
bp_bus_callback(GstBus *bus, GstMessage *message, gpointer data)
{
    BansheePlayer *player = (BansheePlayer *)data;

    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), FALSE);

    switch(GST_MESSAGE_TYPE(message)) {
        case GST_MESSAGE_ERROR: {
            GError *error;
            gchar *debug;
            
            // FIXME: This is to work around a bug in qtdemux in
            // -good <= 0.10.6
            if(message->src != NULL && message->src->name != NULL &&
                strncmp(message->src->name, "qtdemux", 0) == 0) {
                break;
            }
            
            bp_destroy_pipeline(player);
            
            if(player->error_cb != NULL) {
                gst_message_parse_error(message, &error, &debug);
                player->error_cb(player, error->domain, error->code, error->message, debug);
                g_error_free(error);
                g_free(debug);
            }
            
            break;
        } 
        
        #ifdef HAVE_GST_PBUTILS
        case GST_MESSAGE_ELEMENT: {
            if(gst_is_missing_plugin_message(message)) {
                player->missing_element_details = g_slist_append(player->missing_element_details, 
                    gst_missing_plugin_message_get_installer_detail(message));
            }
            
            break;
        }
        #endif
        
        case GST_MESSAGE_EOS:
            if(player->eos_cb != NULL) {
                player->eos_cb(player);
            }
            break;
        case GST_MESSAGE_STATE_CHANGED: {
            GstState old, new, pending;
            gst_message_parse_state_changed(message, &old, &new, &pending);
            
            #ifdef HAVE_GST_PBUTILS
            if(old == GST_STATE_READY && new == GST_STATE_PAUSED) {
                if(player->missing_element_details != NULL) {
                    bp_handle_missing_elements(player);
                }
            }
            #endif
            
            if(player->state_changed_cb != NULL) {
                player->state_changed_cb(player, old, new, pending);
            }
            break;
        }
        case GST_MESSAGE_BUFFERING: {
            const GstStructure *buffering_struct;
            gint buffering_progress = 0;
            
            buffering_struct = gst_message_get_structure(message);
            if(!gst_structure_get_int(buffering_struct, "buffer-percent", &buffering_progress)) {
                g_warning("Could not get completion percentage from BUFFERING message");
                break;
            }
            
            if(buffering_progress >= 100) {
                player->buffering = FALSE;
                if(player->target_state == GST_STATE_PLAYING) {
                    gst_element_set_state(player->playbin, GST_STATE_PLAYING);
                }
            } else if(!player->buffering && player->target_state == GST_STATE_PLAYING) {
                GstState current_state;
                gst_element_get_state(player->playbin, &current_state, NULL, 0);
                if(current_state == GST_STATE_PLAYING) {
                    gst_element_set_state(player->playbin, GST_STATE_PAUSED);
                }
                player->buffering = TRUE;
            } 

            if(player->buffering_cb != NULL) {
                player->buffering_cb(player, buffering_progress);
            }
        }
        case GST_MESSAGE_TAG: {
            GstTagList *tags;
            
            if(GST_MESSAGE_TYPE(message) != GST_MESSAGE_TAG) {
                break;
            }
            
            gst_message_parse_tag(message, &tags);
            
            if(GST_IS_TAG_LIST(tags)) {
                gst_tag_list_foreach(tags, (GstTagForeachFunc)bp_process_tag, player);
                gst_tag_list_free(tags);
            }
            break;
        }
        default:
            break;
    }
    
    return TRUE;
}

static void
bp_video_sink_element_added (GstBin *videosink, GstElement *element, BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    #ifdef GDK_WINDOWING_X11
    g_mutex_lock (player->mutex);
    bp_find_xoverlay (player);
    g_mutex_unlock (player->mutex);    
    #endif
}

static void
bp_bus_element_sync_message (GstBus *bus, GstMessage *message, BansheePlayer *player)
{
    gboolean found_xoverlay;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    #ifdef GDK_WINDOWING_X11

    if (message->structure == NULL || !gst_structure_has_name (message->structure, "prepare-xwindow-id")) {
        return;
    }

    g_mutex_lock (player->mutex);
    found_xoverlay = bp_find_xoverlay (player);
    g_mutex_unlock (player->mutex);

    if (found_xoverlay) {
        gst_x_overlay_set_xwindow_id (player->xoverlay, GDK_WINDOW_XWINDOW (player->video_window));
    }

    #endif
}

static gboolean 
bp_construct(BansheePlayer *player)
{
    GstBus *bus;
    GstElement *videosink;
    GstElement *audiosink;
    GstElement *audiosinkqueue;
    //GstElement *audioconvert;
    GstPad *teepad;
    
    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), FALSE);
    
    // create necessary elements
    player->playbin = gst_element_factory_make("playbin", "playbin");
    g_return_val_if_fail(player->playbin != NULL, FALSE);

    audiosink = gst_element_factory_make("gconfaudiosink", "audiosink");
    g_return_val_if_fail(audiosink != NULL, FALSE);
        
    /* Set the profile to "music and movies" (gst-plugins-good 0.10.3) */
    if(g_object_class_find_property(G_OBJECT_GET_CLASS(audiosink), "profile")) {
        g_object_set(G_OBJECT(audiosink), "profile", 1, NULL);
    }
    
    player->audiobin = gst_bin_new("audiobin");
    g_return_val_if_fail(player->audiobin != NULL, FALSE);
    
    player->audiotee = gst_element_factory_make("tee", "audiotee");
    g_return_val_if_fail(player->audiotee != NULL, FALSE);
    
    audiosinkqueue = gst_element_factory_make("queue", "audiosinkqueue");
    g_return_val_if_fail(audiosinkqueue != NULL, FALSE);
    
    //audioconvert = gst_element_factory_make("audioconvert", "audioconvert");
    //player->equalizer = gst_element_factory_make("equalizer-10bands", "equalizer-10bands");
    //player->preamp = gst_element_factory_make("volume", "preamp");
    
    // add elements to custom audio sink
    gst_bin_add(GST_BIN(player->audiobin), player->audiotee);
    //if(player->equalizer != NULL) {
    //    gst_bin_add(GST_BIN(player->audiobin), audioconvert);
    //    gst_bin_add(GST_BIN(player->audiobin), player->equalizer);
    //    gst_bin_add(GST_BIN(player->audiobin), player->preamp);
    //}
    gst_bin_add(GST_BIN(player->audiobin), audiosinkqueue);
    gst_bin_add(GST_BIN(player->audiobin), audiosink);
   
    // ghost pad the audio bin
    teepad = gst_element_get_pad(player->audiotee, "sink");
    gst_element_add_pad(player->audiobin, gst_ghost_pad_new("sink", teepad));
    gst_object_unref(teepad);

    // link the tee/queue pads for the default
    gst_pad_link(gst_element_get_request_pad(player->audiotee, "src0"), 
        gst_element_get_pad(audiosinkqueue, "sink"));

    //if (player->equalizer != NULL)
    //{
    //    //link in equalizer, preamp and audioconvert.
    //    gst_element_link(audiosinkqueue, player->preamp);
    //    gst_element_link(player->preamp, player->equalizer);
    //    gst_element_link(player->equalizer, audioconvert);
    //    gst_element_link(audioconvert, audiosink);
    //} else {
    //    // link the queue with the real audio sink
        gst_element_link(audiosinkqueue, audiosink);
    //}
    
    g_object_set (G_OBJECT (player->playbin), "audio-sink", player->audiobin, NULL);
    
    videosink = gst_element_factory_make ("gconfvideosink", "videosink");
    if (videosink == NULL) {
        videosink = gst_element_factory_make ("ximagesink", "videosink");
        if (videosink == NULL) {
            videosink = gst_element_factory_make ("fakesink", "videosink");
            if (videosink != NULL) {
                g_object_set (G_OBJECT (videosink), "sync", TRUE, NULL);
            }
        }
    }
    
    g_object_set (G_OBJECT (player->playbin), "video-sink", videosink, NULL);

    bus = gst_pipeline_get_bus (GST_PIPELINE (player->playbin));
    
    gst_bus_add_watch (bus, bp_bus_callback, player);
    gst_bus_set_sync_handler (bus, gst_bus_sync_signal_handler, player);

    g_signal_connect (bus, "sync-message::element", 
        G_CALLBACK (bp_bus_element_sync_message), player);

    g_signal_connect (player->playbin, "notify::source", G_CALLBACK (_bp_cdda_on_notify_source), player);
    
    if (GST_IS_BIN (videosink)) {
        g_signal_connect (videosink, "element-added",
            G_CALLBACK (bp_video_sink_element_added), player);
    }

    return TRUE;
}

static gboolean
bp_iterate_timeout(BansheePlayer *player)
{
    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), FALSE);
    
    if(player->iterate_cb != NULL) {
        player->iterate_cb(player);
    }
    
    return TRUE;
}

static void
bp_start_iterate_timeout(BansheePlayer *player)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));

    if(player->iterate_timeout_id != 0) {
        return;
    }
    
    player->iterate_timeout_id = g_timeout_add(200, 
        (GSourceFunc)bp_iterate_timeout, player);
}

static void
bp_stop_iterate_timeout(BansheePlayer *player)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    
    if(player->iterate_timeout_id == 0) {
        return;
    }
    
    g_source_remove(player->iterate_timeout_id);
    player->iterate_timeout_id = 0;
}

// public methods

BansheePlayer *
bp_new()
{
    BansheePlayer *player = g_new0(BansheePlayer, 1);
    
    player->mutex = g_mutex_new ();
    
    if(!bp_construct(player)) {
        g_free(player);
        return NULL;
    }
    
    return player;
}

void
bp_free(BansheePlayer *player)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    
    g_mutex_free (player->mutex);
    
    if(GST_IS_OBJECT(player->playbin)) {
        player->target_state = GST_STATE_NULL;
        gst_element_set_state(player->playbin, GST_STATE_NULL);
        gst_object_unref(GST_OBJECT(player->playbin));
    }
    
    if(player->cdda_device != NULL) {
        g_free(player->cdda_device);
        player->cdda_device = NULL;
    }
    
    bp_nuke_slist(player->missing_element_details);
    player->missing_element_details = NULL;
    
    #ifdef HAVE_GST_PBUTILS
    if(player->install_plugins_context != NULL) {
        gst_install_plugins_context_free(player->install_plugins_context);
        player->install_plugins_context = NULL;
    }
    #endif
    
    g_free(player);
    player = NULL;
}

void
bp_set_eos_callback(BansheePlayer *player, 
    BansheePlayerEosCallback cb)
{
    SET_CALLBACK(eos_cb);
}

void
bp_set_error_callback(BansheePlayer *player, 
    BansheePlayerErrorCallback cb)
{
    SET_CALLBACK(error_cb);
}

void
bp_set_state_changed_callback(BansheePlayer *player, 
    BansheePlayerStateChangedCallback cb)
{
    SET_CALLBACK(state_changed_cb);
}

void
bp_set_iterate_callback(BansheePlayer *player, 
    BansheePlayerIterateCallback cb)
{
    SET_CALLBACK(iterate_cb);
}

void
bp_set_buffering_callback(BansheePlayer *player, 
    BansheePlayerBufferingCallback cb)
{
    SET_CALLBACK(buffering_cb);
}

void
bp_set_tag_found_callback(BansheePlayer *player, 
    BansheePlayerTagFoundCallback cb)
{
    SET_CALLBACK(tag_found_cb);
}

void
bp_open(BansheePlayer *player, const gchar *uri)
{
    GstState state;
    
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    
    if(player->playbin == NULL && !bp_construct(player)) {
        return;
    }

    if (_bp_cdda_handle_uri (player, uri)) {
        return;
    }
    
    gst_element_get_state(player->playbin, &state, NULL, 0);
    if(state >= GST_STATE_PAUSED) {
        player->target_state = GST_STATE_READY;
        gst_element_set_state(player->playbin, GST_STATE_READY);
    }
    
    g_object_set(G_OBJECT(player->playbin), "uri", uri, NULL);
}

void
bp_stop(BansheePlayer *player, gboolean nullstate)
{
    GstState state = nullstate ? GST_STATE_NULL : GST_STATE_PAUSED;
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    bp_stop_iterate_timeout(player);
    if(GST_IS_ELEMENT(player->playbin)) {
        player->target_state = state;
        gst_element_set_state(player->playbin, state);
    }
}

void
bp_pause(BansheePlayer *player)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    bp_stop_iterate_timeout(player);
    player->target_state = GST_STATE_PAUSED;
    gst_element_set_state(player->playbin, GST_STATE_PAUSED);
}

void
bp_play(BansheePlayer *player)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    player->target_state = GST_STATE_PLAYING;
    gst_element_set_state(player->playbin, GST_STATE_PLAYING);
    bp_start_iterate_timeout(player);
}

void
bp_set_volume(BansheePlayer *player, gint volume)
{
    gdouble act_volume;
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    act_volume = CLAMP(volume, 0, 100) / 100.0;
    g_object_set(G_OBJECT(player->playbin), "volume", act_volume, NULL);
}

gint
bp_get_volume(BansheePlayer *player)
{
    gdouble volume = 0.0;
    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), 0);
    g_object_get(player->playbin, "volume", &volume, NULL);
    return (gint)(volume * 100.0);
}

void
bp_set_position(BansheePlayer *player, guint64 time_ms)
{
    g_return_if_fail(IS_BANSHEE_PLAYER(player));
    
    if(!gst_element_seek(player->playbin, 1.0, 
        GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH,
        GST_SEEK_TYPE_SET, time_ms * GST_MSECOND, 
        GST_SEEK_TYPE_NONE, GST_CLOCK_TIME_NONE)) {
        g_warning("Could not seek in stream");
    }
}

guint64
bp_get_position(BansheePlayer *player)
{
    GstFormat format = GST_FORMAT_TIME;
    gint64 position;

    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), 0);

    if(gst_element_query_position(player->playbin, &format, &position)) {
        return position / 1000000;
    }
    
    return 0;
}

guint64
bp_get_duration(BansheePlayer *player)
{
    GstFormat format = GST_FORMAT_TIME;
    gint64 duration;

    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), 0);

    if(gst_element_query_duration(player->playbin, &format, &duration)) {
        return duration / 1000000;
    }
    
    return 0;
}

gboolean
bp_can_seek(BansheePlayer *player)
{
    GstQuery *query;
    gboolean can_seek = TRUE;
    
    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), FALSE);
    g_return_val_if_fail(player->playbin != NULL, FALSE);
    
    query = gst_query_new_seeking(GST_FORMAT_TIME);
    if(!gst_element_query(player->playbin, query)) {
        // This will probably fail, 100% of the time, because it's apparently 
        // very unimplemented in GStreamer... when it's fixed
        // we will return FALSE here, and show the warning
        // g_warning("Could not query pipeline for seek ability");
        return bp_get_duration(player) > 0;
    }
    
    gst_query_parse_seeking(query, NULL, &can_seek, NULL, NULL);
    gst_query_unref(query);
    
    return can_seek;
}

gboolean
bp_get_pipeline_elements(BansheePlayer *player, GstElement **playbin, GstElement **audiobin, 
    GstElement **audiotee)
{
    g_return_val_if_fail(IS_BANSHEE_PLAYER(player), FALSE);
    
    *playbin = player->playbin;
    *audiobin = player->audiobin;
    *audiotee = player->audiotee;
    
    return TRUE;
}

void
bp_set_application_gdk_window(BansheePlayer *player, GdkWindow *window)
{
    player->window = window;
}

void
bp_get_error_quarks(GQuark *core, GQuark *library, GQuark *resource, GQuark *stream)
{
    *core = GST_CORE_ERROR;
    *library = GST_LIBRARY_ERROR;
    *resource = GST_RESOURCE_ERROR;
    *stream = GST_STREAM_ERROR;
}

/* Region XOverlay */

#ifdef GDK_WINDOWING_X11

gboolean
bp_video_is_supported (BansheePlayer *player)
{
    return TRUE; // bp_find_xoverlay (player);
}

static gboolean
bp_find_xoverlay (BansheePlayer *player)
{
    GstElement *video_sink = NULL;
    GstElement *xoverlay;
    GstXOverlay *previous_xoverlay;

    previous_xoverlay = player->xoverlay;
    
    g_object_get (player->playbin, "video-sink", &video_sink, NULL);
    
    if (video_sink == NULL) {
        player->xoverlay = NULL;
        if (previous_xoverlay != NULL) {
            gst_object_unref (previous_xoverlay);
        }

        return FALSE;
    }
    
    xoverlay = GST_IS_BIN (video_sink)
        ? gst_bin_get_by_interface (GST_BIN (video_sink), GST_TYPE_X_OVERLAY)
        : video_sink;
    
    player->xoverlay = GST_IS_X_OVERLAY (xoverlay) ? GST_X_OVERLAY (xoverlay) : NULL;
    
    if (previous_xoverlay != NULL) {
        gst_object_unref (previous_xoverlay);
    }
        
    if (player->xoverlay != NULL && g_object_class_find_property (
        G_OBJECT_GET_CLASS (player->xoverlay), "force-aspect-ratio")) {
        g_object_set (G_OBJECT (player->xoverlay), "force-aspect-ratio", TRUE, NULL);
    }

    gst_object_unref (video_sink);

    return player->xoverlay != NULL;
}

void
bp_set_video_window (BansheePlayer *player, GdkWindow *window)
{
    player->video_window = window;
}

void
bp_expose_video_window (BansheePlayer *player, GdkWindow *window, gboolean direct)
{
    XID window_id;
    
    if (direct && player->xoverlay != NULL && GST_IS_X_OVERLAY (player->xoverlay)) {
        gst_x_overlay_expose (player->xoverlay);
        return;
    }
   
    g_mutex_lock (player->mutex);
   
    if (player->xoverlay == NULL && !bp_find_xoverlay (player)) {
        g_mutex_unlock (player->mutex);
        return;
    }
    
    gst_object_ref (player->xoverlay);
    g_mutex_unlock (player->mutex);

    window_id = GDK_WINDOW_XWINDOW (window);

    gst_x_overlay_set_xwindow_id (player->xoverlay, window_id);
    gst_x_overlay_expose (player->xoverlay);

    gst_object_unref (player->xoverlay);
}

#else

gboolean
bp_video_is_supported (BansheePlayer *player)
{
    return FALSE;
}

void
bp_set_video_window (BansheePlayer *player, GdkWindow *window)
{
}

void
bp_expose_video_window (BansheePlayer *player, GdkWindow *window, gboolean direct)
{
}

#endif

/* Region Equalizer */

gboolean
gst_equalizer_is_supported(BansheePlayer *player)
{
    return player != NULL && player->equalizer != NULL && player->preamp != NULL;
}

void
gst_equalizer_set_preamp_level(BansheePlayer *player, gdouble level)
{
    if (player->equalizer != NULL && player->preamp != NULL)
        g_object_set (player->preamp, "volume", level, NULL);
}

void
gst_equalizer_set_gain(BansheePlayer *player, guint bandnum, gdouble gain)
{
    if (player->equalizer != NULL) {
        GstObject *band;   
        band = gst_child_proxy_get_child_by_index (GST_CHILD_PROXY (player->equalizer), bandnum);
        g_object_set (band, "gain", gain, NULL);
        g_object_unref (band);
    }
}

void
gst_equalizer_get_bandrange(BansheePlayer *player, gint *min, gint *max)
{    
    /*
     * NOTE: This only refers to the newer version of the equalizer element.
     *
     * Yes, I know GStreamer's equalizer goes from -24 to +12, but -12 to +12 is much better for two reasons:
     * (x) Equal levels on both sides, which means we get a nice linear y=x
     * (x) This also makes converting other eq presets easier.
     * (x) We get a nice roud 0 dB in the middle of the band range, instead of -6, which is stupid
     *     since 0 dB gives us no gain, yet its not in the middle - less sense to the end user.
     *
     * If that didn't make any sense, yay for late-night coding. :)
     */
    
    if (player->equalizer != NULL) {
        GParamSpecDouble *pspec;
        
        // Fetch gain range of first band (since it should be the same for the rest)
        pspec = (GParamSpecDouble*) g_object_class_find_property (G_OBJECT_GET_CLASS (player->equalizer), "band0");
        if (pspec) {
            // Assume old equalizer.
            *min = pspec->minimum;
            *max = pspec->maximum;
        }
        else {
            pspec = (GParamSpecDouble*) g_object_class_find_property (G_OBJECT_GET_CLASS (player->equalizer), "band0::gain");
            if (pspec && pspec->maximum == 12) {
                // New equalizer - return even scale.
                *min = -12;
                *max = 12;
            }
            else if (pspec) {
                // Return just the ranges the equalizer supports
                *min = pspec->minimum;
                *max = pspec->maximum;
            }
            else {
                g_warning("Could not find valid gain range for equalizer.");
            }
        }
    }
}

void
gst_equalizer_get_frequencies(BansheePlayer *player, gdouble *freq[])
{
    gint i;
    gdouble bandfreq[10];
    
    for(i = 0; i < 10; i++) {
        GstObject *band;
        
        band = gst_child_proxy_get_child_by_index (GST_CHILD_PROXY (player->equalizer), i);
        g_object_get (G_OBJECT (band), "freq", &bandfreq[i], NULL);
        g_object_unref (band);
    }
    
    *freq = bandfreq;
}
