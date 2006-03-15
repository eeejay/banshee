SUBDIRS = Gstreamer

if ENABLE_HELIX
SUBDIRS += Helix
endif

if ENABLE_VLC
SUBDIRS += Vlc
endif

MAINTAINERCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/Banshee.MediaEngine

