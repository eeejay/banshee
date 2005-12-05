/***************************************************************************
 *  gst-encode.h
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

#ifndef GST_ENCODE_H
#define GST_ENCODE_H

typedef struct {
    gboolean cancel;
    gchar *error;
} GstFileEncoder;

typedef void (* GstFileEncoderProgressCallback) (GstFileEncoder *encoder, 
    gdouble progress);

GstFileEncoder *gst_file_encoder_new();
void gst_file_encoder_free(GstFileEncoder *encoder);

gboolean gst_file_encoder_encode_file(GstFileEncoder *encoder, 
    const gchar *input_file, const gchar *output_file, 
    const gchar *encoder_pipeline, 
    GstFileEncoderProgressCallback progress_cb);

const gchar *gst_file_encoder_get_error(GstFileEncoder *encoder);
void gst_file_encoder_encode_cancel(GstFileEncoder *encoder);

#endif // GST_ENCODE_H
