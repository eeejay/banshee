INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(LIBBANSHEE_CFLAGS) \
	$(GST_CFLAGS) 

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES =  \
	banshee-gst.c \
	banshee-player.c \
	banshee-player-cdda.c \
	banshee-player-equalizer.c \
	banshee-player-missing-elements.c \
	banshee-player-pipeline.c \
	banshee-player-replaygain.c \
	banshee-player-video.c \
	banshee-player-vis.c \
	banshee-ripper.c \
	banshee-bpmdetector.c \
	banshee-tagger.c \
	banshee-transcoder.c

if HAVE_CLUTTER
libbanshee_la_SOURCES += clutter-gst-video-sink.c
endif

libbanshee_la_LIBADD = \
	$(LIBBANSHEE_LIBS) \
	$(GST_LIBS)

all: $(top_builddir)/bin/libbanshee.so

$(top_builddir)/bin/libbanshee.so: libbanshee.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/libbanshee.so $@

CLEANFILES = $(top_builddir)/bin/libbanshee.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = \
	$(libbanshee_la_SOURCES) \
	$(wildcard *.h) \
	$(wildcard clutter-*.c)
