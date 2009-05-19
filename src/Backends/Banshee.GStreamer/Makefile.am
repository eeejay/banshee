ASSEMBLY = Banshee.GStreamer
TARGET = library
LINK = $(REF_BACKEND_GSTREAMER)
SOURCES =  \
	Banshee.GStreamer/AudioCdRipper.cs \
	Banshee.GStreamer/BpmDetector.cs \
	Banshee.GStreamer/GstErrors.cs \
	Banshee.GStreamer/PlayerEngine.cs \
	Banshee.GStreamer/Service.cs \
	Banshee.GStreamer/TagList.cs \
	Banshee.GStreamer/Transcoder.cs
RESOURCES = Banshee.GStreamer.addin.xml
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

include $(top_srcdir)/build/build.mk

EXTRA_DIST += Banshee.GStreamer.dll.config
module_SCRIPTS += Banshee.GStreamer.dll.config
