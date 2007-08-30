include $(top_srcdir)/build/Common.Makefile

HYENA = Hyena
HYENA_ASSEMBLY = $(HYENA).dll

hyenadir = $(pkglibdir)
hyena_SCRIPTS = $(HYENA_ASSEMBLY) $(HYENA_ASSEMBLY).mdb

HYENA_SOURCE = \
	$(wildcard *.cs) \
	$(wildcard Hyena.Data/*.cs) \
	$(wildcard Hyena.Data.Query/*.cs)
HYENA_SOURCE_BUILD = $(addprefix $(srcdir)/, $(HYENA_SOURCE))

all: $(HYENA_ASSEMBLY)

$(HYENA_ASSEMBLY): $(HYENA_SOURCE_BUILD)
	$(BUILD_LIB) -out:$@ $(HYENA_SOURCE_BUILD)

EXTRA_DIST = \
	$(HYENA_SOURCE)

CLEANFILES = $(HYENA_ASSEMBLY) *.mdb *.dll *.pidb
MAINTAINERCLEANFILES = Makefile.in

