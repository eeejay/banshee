bnpx11dir = $(pkglibdir)/Backends
bnpx11_LTLIBRARIES = libbnpx11.la

libbnpx11_la_SOURCES = \
	bacon-resize.c \
	bacon-resize.h

libbnpx11_la_CFLAGS = $(BNPX_CFLAGS) $(BNPX_GTK_CFLAGS)
libbnpx11_la_LIBADD = $(BNPX_LIBS) $(BNPX_GTK_LIBS) -lX11 -lXxf86vm
libbnpx11_la_LDFLAGS = -avoid-version -module

noinst_HEADERS = bacon-resize.h

all: $(top_builddir)/bin/libbnpx11.so

$(top_builddir)/bin/libbnpx11.so: .libs/libbnpx11.so
	mkdir -p $(top_builddir)/bin
	cp -f $< $@

CLEANFILES = $(top_builddir)/bin/libbnpx11.so

MAINTAINERCLEANFILES = Makefile.in

