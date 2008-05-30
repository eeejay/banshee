//
// banshee-player-pipeline.c
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

#include "banshee-player-pipeline.h"
#include "banshee-player-cdda.h"
#include "banshee-player-video.h"
#include "banshee-player-missing-elements.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static void
bp_pipeline_process_tag (const GstTagList *tag_list, const gchar *tag_name, BansheePlayer *player)
{
    const GValue *value;
    gint value_count;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    value_count = gst_tag_list_get_tag_size (tag_list, tag_name);
    if (value_count < 1) {
        return;
    }
    
    value = gst_tag_list_get_value_index (tag_list, tag_name, 0);

    if (value != NULL && player->tag_found_cb != NULL) {
        player->tag_found_cb (player, tag_name, value);
    }
}

static gboolean
bp_pipeline_bus_callback (GstBus *bus, GstMessage *message, gpointer userdata)
{
    BansheePlayer *player = (BansheePlayer *)userdata;

    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    g_return_val_if_fail (message != NULL, FALSE);
    
    switch (GST_MESSAGE_TYPE (message)) {
        case GST_MESSAGE_EOS: {
            if (player->eos_cb != NULL) {
                player->eos_cb (player);
            }
            break;
        }
            
        case GST_MESSAGE_STATE_CHANGED: {
            GstState old, new, pending;
            gst_message_parse_state_changed (message, &old, &new, &pending);
            
            //_bp_missing_elements_handle_state_changed (player, old, new);
            
            if (player->state_changed_cb != NULL) {
                player->state_changed_cb (player, old, new, pending);
            }
            break;
        }
        
        case GST_MESSAGE_BUFFERING: {
            const GstStructure *buffering_struct;
            gint buffering_progress = 0;
            
            buffering_struct = gst_message_get_structure (message);
            if (!gst_structure_get_int (buffering_struct, "buffer-percent", &buffering_progress)) {
                g_warning ("Could not get completion percentage from BUFFERING message");
                break;
            }
            
            if (buffering_progress >= 100) {
                player->buffering = FALSE;
                if (player->target_state == GST_STATE_PLAYING) {
                    gst_element_set_state (player->playbin, GST_STATE_PLAYING);
                }
            } else if (!player->buffering && player->target_state == GST_STATE_PLAYING) {
                GstState current_state;
                gst_element_get_state (player->playbin, &current_state, NULL, 0);
                if (current_state == GST_STATE_PLAYING) {
                    gst_element_set_state (player->playbin, GST_STATE_PAUSED);
                }
                player->buffering = TRUE;
            } 

            if (player->buffering_cb != NULL) {
                player->buffering_cb (player, buffering_progress);
            }
            break;
        }
        
        case GST_MESSAGE_TAG: {
            GstTagList *tags;
            
            if (GST_MESSAGE_TYPE (message) != GST_MESSAGE_TAG) {
                break;
            }
            
            gst_message_parse_tag (message, &tags);
            
            if (GST_IS_TAG_LIST (tags)) {
                gst_tag_list_foreach (tags, (GstTagForeachFunc)bp_pipeline_process_tag, player);
                gst_tag_list_free (tags);
            }
            break;
        }
    
        case GST_MESSAGE_ERROR: {
            GError *error;
            gchar *debug;
            
            // FIXME: This is to work around a bug in qtdemux in
            // -good <= 0.10.6
            if (message->src != NULL && message->src->name != NULL && 
                strncmp (message->src->name, "qtdemux", 7) == 0) {
                break;
            }
            
            _bp_pipeline_destroy (player);
            
            if (player->error_cb != NULL) {
                gst_message_parse_error (message, &error, &debug);
                player->error_cb (player, error->domain, error->code, error->message, debug);
                g_error_free (error);
                g_free (debug);
            }
            
            break;
        } 
        
        case GST_MESSAGE_ELEMENT: {
            _bp_missing_elements_process_message (player, message);
            break;
        }
        
        default: break;
    }
    
    return TRUE;
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

gboolean 
_bp_pipeline_construct (BansheePlayer *player)
{
    GstBus *bus;
    GstPad *teepad;
    GstElement *audiosink;
    GstElement *audiosinkqueue;
    //GstElement *audioconvert, *audioconvert2;
    //GstElement *capsfilter = NULL;
    //gboolean buggy_eq = FALSE;
    
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    // Playbin is the core element that handles autoplugging (finding the right
    // source and decoder elements) based on source URI and stream content
    player->playbin = gst_element_factory_make ("playbin", "playbin");
    g_return_val_if_fail (player->playbin != NULL, FALSE);

    // Try to find an audio sink, prefer gconf, which typically is set to auto these days,
    // fall back on auto, which should work on windows, and as a last ditch, try alsa
    audiosink = gst_element_factory_make ("gconfaudiosink", "audiosink");
    if (audiosink == NULL) {
        audiosink = gst_element_factory_make ("autoaudiosink", "audiosink");
        if (audiosink == NULL) {
            audiosink = gst_element_factory_make ("alsasink", "audiosink");
        }
    }
    
    g_return_val_if_fail (audiosink != NULL, FALSE);
        
    // Set the profile to "music and movies" (gst-plugins-good 0.10.3)
    if (g_object_class_find_property (G_OBJECT_GET_CLASS (audiosink), "profile")) {
        g_object_set (G_OBJECT (audiosink), "profile", 1, NULL);
    }
    
    // Create a custom audio sink bin that will hold the real primary sink
    player->audiobin = gst_bin_new ("audiobin");
    g_return_val_if_fail (player->audiobin != NULL, FALSE);
    
    // Our audio sink is a tee, so plugins can attach their own pipelines
    player->audiotee = gst_element_factory_make ("tee", "audiotee");
    g_return_val_if_fail (player->audiotee != NULL, FALSE);
    
    audiosinkqueue = gst_element_factory_make ("queue", "audiosinkqueue");
    g_return_val_if_fail (audiosinkqueue != NULL, FALSE);

    /*audioconvert = gst_element_factory_make ("audioconvert", "audioconvert");
    audioconvert2 = gst_element_factory_make ("audioconvert", "audioconvert2");
    player->equalizer = gst_element_factory_make ("equalizer-10bands", "equalizer-10bands");
    player->preamp = gst_element_factory_make ("volume", "preamp");

    // Workaround for equalizer bug that caused clipping when processing integer samples
    if (player->equalizer != NULL) {
        GstElementFactory *efactory = gst_element_get_factory (player->equalizer);

        buggy_eq = !gst_plugin_feature_check_version (GST_PLUGIN_FEATURE (efactory), 0, 10, 9);

        if (buggy_eq) {
            GstCaps *caps;

            capsfilter = gst_element_factory_make ("capsfilter", "capsfilter");
            caps = gst_caps_new_simple ("audio/x-raw-float", NULL);
            g_object_set (capsfilter, "caps", caps, NULL);
            gst_caps_unref (caps);
        }
    }*/

    // Add elements to custom audio sink
    gst_bin_add (GST_BIN (player->audiobin), player->audiotee);
    
    /*if (player->equalizer != NULL) {
        gst_bin_add (GST_BIN (player->audiobin), audioconvert);
        gst_bin_add (GST_BIN (player->audiobin), audioconvert2);

        if (buggy_eq) {
            gst_bin_add (GST_BIN (player->audiobin), capsfilter);
        }
        
        gst_bin_add (GST_BIN (player->audiobin), player->equalizer);
        gst_bin_add (GST_BIN (player->audiobin), player->preamp);
    } else {
        g_object_unref (player->preamp);
        player->preamp = NULL;
        g_object_unref (audioconvert);
        g_object_unref (audioconvert2);
    }*/
    
    gst_bin_add (GST_BIN (player->audiobin), audiosinkqueue);
    gst_bin_add (GST_BIN (player->audiobin), audiosink);
   
    // Ghost pad the audio bin so audio is passed from the bin into the tee
    teepad = gst_element_get_pad (player->audiotee, "sink");
    gst_element_add_pad (player->audiobin, gst_ghost_pad_new ("sink", teepad));
    gst_object_unref (teepad);

    // Link the first tee pad to the primary audio sink queue
    gst_pad_link (gst_element_get_request_pad (player->audiotee, "src0"), 
        gst_element_get_pad (audiosinkqueue, "sink"));

    // Link the queue and the actual audio sink
    /*if (player->equalizer != NULL) {
        // link in equalizer, preamp and audioconvert.
        if (buggy_eq) {
            gst_element_link_many (audiosinkqueue, audioconvert, player->preamp, 
                player->equalizer, audioconvert2, audiosink, NULL);
        } else {
            gst_element_link_many (audiosinkqueue, audioconvert, capsfilter, 
                player->preamp, player->equalizer, audioconvert2, audiosink, NULL);
        }
    } else {*/
        // link the queue with the real audio sink
        gst_element_link (audiosinkqueue, audiosink);
    //}
    
    // Now that our internal audio sink is constructed, tell playbin to use it
    g_object_set (G_OBJECT (player->playbin), "audio-sink", player->audiobin, NULL);
    
    // Connect to the bus to get messages
    bus = gst_pipeline_get_bus (GST_PIPELINE (player->playbin));    
    gst_bus_add_watch (bus, bp_pipeline_bus_callback, player);
    
    // Now allow specialized pipeline setups
    _bp_cdda_pipeline_setup (player);
    _bp_video_pipeline_setup (player, bus);

    return TRUE;
}

void
_bp_pipeline_destroy (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->playbin == NULL) {
        return;
    }
    
    if (GST_IS_ELEMENT (player->playbin)) {
        player->target_state = GST_STATE_NULL;
        gst_element_set_state (player->playbin, GST_STATE_NULL);
        gst_object_unref (GST_OBJECT (player->playbin));
    }
    
    player->playbin = NULL;
}
