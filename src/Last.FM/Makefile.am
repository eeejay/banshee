MCS_FLAGS = -debug
ASSEMBLY_NAME = Last.FM
ASSEMBLY = $(ASSEMBLY_NAME).dll

last_fmdir= $(pkglibdir)
last_fm_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
	$(srcdir)/Account.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$@ -r:Mono.Posix -r:$(top_builddir)/src/Gnome.Keyring/Gnome.Keyring.dll -target:library $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

