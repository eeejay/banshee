if HAVE_ID3TAG
id3_vfs_dir = id3-vfs
id3_vfs_lib = id3-vfs/libid3-vfs.la
endif

SUBDIRS = $(id3_vfs_dir) egg

INCLUDES =               \
	$(SONANCE_CFLAGS)    \
	$(OGG_CFLAGS)        \
	$(ID3TAG_CFLAGS)     \
	$(VORBIS_CFLAGS)     \
	$(VORBISFILE_CFLAGS)

sonancelibdir = $(pkglibdir)
sonancelib_LTLIBRARIES = libsonance.la

libsonance_la_SOURCES =       \
	mm-keys.c mm-keys.h       \
	metadata.c metadata.h     \
	ogg-helper.c ogg-helper.h 

libsonance_la_LIBADD = $(SONANCE_LIBS) $(OGG_LIBS) $(VORBIS_LIBS) $(VORBISFILE_LIBS) $(ID3TAG_LIBS) $(FLAC_LIBS) $(FAAD_LIBS) egg/libegg.la $(id3_vfs_lib)
