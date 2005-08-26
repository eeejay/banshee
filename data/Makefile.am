SUBDIRS = \
	glade \
	images

desktop_in_files=banshee.desktop.in
desktop_files=$(desktop_in_files:.desktop.in=.desktop)
@INTLTOOL_DESKTOP_RULE@

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

EXTRA_DIST =            \
	Tables.sql          \
	$(desktop_in_files) \
	$(desktop_files)
	
DISTCLEANFILES = \
	Makefile.in  \
	$(desktop_files)

