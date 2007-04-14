include $(top_srcdir)/build/Common.Makefile

CUSTOM_MCS_FLAGS = -resource:$(srcdir)/BookmarksMenu.xml,BookmarksMenu.xml

ASSEMBLY = Banshee.Plugins.Bookmarks.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-bookmarks.schemas.in

SOURCES = \
	$(srcdir)/BookmarksPlugin.cs \
	$(top_srcdir)/src/AssemblyInfo.cs

plugindir = $(pkglibdir)/Banshee.Plugins
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -r:System.Data $(CUSTOM_MCS_FLAGS) -out:$@ $(LINK_GTK) $(SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES) BookmarksMenu.xml $(ASSEMBLY_GCONF_SCHEMA)
CLEANFILES = *.dll *.mdb
MAINTAINERCLEANFILES = Makefile.in *.pidb
DISTCLEANFILES = $(schema_DATA)

