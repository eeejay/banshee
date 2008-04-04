INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	-DINSIDE_LIBBANSHEE_I_SWEAR \
	$(LIBBANSHEE_CFLAGS) \
	$(GST_CFLAGS) \
	$(LIBNAUTILUS_BURN_CFLAGS)

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES =  \
	banshee-player.c \
	banshee-player-cdda.c \
	banshee-player-equalizer.c \
	banshee-player-missing-elements.c \
	banshee-player-pipeline.c \
	banshee-player-video.c \
	banshee-ripper.c \
	gst-misc-0.10.c \
	gst-transcode-0.10.c

noinst_HEADERS =  \
	banshee-player-cdda.h \
	banshee-player-missing-elements.h \
	banshee-player-pipeline.h \
	banshee-player-private.h \
	banshee-player-video.h \
	gst-transcode.h

libbanshee_la_LIBADD = \
	$(LIBBANSHEE_LIBS) \
	$(GST_LIBS) \
	$(LIBNAUTILUS_BURN_LIBS)

all: $(top_builddir)/bin/libbanshee.so

$(top_builddir)/bin/libbanshee.so: .libs/libbanshee.so	
	mkdir -p $(top_builddir)/bin
	cp -f $< $@

CLEANFILES = $(top_builddir)/bin/libbanshee.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(libbanshee_la_SOURCES)

