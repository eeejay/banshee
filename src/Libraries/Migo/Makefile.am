ASSEMBLY = Migo
TARGET = library
LINK = $(REF_MIGO)
SOURCES =  \
	Migo/Migo.DownloadCore/DownloadGroupStatusChangedEventArgs.cs \
	Migo/Migo.DownloadCore/DownloadGroupStatusManager.cs \
	Migo/Migo.DownloadCore/DownloadManager.cs \
	Migo/Migo.DownloadCore/HttpDownloadTaskErrors.cs \
	Migo/Migo.DownloadCore/HttpFileDownloadGroup.cs \
	Migo/Migo.DownloadCore/HttpFileDownloadTask.cs \
	Migo/Migo.Net/AsyncWebClient.cs \
	Migo/Migo.Net/AsyncWebClientStatus.cs \
	Migo/Migo.Net/EventArgs/DownloadDataCompletedEventArgs.cs \
	Migo/Migo.Net/EventArgs/DownloadProgressChangedEventArgs.cs \
	Migo/Migo.Net/EventArgs/DownloadStringCompletedEventArgs.cs \
	Migo/Migo.Net/EventArgs/TransferRateUpdatedEventArgs.cs \
	Migo/Migo.Net/Exceptions/RemoteFileModifiedException.cs \
	Migo/Migo.Net/TransferStatusManager.cs \
	Migo/Migo.Syndication/EnclosureManager.cs \
	Migo/Migo.Syndication/Enumerations/FeedBackgroundSyncAction.cs \
	Migo/Migo.Syndication/Enumerations/FeedBackgroundSyncStatus.cs \
	Migo/Migo.Syndication/Enumerations/FeedDownloadError.cs \
	Migo/Migo.Syndication/Enumerations/FeedDownloadStatus.cs \
	Migo/Migo.Syndication/Enumerations/FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS.cs \
	Migo/Migo.Syndication/Enumerations/FEEDS_EVENTS_ITEM_COUNT_FLAGS.cs \
	Migo/Migo.Syndication/Enumerations/FeedSyncSetting.cs \
	Migo/Migo.Syndication/EventArgs/FeedDownloadCompletedEventArgs.cs \
	Migo/Migo.Syndication/EventArgs/FeedDownloadCountChangedEventArgs.cs \
	Migo/Migo.Syndication/EventArgs/FeedEventArgs.cs \
	Migo/Migo.Syndication/EventArgs/FeedItemCountChangedEventArgs.cs \
	Migo/Migo.Syndication/EventArgs/FeedItemEventArgs.cs \
	Migo/Migo.Syndication/Feed.cs \
	Migo/Migo.Syndication/FeedEnclosure.cs \
	Migo/Migo.Syndication/FeedItem.cs \
	Migo/Migo.Syndication/FeedManager.cs \
	Migo/Migo.Syndication/FeedsManager.cs \
	Migo/Migo.Syndication/FeedUpdateTask.cs \
	Migo/Migo.Syndication/MigoItem.cs \
	Migo/Migo.Syndication/MigoModelProvider.cs \
	Migo/Migo.Syndication/OpmlParser.cs \
	Migo/Migo.Syndication/Rfc822DateTime.cs \
	Migo/Migo.Syndication/RssParser.cs \
	Migo/Migo.Syndication/Tests/XmlTests.cs \
	Migo/Migo.Syndication/XmlUtils.cs \
	Migo/Migo.TaskCore/AsyncCommandQueue/AsyncCommandQueue.cs \
	Migo/Migo.TaskCore/AsyncCommandQueue/CommandDelegate.cs \
	Migo/Migo.TaskCore/AsyncCommandQueue/CommandWrapper.cs \
	Migo/Migo.TaskCore/AsyncCommandQueue/EventWrapper.cs \
	Migo/Migo.TaskCore/AsyncCommandQueue/ICommand.cs \
	Migo/Migo.TaskCore/AsyncTaskScheduler.cs \
	Migo/Migo.TaskCore/Enumerations/TaskPriority.cs \
	Migo/Migo.TaskCore/Enumerations/TaskStatus.cs \
	Migo/Migo.TaskCore/EventArgs/GroupStatusChangedEventArgs.cs \
	Migo/Migo.TaskCore/EventArgs/TaskCompletedEventArgs.cs \
	Migo/Migo.TaskCore/EventArgs/TaskEventArgs.cs \
	Migo/Migo.TaskCore/EventArgs/TaskStatusChangedEventArgs.cs \
	Migo/Migo.TaskCore/EventArgs/TaskStatusChangedInfo.cs \
	Migo/Migo.TaskCore/Exceptions/TaskStatusException.cs \
	Migo/Migo.TaskCore/GroupProgressManager.cs \
	Migo/Migo.TaskCore/GroupStatusManager.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/EventArgs/ManipulatedEventArgs.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/EventArgs/ReorderedEventArgs.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/EventArgs/TaskAddedEventArgs.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/EventArgs/TaskRemovedEventArgs.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/ITaskCollectionEnumerator.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/PriorityQueue.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/TaskCollection.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/TaskList.cs \
	Migo/Migo.TaskCore/Migo.TaskCore.Collections/TaskListEnumerator.cs \
	Migo/Migo.TaskCore/Task.cs \
	Migo/Migo.TaskCore/TaskGroup.cs

if ENABLE_PODCAST
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

