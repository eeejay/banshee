ASSEMBLY = BansheeDbusClient.exe
ASSEMBLY_SOURCES = BansheeDbusClient.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	mcs -pkg:gtk-sharp-2.0 -pkg:dbus-sharp -out:$@ $(ASSEMBLY_SOURCES)

clean:
	-rm $(ASSEMBLY)

run:
	mono $(ASSEMBLY)


