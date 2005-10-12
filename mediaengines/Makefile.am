SUBDIRS = gst

if ENABLE_HELIX
SUBDIRS += helix
endif

if ENABLE_VLC
SUBDIRS += vlc
endif

DISTCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/mediaengines
