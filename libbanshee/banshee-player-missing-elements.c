//
// banshee-player-missing-elements.c
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

#include "banshee-player-missing-elements.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static gchar **
bp_slist_to_ptr_array (const GSList *elements)
{
    GPtrArray *vector = g_ptr_array_new ();
    
    while (elements != NULL) {
        g_ptr_array_add (vector, g_strdup (elements->data));
        elements = elements->next;
    }
    
    g_ptr_array_add (vector, NULL);
    return (gchar **)g_ptr_array_free (vector, FALSE);
}

static void
bp_slist_destroy (GSList *list)
{   
    GSList *node = list;
    
    for (; node != NULL; node = node->next) {
        g_free (node->data);
    }
    
    g_slist_free (list);
}

#ifdef HAVE_GST_PBUTILS

static void
bp_missing_elements_handle_install_failed (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    bp_slist_destroy (player->missing_element_details);
    player->missing_element_details = NULL;
    gst_element_set_state (player->playbin, GST_STATE_READY);
    
    if (player->error_cb != NULL) {
        player->error_cb (player, GST_CORE_ERROR, GST_CORE_ERROR_MISSING_PLUGIN, NULL, NULL);
    }
}

static void
bp_missing_elements_handle_install_result (GstInstallPluginsReturn result, gpointer data)
{
    BansheePlayer *player = (BansheePlayer *)data;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    // TODO: Actually handle a successful plugin installation
    // if (result == GST_INSTALL_PLUGINS_SUCCESS) {
    // }
    
    player->install_plugins_noprompt = TRUE;
    
    bp_missing_elements_handle_install_failed (player);
    
    gst_install_plugins_context_free (player->install_plugins_context);
    player->install_plugins_context = NULL;
}

#endif

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

void _bp_missing_elements_destroy (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    bp_slist_destroy (player->missing_element_details);
    
    #ifdef HAVE_GST_PBUTILS
    if (player->install_plugins_context != NULL) {
        gst_install_plugins_context_free (player->install_plugins_context);
    }
    #endif
}

void
_bp_missing_elements_process_message (BansheePlayer *player, GstMessage *message)
{
    #ifdef HAVE_GST_PBUTILS
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    g_return_if_fail (message != NULL);
    
    if (gst_is_missing_plugin_message (message)) {
        player->missing_element_details = g_slist_append (player->missing_element_details, 
            gst_missing_plugin_message_get_installer_detail (message));
    }
    #endif
}

void
_bp_missing_elements_handle_state_changed (BansheePlayer *player, GstState old, GstState new)
{
    #ifdef HAVE_GST_PBUTILS
    GstInstallPluginsReturn install_return;
    gchar **details;
    
    if (old != GST_STATE_READY || new != GST_STATE_PAUSED) {
        return;
    }
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->missing_element_details == NULL) {
        return;
    }
    
    g_return_if_fail (player->install_plugins_context != NULL);
    
    if (player->install_plugins_noprompt) {
        bp_missing_elements_handle_install_failed (player);
        return;
    }
    
    details = bp_slist_to_ptr_array (player->missing_element_details);
    player->install_plugins_context = gst_install_plugins_context_new ();
    
    #ifdef GDK_WINDOWING_X11
    if (player->window != NULL) {
        gst_install_plugins_context_set_xid (player->install_plugins_context, GDK_WINDOW_XWINDOW (player->window));
    }
    #endif
    
    install_return = gst_install_plugins_async (details, player->install_plugins_context, 
        bp_missing_elements_handle_install_result, player);
    
    if (install_return != GST_INSTALL_PLUGINS_STARTED_OK) {
        bp_missing_elements_handle_install_failed (player);
        
        gst_install_plugins_context_free (player->install_plugins_context);
        player->install_plugins_context = NULL;
    } 
    
    g_strfreev (details);
    #endif
}
