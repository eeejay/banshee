ASSEMBLY = Banshee.RemoteAudio
TARGET = library
LINK = $(REF_EXTENSION_REMOTE_AUDIO)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = \
	Banshee.RemoteAudio/RemoteAudioService.cs \
	Banshee.RemoteAudio/RemoteSpeaker.cs

RESOURCES = Banshee.RemoteAudio.addin.xml

if REMOTE_AUDIO_ENABLED
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
