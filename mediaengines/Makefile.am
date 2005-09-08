SUBDIRS = \
	gst 

if ENABLE_HELIX
SUBDIRS += helix
endif

DISTCLEANFILES = Makefile.in

uninstall-hook:
	rm -rf $(pkglibdir)/mediaengines
