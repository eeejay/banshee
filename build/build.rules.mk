SOURCES_EXPANDED = $(foreach expr, $(SOURCES), $(wildcard $(expr)))
SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES_EXPANDED))

RESOURCES_EXPANDED = $(foreach expr, $(RESOURCES), $(wildcard $(expr)))
RESOURCES_EXPANDED_FULL = $(addprefix $(srcdir)/, $(RESOURCES_EXPANDED))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED_FULL), \
	-resource:$(resource),$(notdir $(resource)))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD)
	@echo "$(SOURCES_BUILD)" | tr [:space:] \\n > $(srcdir)/$(ASSEMBLY_FILE).sources
	$(BUILD) -target:$(TARGET) -out:$@ $(LINK) $(RESOURCES_BUILD) @$(srcdir)/$(ASSEMBLY_FILE).sources
	@rm -f $(srcdir)/$(ASSEMBLY_FILE).sources

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED)

CLEANFILES = $(ASSEMBLY_FILE) $(ASSEMBLY_FILE).mdb
MAINTAINERCLEANFILES = Makefile.in
