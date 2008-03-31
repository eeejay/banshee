//
// banshee-player-equalizer.c
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
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

#include "banshee-player-private.h"

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

P_INVOKE gboolean
bp_equalizer_is_supported (BansheePlayer *player)
{
    return player != NULL && player->equalizer != NULL && player->preamp != NULL;
}

P_INVOKE void
bp_equalizer_set_preamp_level (BansheePlayer *player, gdouble level)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    if (player->equalizer != NULL && player->preamp != NULL) {
        g_object_set (player->preamp, "volume", level, NULL);
    }
}

P_INVOKE void
bp_equalizer_set_gain (BansheePlayer *player, guint bandnum, gdouble gain)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->equalizer != NULL) {
        GstObject *band = gst_child_proxy_get_child_by_index (GST_CHILD_PROXY (player->equalizer), bandnum);
        g_object_set (band, "gain", gain, NULL);
        g_object_unref (band);
    }
}

P_INVOKE void
bp_equalizer_get_bandrange (BansheePlayer *player, gint *min, gint *max)
{    
    // NOTE: This only refers to the newer version of the equalizer element.
    //
    // GStreamer's equalizer goes from -24 to +12, but -12 to +12 is much better:
    //  - Equal levels on both sides, which means we get a nice linear y=x
    //  - This also makes converting other eq presets easier.
    //  - We get a nice roud 0 dB in the middle of the band range, instead of -6, which is stupid
    //    since 0 dB gives us no gain, yet its not in the middle - less sense to the end user.
    
    GParamSpecDouble *pspec;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->equalizer == NULL) {
        return;
    }
    
    // Fetch gain range of first band (since it should be the same for the rest)
    pspec = (GParamSpecDouble*)g_object_class_find_property (G_OBJECT_GET_CLASS (player->equalizer), "band0");
    if (pspec != NULL) {
        // Assume old equalizer.
        *min = pspec->minimum;
        *max = pspec->maximum;
        return;
    } 
    
    pspec = (GParamSpecDouble*)g_object_class_find_property (G_OBJECT_GET_CLASS (player->equalizer), "band0::gain");
    if (pspec != NULL && pspec->maximum == 12) {
        // New equalizer - return even scale.
        *min = -12;
        *max = 12;
    } else if (pspec != NULL) {
        // Return just the ranges the equalizer supports
        *min = pspec->minimum;
        *max = pspec->maximum;
    } else {
       g_warning ("Could not find valid gain range for equalizer element");
    }
}

P_INVOKE void
bp_equalizer_get_frequencies (BansheePlayer *player, gdouble **freq)
{
    gint i;
    gdouble bandfreq[10];
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    for (i = 0; i < 10; i++) {
        GstObject *band;
        
        band = gst_child_proxy_get_child_by_index (GST_CHILD_PROXY (player->equalizer), i);
        g_object_get (G_OBJECT (band), "freq", &bandfreq[i], NULL);
        g_object_unref (band);
    }
    
    *freq = bandfreq;
}
