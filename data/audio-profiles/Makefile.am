audioprofilesdir = $(pkgdatadir)/audio-profiles
audioprofiles_in_files = $(wildcard *.xml.in)
audioprofiles_DATA = $(audioprofiles_in_files:.xml.in=.xml)

@INTLTOOL_XML_RULE@

EXTRA_DIST = $(audioprofiles_in_files)
DISTCLEANFILES = *.xml
MAINTAINERCLEANFILES = Makefile.in

