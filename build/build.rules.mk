SOURCES_EXPANDED = $(foreach expr, $(SOURCES), $(wildcard $(expr)))
SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES_EXPANDED))

RESOURCES_EXPANDED = $(foreach expr, $(RESOURCES), $(wildcard $(expr)))
RESOURCES_EXPANDED_FULL = $(addprefix $(srcdir)/, $(RESOURCES_EXPANDED))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED_FULL), \
	-resource:$(resource),$(notdir $(resource)))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

moduledir = $(INSTALL_DIR_RESOLVED)
module_SCRIPTS = $(ASSEMBLY_FILE) $(ASSEMBLY_FILE).mdb

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED_FULL)
	@echo "$(SOURCES_BUILD)" | tr [:space:] \\n > $(ASSEMBLY_FILE).sources
	$(BUILD) -target:$(TARGET) -out:$@ $(LINK) $(RESOURCES_BUILD) @$(ASSEMBLY_FILE).sources
	@rm -f $(ASSEMBLY_FILE).sources

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED_FULL)

CLEANFILES = $(ASSEMBLY_FILE) $(ASSEMBLY_FILE).mdb *.dll *.mdb *.exe
MAINTAINERCLEANFILES = Makefile.in
