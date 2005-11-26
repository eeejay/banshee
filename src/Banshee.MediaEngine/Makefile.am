SUBDIRS = Gstreamer

if ENABLE_HELIX
SUBDIRS += Helix
endif

if ENABLE_VLC
SUBDIRS += Vlc
endif

DISTCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/mediaengines
