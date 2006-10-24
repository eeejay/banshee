include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = BooBuddy
ASSEMBLY = $(ASSEMBLY_NAME).dll

boobuddydir = $(pkglibdir)
boobuddy_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
	$(srcdir)/BooBuddyInterpreter.cs \
	$(srcdir)/BooBuddyWindow.cs \
	$(srcdir)/BooBuddyShell.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB) -out:$@ $(LINK_GTK) $(LINK_BOO) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
MAINTAINERCLEANFILES = Makefile.in

