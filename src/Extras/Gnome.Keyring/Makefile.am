include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Gnome.Keyring
ASSEMBLY = $(ASSEMBLY_NAME).dll

gnome_keyringdir= $(pkglibdir)
gnome_keyring_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES =  \
	AccessRights.cs \
	AttributeType.cs \
	GenericItemData.cs \
	ItemACL.cs \
	ItemData.cs \
	ItemType.cs \
	KeyringException.cs \
	KeyringInfo.cs \
	NetItemData.cs \
	NoteItemData.cs \
	Operation.cs \
	RequestMessage.cs \
	ResponseMessage.cs \
	ResultCode.cs \
	Ring.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB) -out:$@ $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

