SUBDIRS = Banshee.MediaEngine.GStreamer

if ENABLE_HELIX
SUBDIRS += Banshee.MediaEngine.Helix
endif

MAINTAINERCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/Banshee.MediaEngine

