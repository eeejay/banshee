/* ex: set ts=4: */
/***************************************************************************
 *  cd-test.c
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
 
#include <stdlib.h>
#include <stdio.h>
#include <glib.h>

#include "cd-detect.h"
#include "cd-info.h"

void on_cd_added(const gchar *udi)
{
	g_printf("NEW AUDIO CD: %s\n", udi);

}

void on_device_removed(const gchar *udi)
{
	g_printf("REMOVED DEVICE: %s\n", udi);
}

gint main()
{
	GMainLoop *loop;
	CdDetect *cd_detect;
	CdDiskInfo *disk;
	DiskInfo **disks;
	int i;
		
	loop = g_main_loop_new(g_main_context_default(), FALSE);
		
	cd_detect = cd_detect_new();
	if(!cd_detect) {
		g_printf("Error: Could not initialize HAL\n");
		exit(1);
	}
	
	cd_detect_set_device_added_callback(cd_detect, on_cd_added);
	cd_detect_set_device_removed_callback(cd_detect, on_device_removed);

	disks = cd_detect_get_disk_array(cd_detect);

	for(i = 0; disks[i] != NULL; i++) {
		disk = cd_disk_info_new(disks[i]->device_node);
		g_printf("Tracks: %lld\n", disk->n_tracks);
		cd_disk_info_free(disk);
	}
		
	cd_detect_disk_array_free(disks);

//	g_printf("Listening for Audio-CD-specific HAL events...\n");
//	g_main_loop_run(loop);

	cd_detect_free(cd_detect);	

	exit(0);
}

