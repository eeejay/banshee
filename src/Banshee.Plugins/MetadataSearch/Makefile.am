MCS_FLAGS = -debug 
ASSEMBLY_NAME = MetadataSearch
ASSEMBLY = $(ASSEMBLY_NAME).dll

fsmondir = $(pkglibdir)/Banshee.Plugins
fsmon_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(srcdir)/MetadataSearchPlugin.cs
	
PKG_REFERENCES = \
	$(GTKSHARP_LIBS) \
	-r:System.Data \
	-r:Mono.Posix
	
schema_in_files = metadatasearch.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

$(ASSEMBLY): $(ASSEMBLY_SOURCES) 
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES) -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll $(PKG_REFERENCES)
	
EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in $(schema_in_files)

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb Makefile.in $(schema_DATA)

if GCONF_SCHEMAS_INSTALL
install-data-local:
	if [ -z "$(DESTDIR)" ]; then \
		GCONF_CONFIG_SOURCE="" $(GCONFTOOL) --makefile-install-rule $(builddir)$(schema_DATA); \
	fi
endif

