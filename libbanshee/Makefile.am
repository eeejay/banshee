SUBDIRS = xing

INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(HAL_CFLAGS) \
	$(GST_CFLAGS)

if ENABLE_XING
	INCLUDES += -I$(top_builddir)/libbanshee/xing
endif

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES = \
	cd-detect.c \
	cd-detect.h \
	cd-info.c   \
	cd-info.h   \
	cd-rip.c    \
	cd-rip.h    \
	gst-init.c  \
	gst-init.h  \
	gst-encode.c \
	gst-encode.h \
	gst-player-engine.c \
	gst-player-engine.h \
	gst-misc.c \
	gst-misc.h

if ENABLE_XING
libbanshee_la_LIBADD = $(HAL_LIBS) $(GST_LIBS) $(top_builddir)/libbanshee/xing/libgstxingenc.la
else
libbanshee_la_LIBADD = $(HAL_LIBS) $(GST_LIBS)
endif

CLEANFILES = cdtest
DISTCLEANFILES = Makefile.in

EXTRA_DIST = cd-test.c gst-encode-test.c gst-player-engine-test.c

if ENABLE_TESTS
noinst_PROGRAMS = cd-test gst-player-test gst-encode-test
cd_test_SOURCES = cd-test.c
cd_test_LDADD = libbanshee.la

gst_player_test_SOURCES = gst-player-engine-test.c
gst_player_test_LDADD = libbanshee.la

gst_encode_test_SOURCES = gst-encode-test.c
gst_encode_test_LDADD = libbanshee.la
endif


