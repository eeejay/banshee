/* ex: set ts=4: */
/***************************************************************************
 *  cd-info.c
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

#include "cd-info.h"

#define TOC_OFFSET 150
#define SECTORS_PER_SEC 75
#define TIME_TO_SECTOR(t) ((t) / GST_SECOND * SECTORS_PER_SEC)
#define SECTOR_TO_TIME(s) (((s) / SECTORS_PER_SEC) * GST_SECOND)

void cd_disk_info_load(CdDiskInfo *disk);

CdTrackInfo *cd_track_info_new(int number, gint64 start_sector, 
	gint64 end_sector);
static void cd_track_info_free(CdTrackInfo *track);

/* CD Disk Info */

CdDiskInfo *
cd_disk_info_new(const gchar *device_node)
{
	CdDiskInfo *disk;
	
	disk = g_new0(CdDiskInfo, 1);
	
	disk->device_node = g_strdup(device_node);
	
	disk->n_tracks = 0;
	disk->total_sectors = 0;
	disk->total_time = 0;
	disk->tracks = NULL;
	
	cd_disk_info_load(disk);
	
	if(disk->n_tracks == 0) {
		cd_disk_info_free(disk);
		disk = NULL;
	}
	
	return disk;
}

void 
cd_disk_info_free(CdDiskInfo *disk)
{
	gint i;
	
	if(disk == NULL)
		return;
		
	if(disk->tracks != NULL) {
		for(i = 0; i < disk->n_tracks; i++) {
			cd_track_info_free(disk->tracks[i]);	
		}
		
		g_free(disk->tracks);
	}
	
	g_free(disk->device_node);
	g_free(disk);
	
	disk = NULL;
}

void
cd_disk_info_load(CdDiskInfo *disk)
{
	GstElement *source;
	GstPad *source_pad;
	GstFormat track_format, sector_format;
	GstFormat time_format = GST_FORMAT_TIME;
	gint64 start_sector = 0, end_sector;
	gint i;
	
	gst_init(NULL, NULL);
	
	source = gst_element_factory_make("cdparanoia", "cdparanoia");
	g_object_set(G_OBJECT(source), "device", disk->device_node, NULL);

	track_format = gst_format_get_by_nick("track");
	sector_format = gst_format_get_by_nick("sector");
	source_pad = gst_element_get_pad(source, "src");
	
	gst_element_set_state(source, GST_STATE_PAUSED);

	gst_pad_query(source_pad, GST_QUERY_TOTAL, &track_format, 
		&(disk->n_tracks));
	gst_pad_query(source_pad, GST_QUERY_TOTAL, &sector_format, 
		&(disk->total_sectors));
	disk->total_sectors += TOC_OFFSET;
	
	gst_pad_convert(source_pad, sector_format, disk->total_sectors, 
		&time_format, &(disk->total_time));
		
	if(disk->n_tracks <= 0) {
		gst_element_set_state(source, GST_STATE_NULL);
		gst_object_unref(GST_OBJECT(source));
	}
	
	disk->tracks = (CdTrackInfo **)g_new0(CdTrackInfo, disk->n_tracks + 1);
	
	for(i = 0; i <= disk->n_tracks; i++) {
		end_sector = 0;
		
		if(i < disk->n_tracks) {
			gst_pad_convert(source_pad, track_format, i, 
				&sector_format, &end_sector);
			end_sector += TOC_OFFSET;
		} else {
			end_sector = disk->total_sectors;
		}
		
		if(i > 0) {
			disk->tracks[i - 1] = cd_track_info_new(i - 1, start_sector,
				end_sector);
		}
		
		start_sector = end_sector;
	}
	
	gst_element_set_state(source, GST_STATE_NULL);
	gst_object_unref(GST_OBJECT(source));
}

/* Track Info */

CdTrackInfo *
cd_track_info_new(int number, gint64 start_sector, gint64 end_sector)
{
	CdTrackInfo *track;

	track = g_new0(CdTrackInfo, 1);

	track->number = number;
	
	track->start_sector = start_sector;
	track->end_sector = end_sector;
	track->sectors = track->end_sector - track->start_sector;
	
	track->start_time = SECTOR_TO_TIME(start_sector);
	track->end_time = SECTOR_TO_TIME(end_sector);
	
	track->duration = (track->end_time - track->start_time) / GST_SECOND;
	
	track->minutes = track->duration / 60;
	track->seconds = track->duration % 60;

	return track;
}

static void
cd_track_info_free(CdTrackInfo *track)
{
	g_free(track);
	track = NULL;
}

