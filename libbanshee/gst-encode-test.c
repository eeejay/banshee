/************************************************************************
 *  gst-encode-test.c
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
 
#include <glib.h>
#include <stdlib.h>
#include <stdio.h>
#include <signal.h>
#include <glib/gstdio.h>

#include "gst-encode.h"
 
static GstFileEncoder *file_encoder = NULL;
 
void signal_cb(int signum)
{
	g_printf("Caught cancel signal... terminating encode process...\n");
	gst_file_encoder_encode_cancel(file_encoder);
	gst_file_encoder_free(file_encoder);
	g_printf("Encode process canceled by user\n");
	exit(1);
}
 
void encoder_progress_cb(GstFileEncoder *encoder, gdouble progress)
{
	g_printf("Progress: %g\n", progress);	
}

gint main(gint argc, gchar **argv)
{
	const gchar *infile, *outfile, *encode_pipeline;
	 
 	if(argc < 4) {
		g_printf("Usage: gst-encode <infile> <outfile> <encode-pipeline>\n");
		exit(1);
	}
	
	infile = argv[1];
	outfile = argv[2];
	encode_pipeline = argv[3];
	
	file_encoder = gst_file_encoder_new();
	if(file_encoder == NULL) {
		g_printerr("Could not construct file encoder\n");
		exit(1);
	}
	
	signal(SIGINT, signal_cb);
	
	g_printf("Starting encoding...\n");
	
	if(!gst_file_encoder_encode_file(file_encoder, infile, outfile, 
		encode_pipeline, encoder_progress_cb)) {
		g_printerr("Error: %s\n", gst_file_encoder_get_error(file_encoder));
		gst_file_encoder_free(file_encoder);
		exit(1);
	}
	
	gst_file_encoder_free(file_encoder);
	
	g_printf("\nFinished Encoding!\n");
	
	exit(0);
 }
