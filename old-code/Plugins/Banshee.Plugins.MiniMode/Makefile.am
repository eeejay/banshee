include $(top_srcdir)/build/Common.Makefile

CUSTOM_MCS_FLAGS = -resource:$(srcdir)/minimode.glade,minimode.glade

ASSEMBLY = Banshee.Plugins.MiniMode.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-minimode.schemas.in

SOURCES = \
	$(srcdir)/MiniModePlugin.cs \
	$(srcdir)/MiniModeWindow.cs \
	$(srcdir)/SourceComboBox.cs \
	$(srcdir)/SourceModel.cs \
	$(top_srcdir)/src/AssemblyInfo.cs

plugindir = $(pkglibdir)/Banshee.Plugins
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) $(CUSTOM_MCS_FLAGS) -out:$@ $(LINK_GTK) $(SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES) minimode.glade $(ASSEMBLY_GCONF_SCHEMA)
CLEANFILES = *.dll *.mdb
MAINTAINERCLEANFILES = Makefile.in *.pidb
DISTCLEANFILES = minimode.gladep* minimode.glade.bak $(schema_DATA)

