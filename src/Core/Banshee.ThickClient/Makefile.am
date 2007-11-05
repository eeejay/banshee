ASSEMBLY = Banshee.ThickClient
TARGET = library
ASSEMBLY_BUILD_FLAGS = -unsafe
LINK = $(REF_BANSHEE_THICKCLIENT)

SOURCES = \
	Banshee.Gui/*.cs \
	Banshee.Gui.DragDrop/*.cs  \
	Banshee.Gui.Dialogs/*.cs \
	Banshee.Sources.Gui/*.cs \
	Banshee.Collection.Gui/*.cs

RESOURCES = \
	$(wildcard Resources/*)

include $(top_srcdir)/build/build.mk

