SUBDIRS = \
	glade \
	images

@INTLTOOL_DESKTOP_RULE@

desktop_in_files=banshee.desktop.in
desktop_files=$(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

@INTLTOOL_SCHEMAS_RULE@

schema_in_files = banshee.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

dbusservice_in_files = org.gnome.Banshee.service.in
dbusservicedir = $(libdir)/dbus-1.0/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

EXTRA_DIST =            \
	Tables.sql          \
	$(desktop_in_files) \
	$(desktop_files)    \
	$(schema_in_files)  \
	$(dbusservice_in_files)
	
DISTCLEANFILES =     \
	Makefile.in      \
	$(desktop_files) \
	$(schema_DATA)   \
	$(dbusservice_DATA)

if GCONF_SCHEMAS_INSTALL
install-data-local:
	GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) $(GCONFTOOL) --makefile-install-rule $(schema_DATA)
endif

