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
	$(gnome_media_cddbslave2_idl_sources) \
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
	gst-misc.h \
	cddb-slave-client.c \
	cddb-slave-client.h

libbanshee_la_LIBADD = $(GTK_LIBS) $(HAL_LIBS) $(GST_LIBS)

gnome_media_cddbslave2_idl_sources= \
	GNOME_Media_CDDBSlave2-stubs.c \
	GNOME_Media_CDDBSlave2-skels.c \
	GNOME_Media_CDDBSlave2.h \
	GNOME_Media_CDDBSlave2-common.c

$(gnome_media_cddbslave2_idl_sources): gnome_media_cddbslave2_idl_stamp
gnome_media_cddbslave2_idl_stamp: GNOME_Media_CDDBSlave2.idl $(ORBIT_IDL)
	$(ORBIT_IDL) $(IDL_INCLUDES) $<
	touch $@

CLEANFILES = cdtest $(gnome_media_cddbslave2_idl_sources) gnome_media_cddbslave2_idl_stamp
DISTCLEANFILES = Makefile.in

EXTRA_DIST = cddb-test.c cd-test.c gst-encode-test.c gst-player-engine-test.c GNOME_Media_CDDBSlave2.idl

if ENABLE_TESTS
noinst_PROGRAMS = cd-test gst-player-test gst-encode-test cddb-test
cd_test_SOURCES = cd-test.c
cd_test_LDADD = libbanshee.la

gst_player_test_SOURCES = gst-player-engine-test.c
gst_player_test_LDADD = libbanshee.la

gst_encode_test_SOURCES = gst-encode-test.c
gst_encode_test_LDADD = libbanshee.la

cddb_test_SOURCES = cddb-test.c
cddb_test_LDADD = libbanshee.la
endif


