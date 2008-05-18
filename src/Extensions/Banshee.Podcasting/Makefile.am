ASSEMBLY = Banshee.Podcasting
TARGET = library
LINK = $(REF_EXTENSION_PODCASTING)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Podcasting.Data/PodcastTrackInfo.cs \
	Banshee.Podcasting.Gui/ColumnCellPodcast.cs \
	Banshee.Podcasting.Gui/ColumnCellPublished.cs \
	Banshee.Podcasting.Gui/DownloadManager/DownloadManagerInterface.cs \
	Banshee.Podcasting.Gui/DownloadManager/DownloadUserJob.cs \
	Banshee.Podcasting.Gui/Models/FilterableListModel.cs \
	Banshee.Podcasting.Gui/Models/ListModel.cs \
	Banshee.Podcasting.Gui/PodcastActions.cs \
	Banshee.Podcasting.Gui/PodcastManager/Dialog/PodcastFeedPropertiesDialog.cs \
	Banshee.Podcasting.Gui/PodcastManager/Dialog/PodcastPropertiesDialog.cs \
	Banshee.Podcasting.Gui/PodcastManager/Dialog/PodcastSubscribeDialog.cs \
	Banshee.Podcasting.Gui/PodcastManager/Dialog/SyncPreferenceComboBox.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/ColumnCells/FeedActivityColumnCell.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/ColumnCells/ItemActivityColumnCell.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/ColumnCells/PixbufColumnCell.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/Models/PodcastFeedModel.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/Models/PodcastItemModel.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/PodcastSource.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/PodcastSourceContents.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/Views/PodcastFeedView.cs \
	Banshee.Podcasting.Gui/PodcastManager/Source/Views/PodcastItemView.cs \
	Banshee.Podcasting/PodcastImageFetchJob.cs \
	Banshee.Podcasting/PodcastImportManager.cs \
	Banshee.Podcasting/PodcastService.cs \
	Banshee.Podcasting/PodcastService_Interface.cs

RESOURCES =  \
	Banshee.Podcasting.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml \
	Resources/Images/podcast-icon-16.png \
	Resources/Images/podcast-icon-22.png \
	Resources/Images/podcast-icon-48.png \
	Resources/Images/podcast-new-16.png

if ENABLE_PODCAST
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

