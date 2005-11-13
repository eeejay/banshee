SUBDIRS = xing

INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(HAL_CFLAGS) \
	$(GST_CFLAGS) \
	$(GTK_CFLAGS)

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES = \
	cd-detect.c \
	cd-detect.h \
	cd-rip.c    \
	cd-rip.h    \
	gst-init.c  \
	gst-init.h  \
	gst-encode.c \
	gst-encode.h \
	gst-player-engine.c \
	gst-player-engine.h \
	gst-misc.c \
	gst-misc.h \
	inotify-glue.c \
	inotify.h \
	inotify-syscalls.h

libbanshee_la_LIBADD = $(GTK_LIBS) $(HAL_LIBS) $(GST_LIBS)

CLEANFILES = gst-player-test gst-encode-test
DISTCLEANFILES = Makefile.in

EXTRA_DIST = gst-encode-test.c gst-player-engine-test.c

if ENABLE_TESTS
noinst_PROGRAMS = gst-player-test gst-encode-test

gst_player_test_SOURCES = gst-player-engine-test.c
gst_player_test_LDADD = libbanshee.la

gst_encode_test_SOURCES = gst-encode-test.c
gst_encode_test_LDADD = libbanshee.la

endif

player-test:
	$(CC) -o gst-player-test $(GST_CFLAGS) $(GST_LIBS) gst-player-engine.c gst-player-engine-test.c
