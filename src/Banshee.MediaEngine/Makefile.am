SUBDIRS = Gstreamer

if ENABLE_HELIX
SUBDIRS += Helix
endif

MAINTAINERCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/Banshee.MediaEngine

