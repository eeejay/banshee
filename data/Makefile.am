SUBDIRS = \
	audio-profiles \
	images

desktopdir = $(datadir)/applications
desktop_in_files=banshee.desktop.in.in
desktop_DATA=$(desktop_in_files:.desktop.in.in=.desktop)

%.desktop.in: %.desktop.in.in
	sed -e "s/\@VERSION\@/$(VERSION)/" < $< > $@

@INTLTOOL_DESKTOP_RULE@

dbusservice_in_files = org.gnome.Banshee.service.in
dbusservicedir = $(datadir)/dbus-1/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

EXTRA_DIST = \
	$(desktop_in_files) \
	$(dbusservice_in_files)

DISTCLEANFILES =	\
	$(dbusservice_DATA) \
	$(desktop_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in

