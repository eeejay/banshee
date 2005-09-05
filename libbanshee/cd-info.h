/* ex: set ts=4: */
/***************************************************************************
 *  cd-info.h
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
 
#ifndef CD_INFO_H
#define CD_INFO_H

#include <glib.h>

typedef struct {	
	gint number;
	
	gint duration;
	gint minutes;
	gint seconds;
	
	gint64 start_sector;
	gint64 end_sector;
	gint64 sectors;
	gint64 start_time;
	gint64 end_time;
} CdTrackInfo;

typedef struct {
	gchar *device_node;
	gchar *disk_id;

	gint64 n_tracks;
	gint64 total_sectors;
	gint64 total_time;
	gint64 total_seconds;
	
	CdTrackInfo **tracks;
	gchar *offsets;
} CdDiskInfo;

CdDiskInfo *cd_disk_info_new(const gchar *device_node);
void cd_disk_info_free(CdDiskInfo *disk);

#endif /* CD_INFO_H */
