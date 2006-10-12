SUBDIRS = \
	images

audioprofilesdir = $(pkgdatadir)
audioprofiles_DATA = $(srcdir)/audio-profiles.xml

desktopdir = $(datadir)/applications
desktop_in_files=banshee.desktop.in.in
desktop_DATA=$(desktop_in_files:.desktop.in.in=.desktop)

%.desktop.in: %.desktop.in.in
	sed -e "s/\@VERSION\@/$(VERSION)/" < $< > $@

@INTLTOOL_DESKTOP_RULE@

schema_in_files = banshee.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

dbusservice_in_files = org.gnome.Banshee.service.in
dbusservicedir = $(datadir)/dbus-1/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

all: translator-extract.exe

translator-extract.exe: TranslatorExtract.cs
	$(MCS) -out:$@ $< && $(MONO) $@ $(top_builddir)/po > translators.xml

CLEANFILES = \
	*.gladep \
	*.bak \
	translators.xml \
	translator-extract.exe

EXTRA_DIST = \
	UIManagerLayout.xml \
	banshee.glade \
	banshee-dialogs.glade \
	contributors.xml \
	audio-profiles.xml \
	TranslatorExtract.cs \
	$(desktop_in_files) \
	$(schema_in_files) \
	$(dbusservice_in_files)

DISTCLEANFILES =	\
	$(dbusservice_DATA) \
	$(desktop_DATA) \
	$(schema_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in

if GCONF_SCHEMAS_INSTALL
install-data-local:
	if [ -z "$(DESTDIR)" ]; then \
		GCONF_CONFIG_SOURCE="" $(GCONFTOOL) --makefile-install-rule $(builddir)$(schema_DATA); \
	fi
endif

