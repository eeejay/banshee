MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = GtkSharpBackports
ASSEMBLY = $(ASSEMBLY_NAME).dll

gtksharpbackportsdir = $(pkglibdir)
gtksharpbackports_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(srcdir)/FileChooserButton.cs \
	$(srcdir)/AboutDialog.cs \
	$(srcdir)/AboutDialogActivateLinkFunc.cs \
	$(srcdir)/GtkSharp.AboutDialogActivateLinkFuncNative.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library $(GTKSHARP_LIBS) -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in 

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb Makefile.in 

