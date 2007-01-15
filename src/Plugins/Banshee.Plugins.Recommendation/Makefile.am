include $(top_srcdir)/build/Common.Makefile

ASSEMBLY = Banshee.Plugins.Recommendation.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-recommendation.schemas.in

SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/RecommendationPlugin.cs \
	$(srcdir)/RecommendationPane.cs \
	$(srcdir)/RecommendationUtilities.cs

MENU_RESOURCE = RecommendationMenu.xml
NO_RESULTS_RESOURCE = no-results.png

plugindir = $(pkglibdir)/Banshee.Plugins
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) -resource:$(srcdir)/$(NO_RESULTS_RESOURCE),$(NO_RESULTS_RESOURCE) $(SOURCES) $(LINK_GTK) $(LINK_MONO_UNIX) -r:System.Web

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES) $(MENU_RESOURCE) $(NO_RESULTS_RESOURCE) $(ASSEMBLY_GCONF_SCHEMA)
CLEANFILES = *.dll *.mdb
DISTCLEANFILES = *.pidb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
