ASSEMBLY = RemoteClient.exe
SOURCES = RemoteClient.cs

INDEXER_LIB = Banshee.CollectionIndexer.dll
INDEXER_LIB_SRC = ../../../../bin/$(INDEXER_LIB)

$(ASSEMBLY): $(INDEXER_LIB) $(SOURCES)
	gmcs -debug -out:$@ -r:$< -pkg:ndesk-dbus-1.0  $(SOURCES)

$(INDEXER_LIB): $(INDEXER_LIB_SRC)
	cp -a $< $@

run: $(ASSEMBLY)
	mono --debug $< --debug

clean:
	rm -f $(INDEXER_LIB) $(ASSEMBLY) *.mdb

