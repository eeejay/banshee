/* ex: set ts=4: */
/***************************************************************************
 *  gst-init.c
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

#include <xing/gst-xing-encoder.h>

static gboolean gstreamer_initialized = FALSE;

#ifdef ENABLE_XING
static gboolean
register_elements(GstPlugin *plugin)
{
	return gst_element_register(plugin, 
		XING_MP3_ENCODER_NAME,
		GST_RANK_NONE, 
		XING_TYPE_MP3_ENCODER);
}

static GstPluginDesc xingenc_plugin_desc = {
	GST_VERSION_MAJOR,
	GST_VERSION_MINOR,
	XING_MP3_ENCODER_NAME,
	"Xing MP3 Encoder",
	register_elements,
	NULL,
	VERSION,
	"LGPL",
	"Banshee",
	"http://banshee-project.org",
	GST_PADDING_INIT
};
#endif

void gstreamer_initialize()
{
	if(gstreamer_initialized)
		return;

	gst_init(NULL, NULL);
	
#ifdef ENABLE_XING
	g_message("Registering GStreamer plugin for the Xing Encoder");
	_gst_plugin_register_static(&xingenc_plugin_desc);
#endif

	gstreamer_initialized = TRUE;
}
