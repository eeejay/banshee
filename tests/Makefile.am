include $(top_srcdir)/build/build.environment.mk

MCS_FLAGS = -debug
NUNIT_FLAGS = @MONO_NUNIT_LIBS@

ASSEMBLY_NAME = banshee-tests
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_CSFILES =  \
	$(srcdir)/BansheeTests.cs \
	$(srcdir)/Hyena/CryptoUtilTests.cs \
	$(srcdir)/Hyena/DbBoundType.cs \
	$(srcdir)/Hyena/RangeCollectionTests.cs \
	$(srcdir)/Hyena/StringUtilTests.cs \
	$(srcdir)/Hyena/SqliteCommandTests.cs \
	$(srcdir)/Hyena/SqliteModelProviderTests.cs \
	$(srcdir)/Hyena/QueryTests.cs \
	$(srcdir)/Banshee.Services/SmartPlaylistTests.cs \
	$(srcdir)/Banshee.ThickClient/GuiTests.cs \
	$(srcdir)/Banshee.Core/TaglibReadWriteTests.cs

#	Banshee.Core/KernelTests.cs
#	Banshee.Core/FileNamePatternTests.cs
#	Banshee.Services/PlaylistFormatTests.cs
#	Banshee.Services/Xspf/Xspf.cs

NUNIT_TESTER_NAME = ConsoleUi
NUNIT_TESTER = $(NUNIT_TESTER_NAME).exe
NUNIT_TESTER_CSFILES =  \
	AssemblyInfo.cs \
	ConsoleUi.cs

$(ASSEMBLY): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(NUNIT_FLAGS) -out:$@ -target:library -r:System.Xml $(LINK_HYENA_DEPS) $(LINK_BANSHEE_CORE_DEPS) $(LINK_BANSHEE_THICKCLIENT_DEPS) $(LINK_NEREID_DEPS) $(ASSEMBLY_CSFILES) $(REF_BACKEND_UNIX) -r:$(DIR_BIN)/Banshee.Unix.dll

$(NUNIT_TESTER): $(NUNIT_TESTER_CSFILES)
	$(MCS) $(MCS_FLAGS) -out:$@ $(NUNIT_FLAGS) $(NUNIT_TESTER_CSFILES)

if ENABLE_TESTS
all: $(ASSEMBLY)

test: $(NUNIT_TESTER) $(ASSEMBLY)
	LANG=it_IT MONO_PATH="$(top_builddir)/bin" mono --debug $(NUNIT_TESTER) $(ASSEMBLY)
endif

EXTRA_DIST = $(ASSEMBLY_CSFILES)

CLEANFILES = $(ASSEMBLY) $(NUNIT_TESTER) TestResult.xml
DISTCLEANFILES = *.mdb *.dll *.exe
MAINTAINERCLEANFILES = Makefile.in

