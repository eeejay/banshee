include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = NotificationAreaIcon
ASSEMBLY = $(ASSEMBLY_NAME).dll

notificationareaicondir = $(pkglibdir)/Banshee.Plugins
notificationareaicon_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

MENU_RESOURCE = NotificationAreaIconMenu.xml

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/NotificationAreaIconPlugin.cs \
	$(srcdir)/NotificationAreaIconConfigPage.cs \
	$(srcdir)/NotificationArea.cs

schema_in_files = banshee-notificationareaicon.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(MENU_RESOURCE)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in $(schema_in_files) $(MENU_RESOURCE)

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in

if GCONF_SCHEMAS_INSTALL
install-data-local:
	if [ -z "$(DESTDIR)" ]; then \
		GCONF_CONFIG_SOURCE="" $(GCONFTOOL) --makefile-install-rule $(builddir)$(schema_DATA); \
	fi
endif

