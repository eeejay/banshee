MCS_FLAGS = -debug
ASSEMBLY_NAME = Audioscrobbler
ASSEMBLY = $(ASSEMBLY_NAME).dll

audioscrobblerdir = $(pkglibdir)/Banshee.Plugins
audioscrobbler_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

MENU_RESOURCE=AudioscrobblerMenu.xml

ASSEMBLY_SOURCES = \
	$(srcdir)/AudioscrobblerPlugin.cs \
	$(srcdir)/AudioscrobblerConfigDialog.cs \
	$(srcdir)/Engine.cs \
	$(srcdir)/Queue.cs

PKG_REFERENCES = \
	$(GTKSHARP_LIBS) \
	-r:System.Web.dll \
	-r:Mono.Security.dll \
	-r:Mono.Posix.dll

schema_in_files = audioscrobbler.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(MENU_RESOURCE)
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES) -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll $(PKG_REFERENCES) -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) -resource:$(srcdir)/audioscrobbler-logo.png,audioscrobbler-logo.png

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

