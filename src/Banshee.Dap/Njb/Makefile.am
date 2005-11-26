MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Dap.Njb
ASSEMBLY = $(ASSEMBLY_NAME).dll

bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
	$(srcdir)/NjbDap.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) 

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb Makefile.in 

