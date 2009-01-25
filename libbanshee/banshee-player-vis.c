//
// banshee-player-vis.c
//
// Author:
//   Chris Howie <cdhowie@gmail.com>
//
// Copyright (C) 2008 Chris Howie
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

#include "banshee-player-vis.h"

#define SPECTRUM_SIZE 512

static GstStaticCaps vis_data_sink_caps = GST_STATIC_CAPS (
    "audio/x-raw-float, "
    "rate = (int) 30720, "
    "channels = (int) 2, "
    "endianness = (int) BYTE_ORDER, "
    "width = (int) 32"
);

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static void
bp_vis_pcm_handoff (GstElement *sink, GstBuffer *buffer, GstPad *pad, gpointer userdata)
{
    BansheePlayer *player = (BansheePlayer*)userdata;
    GstStructure *structure;
    gint channels, wanted_size;
    gfloat *data;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->vis_data_cb == NULL) {
        return;
    }
    
    structure = gst_caps_get_structure (gst_buffer_get_caps (buffer), 0);
    gst_structure_get_int (structure, "channels", &channels);
    
    wanted_size = channels * SPECTRUM_SIZE * sizeof (gfloat);
    
    gst_adapter_push (player->vis_buffer, gst_buffer_copy (buffer));
    
    while ((data = (gfloat *)gst_adapter_peek (player->vis_buffer, wanted_size)) != NULL) {
        gfloat *deinterlaced = g_malloc (wanted_size);
        gint i, j;
        
        for (i = 0; i < SPECTRUM_SIZE; i++) {
            for (j = 0; j < channels; j++) {
                deinterlaced[j * SPECTRUM_SIZE + i] = data[i * channels + j];
            }
        }
        
        player->vis_data_cb (player, channels, SPECTRUM_SIZE, deinterlaced, player->spectrum_buffer);
        
        g_free (deinterlaced);
        gst_adapter_flush (player->vis_buffer, wanted_size);
    }
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

void
_bp_vis_process_message (BansheePlayer *player, GstMessage *message)
{
    const GstStructure *st;
    const GValue *spec;
    gint i;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    st = gst_message_get_structure (message);
    if (strcmp (gst_structure_get_name (st), "spectrum") != 0) {
        return;
    }
    
    spec = gst_structure_get_value (st, "magnitude");
    
    for (i = 0; i < SPECTRUM_SIZE; i++) {
        // v is in the range -60 to 0.  Move this up to 0 to 1.
        gfloat v = g_value_get_float (gst_value_list_get_value (spec, i));
        player->spectrum_buffer[i] = (v + 60.0f) / 60.0f;
    }
}

void
_bp_vis_pipeline_setup (BansheePlayer *player)
{
    GstElement *fakesink, *converter, *resampler, *audiosinkqueue, *spectrum;
    GstCaps *caps;
    GstPad *pad;
    
    player->vis_buffer = NULL;
    player->spectrum_buffer = NULL;
    
    // Privided by gst-plugins-good
    spectrum = gst_element_factory_make ("spectrum", "vis-spectrum");
    if (spectrum == NULL) {
        bp_debug ("Could not create the spectrum element. Visualization will be disabled.");
        return;
    }
    
    g_object_set (G_OBJECT (spectrum), "bands", SPECTRUM_SIZE, "interval", GST_SECOND / 60, NULL);
    
    // Core elements, if something fails here, it's the end of the world
    audiosinkqueue = gst_element_factory_make ("queue", "vis-queue");
    resampler = gst_element_factory_make ("audioresample", "vis-resample");
    converter = gst_element_factory_make ("audioconvert", "vis-convert");
    fakesink = gst_element_factory_make ("fakesink", "vis-sink");
    
    if (audiosinkqueue == NULL || resampler == NULL || converter == NULL || fakesink == NULL) {
        bp_debug ("Could not construct visualization pipeline, a fundamental element could not be created");
        return;
    }
    
    g_signal_connect (G_OBJECT (fakesink), "handoff", G_CALLBACK (bp_vis_pcm_handoff), player);
    g_object_set (G_OBJECT (fakesink), "signal-handoffs", TRUE, "sync", TRUE, NULL);
    
    gst_bin_add_many (GST_BIN (player->audiobin), audiosinkqueue, resampler, converter, spectrum, fakesink, NULL);
    
    pad = gst_element_get_static_pad (audiosinkqueue, "sink");
    gst_pad_link (gst_element_get_request_pad (player->audiotee, "src%d"), pad);
    gst_object_unref (GST_OBJECT (pad));
    
    gst_element_link_many (audiosinkqueue, resampler, converter, NULL);
    
    caps = gst_static_caps_get (&vis_data_sink_caps);
    gst_element_link_filtered (converter, spectrum, caps);
    gst_caps_unref (caps);
    
    gst_element_link (spectrum, fakesink);
    
    player->vis_buffer = gst_adapter_new ();
    player->spectrum_buffer = g_new0 (gfloat, SPECTRUM_SIZE);
}

void
_bp_vis_pipeline_destroy (BansheePlayer *player)
{
    if (player->vis_buffer != NULL) {
        gst_object_unref (player->vis_buffer);
        player->vis_buffer = NULL;
    }
    
    if (player->spectrum_buffer != NULL) {
        g_free (player->spectrum_buffer);
        player->spectrum_buffer = NULL;
    }
}

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

P_INVOKE void
bp_set_vis_data_callback (BansheePlayer *player, BansheePlayerVisDataCallback cb)
{
    SET_CALLBACK (vis_data_cb);
}
