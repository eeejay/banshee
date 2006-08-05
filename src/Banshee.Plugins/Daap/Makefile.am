if DAAP_ENABLED
MCS_FLAGS = -debug 
ASSEMBLY_NAME = Daap
ASSEMBLY = $(ASSEMBLY_NAME).dll

daapdir = $(pkglibdir)/Banshee.Plugins
daap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/DaapPlugin.cs \
	$(srcdir)/DaapCore.cs \
	$(srcdir)/DaapSource.cs \
	$(srcdir)/DaapTrackInfo.cs \
	$(srcdir)/DaapLoginDialog.cs \
	$(srcdir)/DatabaseProxy.cs \
	$(srcdir)/DaapProxyWebServer.cs \
	$(srcdir)/DaapConfigPage.cs \
	$(srcdir)/daap-sharp/*.cs \
	$(srcdir)/daap-sharp/Mono.Zeroconf/*.cs
	
PKG_REFERENCES = \
	$(GTKSHARP_LIBS) \
	-r:Mono.Posix
	
schema_in_files = daap.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

if GCONF_SCHEMAS_INSTALL
install-data-local:
	if [ -z "$(DESTDIR)" ]; then \
		GCONF_CONFIG_SOURCE="" $(GCONFTOOL) --makefile-install-rule $(builddir)$(schema_DATA); \
	fi;
endif

endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES) 
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES) -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll $(DAAPSHARP_FLAGS) -r:System.Web -r:ICSharpCode.SharpZipLib $(PKG_REFERENCES) -resource:$(srcdir)/daap-sharp/content-codes,content-codes
	
EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in $(schema_in_files) $(srcdir)/daap-sharp/content-codes

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
