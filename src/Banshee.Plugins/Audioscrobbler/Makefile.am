include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Banshee.Plugins.Audioscrobbler
ASSEMBLY = $(ASSEMBLY_NAME).dll

audioscrobblerdir = $(pkglibdir)/Banshee.Plugins
audioscrobbler_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

MENU_RESOURCE = AudioscrobblerMenu.xml

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/AudioscrobblerPlugin.cs \
	$(srcdir)/AudioscrobblerConfigDialog.cs \
	$(srcdir)/Engine.cs \
	$(srcdir)/Queue.cs

schema_in_files = audioscrobbler.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(MENU_RESOURCE)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_LAST_FM) $(LINK_GTK) $(LINK_MONO_UNIX) -r:System.Web -r:Mono.Security -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) -resource:$(srcdir)/audioscrobbler-logo.png,audioscrobbler-logo.png $(ASSEMBLY_SOURCES) 

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in $(schema_in_files) $(MENU_RESOURCE) audioscrobbler-logo.png

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in

if GCONF_SCHEMAS_INSTALL
install-data-local:
	if [ -z "$(DESTDIR)" ]; then \
		GCONF_CONFIG_SOURCE="" $(GCONFTOOL) --makefile-install-rule $(builddir)$(schema_DATA); \
	fi
endif

