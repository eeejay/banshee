SUBDIRS = \
	audio-profiles \
	desktop-files \
	icon-theme-hicolor

dbusservice_in_files = \
	org.bansheeproject.Banshee.service.in \
	org.bansheeproject.CollectionIndexer.service.in
dbusservicedir = $(datadir)/dbus-1/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

EXTRA_DIST = \
	$(dbusservice_in_files)

DISTCLEANFILES = \
	$(dbusservice_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in

