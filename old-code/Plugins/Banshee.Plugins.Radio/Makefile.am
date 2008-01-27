include $(top_srcdir)/build/Common.Makefile

CUSTOM_MCS_FLAGS = $(MCS_FLAGS) \
	-resource:$(srcdir)/source-radio.png,source-radio.png \
	-resource:$(srcdir)/dialog-radio.png,dialog-radio.png \
	-resource:$(srcdir)/dialog-radio.svg,dialog-radio.svg \
	-resource:$(srcdir)/radio.png,radio.png \
	-resource:$(srcdir)/RadioActions.xml,RadioActions.xml

ASSEMBLY = Banshee.Plugins.Radio.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-radio.schemas.in
SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/RadioPlugin.cs \
	$(srcdir)/RadioSource.cs \
	$(srcdir)/StationManager.cs \
	$(srcdir)/StationModel.cs \
	$(srcdir)/StationView.cs \
	$(srcdir)/StationGroup.cs \
	$(srcdir)/StationEditor.cs \
	$(srcdir)/CellRendererStation.cs

plugindir = $(pkglibdir)/Banshee.Plugins
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) RadioActions.xml
	$(BUILD_LIB_BANSHEE_CORE) $(CUSTOM_MCS_FLAGS) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) $(SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES) RadioActions.xml radio.png source-radio.png dialog-radio.png dialog-radio.svg $(ASSEMBLY_GCONF_SCHEMA)
CLEANFILES = *.dll *.mdb
MAINTAINERCLEANFILES = Makefile.in *.pidb
DISTCLEANFILES = $(schema_DATA)

