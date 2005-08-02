
#include <nautilus-burn-recorder.h>

struct NautilusBurnRecorderTrack *
nautilus_burn_glue_create_track (const char *filename,
				 NautilusBurnRecorderTrackType type)
{
	struct NautilusBurnRecorderTrack *track;

	track = g_new0 (typeof (struct NautilusBurnRecorderTrack), 1);

	track->type = type;
	
	if (type == NAUTILUS_BURN_RECORDER_TRACK_TYPE_AUDIO) {
		track->contents.audio.filename = g_strdup (filename);
	} else {
		track->contents.data.filename = g_strdup (filename);
	}

	return track;
}
