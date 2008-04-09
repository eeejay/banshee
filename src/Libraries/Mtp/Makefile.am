ASSEMBLY = Mtp
TARGET = library
LINK = $(REF_MTP)

SOURCES = \
	Mtp/FileType.cs \
	Mtp/ErrorCode.cs \
	Mtp/Error.cs \
	Mtp/FileSampleData.cs \
	Mtp/Album.cs \
	Mtp/Folder.cs \
	Mtp/MtpDevice.cs \
	Mtp/Track.cs

if ENABLE_MTP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) *.config.in
endif

