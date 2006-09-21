if DAAP_ENABLED
include $(top_srcdir)/Makefile.include

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
	$(srcdir)/DaapPlaylistSource.cs \
	$(srcdir)/DaapProxyWebServer.cs \
	$(srcdir)/DaapConfigPage.cs \
	$(srcdir)/daap-sharp/*.cs \
	$(srcdir)/daap-sharp/Mono.Zeroconf/*.cs

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
	$(BUILD_LIB_BANSHEE_CORE) -out:$@  $(DAAPSHARP_FLAGS) $(LINK_GTK) $(LINK_MONO_UNIX) -r:ICSharpCode.SharpZipLib -r:System.Web -resource:$(srcdir)/daap-sharp/content-codes,content-codes $(ASSEMBLY_SOURCES)
	
EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in $(schema_in_files) $(srcdir)/daap-sharp/content-codes

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
