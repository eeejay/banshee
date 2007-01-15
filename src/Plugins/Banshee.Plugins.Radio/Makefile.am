include $(top_srcdir)/build/Common.Makefile

CUSTOM_MCS_FLAGS = $(MCS_FLAGS) \
	-unsafe \
	-resource:$(srcdir)/source-radio.png,source-radio.png \
	-resource:$(srcdir)/radio.png,radio.png \
	-resource:$(srcdir)/RadioActions.xml,RadioActions.xml

ASSEMBLY = Banshee.Plugins.Radio.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-radio.schemas.in
SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/RadioPlugin.cs \
	$(srcdir)/RadioSource.cs \
	$(srcdir)/RadioTrackInfo.cs \
	$(srcdir)/StationManager.cs \
	$(srcdir)/StationModel.cs \
	$(srcdir)/StationView.cs \
	$(srcdir)/StationGroup.cs \
	$(srcdir)/StationGroupEditor.cs \
	$(srcdir)/CellRendererStation.cs \
	$(srcdir)/totem-plparser/*.cs

plugindir = $(pkglibdir)/Banshee.Plugins
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) $(CUSTOM_MCS_FLAGS) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) $(SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES) RadioActions.xml radio.png source-radio.png $(ASSEMBLY_GCONF_SCHEMA)
CLEANFILES = *.dll *.mdb
MAINTAINERCLEANFILES = Makefile.in *.pidb
DISTCLEANFILES = $(schema_DATA)

