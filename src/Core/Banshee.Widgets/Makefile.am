ASSEMBLY = Banshee.Widgets
TARGET = library
LINK = $(REF_BANSHEE_WIDGETS)
ASSEMBLY_BUILD_FLAGS = -unsafe

SOURCES = \
	Banshee.Widgets/*.cs

RESOURCES = \
	Resources/rating-rated.png \
	Resources/rating-unrated.png

include $(top_srcdir)/build/build.mk

EXTRA_DIST += $(ASSEMBLY_FILE).config.in

#Banshee_Widgets_EXPANDED_RESOURCES = $(foreach resource,$(Banshee_Widgets_RESOURCES), $(top_srcdir)/$(resource))

#Banshee_Widgets_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(Banshee_Widgets_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))


