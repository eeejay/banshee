ASSEMBLY = Mtp
TARGET = library
LINK = $(REF_MTP)

if LIBMTP_EIGHT
BUILD_DEFINES = "-define:LIBMTP8"
else
BUILD_DEFINES = ""
endif

if LIBMTP_TRACK_STRUCT_HAS_MODDATE
BUILD_DEFINES += "-define:LIBMTP_TRACK_HAS_MODDATE"
endif

SOURCES =  \
	Mtp/AbstractTrackList.cs \
	Mtp/Album.cs \
	Mtp/Error.cs \
	Mtp/ErrorCode.cs \
	Mtp/FileSampleData.cs \
	Mtp/FileType.cs \
	Mtp/Folder.cs \
	Mtp/MtpDevice.cs \
	Mtp/Playlist.cs \
	Mtp/Track.cs

if ENABLE_MTP
include $(top_srcdir)/build/build.mk
module_SCRIPTS += Mtp.dll.config
EXTRA_DIST += Mtp.dll.config.in
DISTCLEANFILES += Mtp.dll.config
else
EXTRA_DIST = $(SOURCES) Mtp.dll.config.in
DISTCLEANFILES = Mtp.dll.config
endif


