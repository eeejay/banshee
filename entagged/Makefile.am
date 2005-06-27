entaggeddir = $(pkglibdir)
entagged_SCRIPTS = entagged.dll

CLEANFILES = entagged.dll

entagged_sources = \
ape/ApeTag.cs \
ape/MonkeyFileReader.cs \
ape/util/ApeTagBinaryField.cs \
ape/util/ApeTagField.cs \
ape/util/ApeTagReader.cs \
ape/util/ApeTagTextField.cs \
ape/util/MonkeyDescriptor.cs \
ape/util/MonkeyHeader.cs \
ape/util/MonkeyInfoReader.cs \
ape/util/WavFormatHeader.cs \
ape/util/WavRIFFHeader.cs \
AudioFile.cs \
AudioFileIO.cs \
EncodingInfo.cs \
exceptions/CannotReadException.cs \
flac/FlacFileReader.cs \
flac/util/FlacInfoReader.cs \
flac/util/FlacTagReader.cs \
flac/util/MetadataBlockDataStreamInfo.cs \
flac/util/MetadataBlockHeader.cs \
generic/AbstractTag.cs \
generic/AudioFileReader.cs \
generic/ByteBuffer.cs \
generic/GenericTag.cs \
generic/TagField.cs \
generic/TagTextField.cs \
generic/Utils.cs \
mp3/Id3v1Tag.cs \
mp3/Id3v2Tag.cs \
mp3/Mp3FileReader.cs \
mp3/util/id3frames/CommId3Frame.cs \
mp3/util/id3frames/GenericId3Frame.cs \
mp3/util/id3frames/Id3Frame.cs \
mp3/util/id3frames/TextId3Frame.cs \
mp3/util/id3frames/ApicId3Frame.cs \
mp3/util/Id3v1TagReader.cs \
mp3/util/Id3v23TagReader.cs \
mp3/util/Id3v2TagReader.cs \
mp3/util/Id3v2TagSynchronizer.cs \
mp3/util/LameMPEGFrame.cs \
mp3/util/Mp3InfoReader.cs \
mp3/util/MPEGFrame.cs \
mp3/util/XingMPEGFrame.cs \
mpc/MpcFileReader.cs \
mpc/util/MpcHeader.cs \
mpc/util/MpcInfoReader.cs \
ogg/OggFileReader.cs \
ogg/OggTag.cs \
ogg/util/OggInfoReader.cs \
ogg/util/OggPageHeader.cs \
ogg/util/OggTagField.cs \
ogg/util/OggTagReader.cs \
ogg/util/VorbisCodecHeader.cs \
ogg/util/VorbisTagReader.cs \
Tag.cs

SYSTEM_REFERENCES = \
	System.dll

SYSTEM_REFERENCES_BUILD = $(addprefix -r:, $(SYSTEM_REFERENCES))

entagged_build_sources = $(addprefix $(srcdir)/, $(entagged_sources))

all: entagged.dll

entagged.dll: $(entagged_build_sources)
	$(MCS) $(PKG_REFERENCES) $(entagged_resources_build) \
	-target:library -out:"$@" \
	$(SYSTEM_REFERENCES_BUILD) \
	$(entagged_build_sources)
