//
// banshee-player-replaygain.h
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

#ifndef _BANSHEE_PLAYER_REPLAYGAIN_H
#define _BANSHEE_PLAYER_REPLAYGAIN_H

#include "banshee-player-private.h"

void _bp_replaygain_process_tag          (BansheePlayer *player, const gchar *tag_name, const GValue *value);
void _bp_replaygain_handle_state_changed (BansheePlayer *player, GstState old, GstState new, GstState pending);
void _bp_replaygain_update_volume        (BansheePlayer *player);

static inline void
_bp_replaygain_init (BansheePlayer *player)
{
    gint i;
    for (i = 0; i < 11; i++) {
        player->volume_scale_history[i] = 1.0;
    }
    
}

#endif /* _BANSHEE_PLAYER_REPLAYGAIN_H */
