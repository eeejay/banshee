SOURCES_EXPANDED = $(foreach expr, $(SOURCES), $(wildcard $(expr)))
SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES_EXPANDED))

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD)
	$(BUILD) -target:$(TARGET) -out:$@ \
		$(LINK) \
		$(RESOURCES_BUILD) \
		$(SOURCES_BUILD)

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED)

CLEANFILES = $(ASSEMBLY_FILE) $(ASSEMBLY_FILE).mdb
MAINTAINERCLEANFILES = Makefile.in
