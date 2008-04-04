audioprofilesdir = $(pkgdatadir)/audio-profiles
audioprofiles_in_files = \
	base.xml.in \
	flac.xml.in \
	mp3-lame.xml.in \
	mp3-xing.xml.in \
	vorbis.xml.in \
	wavpack.xml.in \
	wav.xml.in \
	wma.xml.in
audioprofiles_DATA = $(audioprofiles_in_files:.xml.in=.xml)

@INTLTOOL_XML_RULE@

all:
	mkdir -p $(top_builddir)/bin/share/$(PACKAGE)/audio-profiles && cp $(wildcard $(srcdir)/*.xml) $(top_builddir)/bin/share/$(PACKAGE)/audio-profiles

EXTRA_DIST = $(audioprofiles_in_files)
DISTCLEANFILES = *.xml
MAINTAINERCLEANFILES = Makefile.in

