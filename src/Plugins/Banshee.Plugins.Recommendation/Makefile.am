include $(top_srcdir)/build/Common.Makefile

ASSEMBLY = Banshee.Plugins.Recommendation.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-recommendation.schemas.in

SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/RecommendationPlugin.cs \
	$(srcdir)/RecommendationPane.cs \
	$(srcdir)/RecommendationUtilities.cs

RESOURCES = \
	$(srcdir)/RecommendationMenu.xml \
	$(srcdir)/no-results.png \
	$(srcdir)/generic-artist.png

RESOURCES_BUILD = $(foreach resource,$(RESOURCES),$(addprefix -resource:,$(resource)),$(notdir $(resource)))

plugindir = $(pkglibdir)/Banshee.Plugins
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(RESOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(RESOURCES_BUILD) $(SOURCES) $(LINK_GTK) $(LINK_MONO_UNIX) -r:System.Web

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES) $(RESOURCES) $(NO_RESULTS_RESOURCE) $(ASSEMBLY_GCONF_SCHEMA)
CLEANFILES = *.dll *.mdb
DISTCLEANFILES = *.pidb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
