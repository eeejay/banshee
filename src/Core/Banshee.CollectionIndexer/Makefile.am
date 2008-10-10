ASSEMBLY = Banshee.CollectionIndexer
#ASSEMBLY_BUILD_FLAGS = -main:RemoteClient.RemoteClientExample
TARGET = library
LINK = $(REF_BANSHEE_COLLECTIONINDEXER)
SOURCES =  \
	Banshee.Collection.Indexer.RemoteHelper/IndexerClient.cs \
	Banshee.Collection.Indexer.RemoteHelper/SimpleIndexerClient.cs \
	Banshee.Collection.Indexer/ICollectionIndexer.cs \
	Banshee.Collection.Indexer/ICollectionIndexerService.cs \
	Hyena/ConsoleCrayon.cs \
	Hyena/DateTimeUtil.cs \
	Hyena/Log.cs 

include $(top_srcdir)/build/build.mk

#ASSEMBLY_FILE_TMP=$(ASSEMBLY_FILE)
#ASSEMBLY_FILE=$(strip $(patsubst .exe, .dll, $(ASSEMBLY_FILE_TMP)))
