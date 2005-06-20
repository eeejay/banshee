noinst_LTLIBRARIES = libid3-vfs.la

libid3_vfs_la_SOURCES = id3-vfs.c id3-vfs.h field.h file.h global.h tag.h mp3bitrate.c mp3bitrate.h

INCLUDES = 						\
	-DG_LOG_DOMAIN=\"id3-vfs\"			\
	-I$(top_srcdir) 				\
	$(SONANCE_CFLAGS)				\
	$(ID3TAG_CFLAGS)				\
	$(WARN_CFLAGS)					\
	-DG_DISABLE_DEPRECATED
