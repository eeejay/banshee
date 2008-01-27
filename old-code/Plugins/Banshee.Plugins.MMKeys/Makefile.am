include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.Plugins.MMKeys
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-mmkeys.schemas.in

fsmondir = $(pkglibdir)/Banshee.Plugins
fsmon_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/MMKeysPlugin.cs \
	$(srcdir)/MMKeysConfigPage.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES) 
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(ASSEMBLY).config.in \
	$(ASSEMBLY_GCONF_SCHEMA)

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in

