/***************************************************************************
 *  gst-transcode.h
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

#ifndef GST_TRANSCODE_H
#define GST_TRANSCODE_H

typedef struct GstTranscoder GstTranscoder;

typedef void (* GstTranscoderProgressCallback) (GstTranscoder *transcoder, gdouble progress);
typedef void (* GstTranscoderFinishedCallback) (GstTranscoder *transcoder);
typedef void (* GstTranscoderErrorCallback) (GstTranscoder *transcoder, gchar *error);

struct GstTranscoder {
    gboolean cancel;
    gchar *error;
    gboolean is_transcoding;
    GstTranscoderProgressCallback progress_cb;
    GstTranscoderFinishedCallback finished_cb;
    GstTranscoderErrorCallback error_cb);
};

GstTranscoder *gst_transcoder_new();
void gst_transcoder_free(GstTranscoder *encoder);
const gchar *gst_transcoder_get_error(GstTranscoder *encoder);
void gst_transcoder_cancel(GstTranscoder *encoder);
gboolean gst_transcoder_transcode(GstTranscoder *encoder, 
    const gchar *input_uri, const gchar *output_uri, 
    const gchar *encoder_pipeline, 
    GstTranscoderProgressCallback progress_cb);

#endif // GST_TRANSCODE_H
