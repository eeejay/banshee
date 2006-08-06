MCS_FLAGS = -debug
ASSEMBLY_NAME = Gnome.Keyring
ASSEMBLY = $(ASSEMBLY_NAME).dll

gnome_keyringdir= $(pkglibdir)
gnome_keyring_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = $(srcdir)/*.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$@ -r:Mono.Posix -target:library $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

