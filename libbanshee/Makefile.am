INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(HAL_CFLAGS) \
	$(GST_CFLAGS) \
	$(GTK_CFLAGS) \
	$(LIBNAUTILUS_BURN_CFLAGS)

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

GST_0_10_CFILES = \
	gst-playback-0.10.c \
	gst-misc-0.10.c \
	gst-transcode-0.10.c \
	gst-cd-rip-0.10.c \
	gst-mbtrm.c

GST_CFILES = $(GST_0_10_CFILES)

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES = \
	gst-playback.h \
	gst-misc.h \
	gst-transcode.h \
	gst-cd-rip.h \
	gst-tagger.c \
	gst-tagger.h \
	gst-mbtrm.h \
	gtksharp-glue.c \
	nautilus-burn.c \
	$(GST_CFILES)

libbanshee_la_LIBADD = $(GTK_LIBS) $(GST_LIBS) $(LIBNAUTILUS_BURN_LIBS)

CLEANFILES = gst-player-test gst-encode-test
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(GST_0_10_CFILES) 

if ENABLE_TESTS
endif

