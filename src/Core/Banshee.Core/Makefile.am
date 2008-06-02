ASSEMBLY = Banshee.Core
TARGET = library
LINK = $(REF_BANSHEE_CORE)
SOURCES =  \
	Banshee.Base/ApplicationContext.cs \
	Banshee.Base/CoverArtSpec.cs \
	Banshee.Base/FileNamePattern.cs \
	Banshee.Base/Localization.cs \
	Banshee.Base/NamingUtil.cs \
	Banshee.Base/Paths.cs \
	Banshee.Base/PlatformHacks.cs \
	Banshee.Base/ProductInformation.cs \
	Banshee.Base/Resource.cs \
	Banshee.Base/SafeUri.cs \
	Banshee.Base/Tests/FileNamePatternTests.cs \
	Banshee.Base/Tests/TaglibReadWriteTests.cs \
	Banshee.Base/UriList.cs \
	Banshee.Base/XdgBaseDirectorySpec.cs \
	Banshee.Collection/AlbumInfo.cs \
	Banshee.Collection/ArtistInfo.cs \
	Banshee.Collection/CacheableItem.cs \
	Banshee.Collection/FileTrackInfo.cs \
	Banshee.Collection/IBasicTrackInfo.cs \
	Banshee.Collection/ITrackInfo.cs \
	Banshee.Collection/SampleTrackInfo.cs \
	Banshee.Collection/TrackAttributes.cs \
	Banshee.Collection/TrackFilterType.cs \
	Banshee.Collection/TrackInfo.cs \
	Banshee.Collection/TrackMediaAttributes.cs \
	Banshee.Collection/UnknownTrackInfo.cs \
	Banshee.Configuration.Schema/ImportSchema.cs \
	Banshee.Configuration.Schema/LibrarySchema.cs \
	Banshee.Configuration/ConfigurationClient.cs \
	Banshee.Configuration/IConfigurationClient.cs \
	Banshee.Configuration/SchemaEntry.cs \
	Banshee.Configuration/XmlConfigurationClient.cs \
	Banshee.I18n/AssemblyCatalogAttribute.cs \
	Banshee.I18n/Catalog.cs \
	Banshee.IO.SystemIO/DemuxVfs.cs \
	Banshee.IO.SystemIO/Directory.cs \
	Banshee.IO.SystemIO/File.cs \
	Banshee.IO.SystemIO/Provider.cs \
	Banshee.IO/DemuxVfs.cs \
	Banshee.IO/Directory.cs \
	Banshee.IO/File.cs \
	Banshee.IO/IDemuxVfs.cs \
	Banshee.IO/IDirectory.cs \
	Banshee.IO/IFile.cs \
	Banshee.IO/IProvider.cs \
	Banshee.IO/Provider.cs \
	Banshee.IO/StreamAssist.cs \
	Banshee.IO/Utilities.cs \
	Banshee.Kernel/DelegateJob.cs \
	Banshee.Kernel/IInstanceCriticalJob.cs \
	Banshee.Kernel/IJob.cs \
	Banshee.Kernel/Job.cs \
	Banshee.Kernel/JobPriority.cs \
	Banshee.Kernel/Scheduler.cs \
	Banshee.Streaming/CommonTags.cs \
	Banshee.Streaming/SaveTrackMetadataJob.cs \
	Banshee.Streaming/StreamPlaybackError.cs \
	Banshee.Streaming/StreamTag.cs \
	Banshee.Streaming/StreamTagger.cs

RESOURCES =  \
	Banshee.Core.addin.xml \
	Resources/contributors.xml \
	Resources/COPYING \
	Resources/translators.xml

include $(top_srcdir)/build/build.mk

module_SCRIPTS += Banshee.Core.dll.config
EXTRA_DIST += Banshee.Core.dll.config

