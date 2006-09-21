BANSHEE_IMPORT = banshee-import
BANSHEE_IMPORT_ASSEMBLY = $(BANSHEE_IMPORT).exe
DBUS_CLIENT_ASSEMBLY = dbus-client.exe

DIR_DBUS = $(top_builddir)/dbus-sharp
LINK_DBUS = \
	-r:$(DIR_DBUS)/NDesk.DBus.dll \
	-r:$(DIR_DBUS)/NDesk.DBus.GLib.dll

#toolsdir = $(pkglibdir)
#tools_SCRIPTS = $(BANSHEE_IMPORT_ASSEMBLY)
#bin_SCRIPTS = $(BANSHEE_IMPORT)

BANSHEE_IMPORT_SOURCES = $(srcdir)/BansheeImport.cs
DBUS_CLIENT_SOURCES = $(srcdir)/BansheeDBusClient.cs

all: $(BANSHEE_IMPORT_ASSEMBLY) $(DBUS_CLIENT_ASSEMBLY)

$(BANSHEE_IMPORT_ASSEMBLY): $(BANSHEE_IMPORT_SOURCES)
	$(MCS) -out:$@ -r:Mono.Posix -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll $(BANSHEE_IMPORT_SOURCES)

$(DBUS_CLIENT_ASSEMBLY): $(DBUS_CLIENT_SOURCES)
	$(MCS) -out:$@ $(LINK_DBUS) $(GTKSHARP_LIBS) $(DBUS_CLIENT_SOURCES)

EXTRA_DIST = \
	$(BANSHEE_IMPORT).in \
	$(BANSHEE_IMPORT_SOURCES) \
	$(DBUS_CLIENT_SOURCES)

CLEANFILES = *.exe *.mdb
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

