if ENABLE_MTP
ASSEMBLY = Mtp
TARGET = library
LINK = $(REF_MTP)

#EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in

SOURCES = \
	Mtp/FileType.cs \
	Mtp/ErrorCode.cs \
	Mtp/Error.cs \
	Mtp/FileSampleData.cs \
	Mtp/Album.cs \
	Mtp/Folder.cs \
	Mtp/MtpDevice.cs \
	Mtp/Track.cs

include $(top_srcdir)/build/build.mk
endif
