SUBDIRS = Banshee.MediaEngine.GStreamer

if ENABLE_HELIX
SUBDIRS += Banshee.MediaEngine.HelixRemote
endif

MAINTAINERCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/Banshee.MediaEngine

