include $(srcdir)/../gst-package.mk

plugindir = $(pkglibdir)/gstreamer-0.10
plugin_LTLIBRARIES = libgstequalizer.la

libgstequalizer_la_SOURCES = \
        gstiirequalizer.c gstiirequalizer.h \
        gstiirequalizer10bands.c gstiirequalizer10bands.h

libgstequalizer_la_CFLAGS = $(GST_CFLAGS) $(GST_INFO_FLAGS)
libgstequalizer_la_LIBADD = $(GST_LIBS) -lgstaudio-0.10 -lm
libgstequalizer_la_LDFLAGS = -avoid-version -module

noinst_HEADERS = gstiirequalizer.h

MAINTAINERCLEANFILES = Makefile.in

