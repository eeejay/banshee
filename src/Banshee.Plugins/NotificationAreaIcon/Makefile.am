include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = NotificationAreaIcon
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-notificationarea.schemas.in

notificationareaicondir = $(pkglibdir)/Banshee.Plugins
notificationareaicon_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

MENU_RESOURCE = NotificationAreaIconMenu.xml

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/NotificationAreaIconPlugin.cs \
	$(srcdir)/NotificationAreaIconConfigPage.cs \
	$(srcdir)/NotificationArea.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(MENU_RESOURCE)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) $(ASSEMBLY_SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(ASSEMBLY).config.in \
	$(MENU_RESOURCE) \
	$(ASSEMBLY_GCONF_SCHEMA)

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in

