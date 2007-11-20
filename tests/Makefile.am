include $(top_srcdir)/build/build.environment.mk

MCS_FLAGS = -debug
NUNIT_FLAGS = @MONO_NUNIT_LIBS@

ASSEMBLY_NAME = banshee-tests
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_CSFILES = \
	$(srcdir)/PlaylistFormats.cs \
	$(srcdir)/Banshee.Kernel.cs \
	$(srcdir)/FileNamePattern.cs \
	$(srcdir)/Xspf/Xspf.cs

NUNIT_TESTER_NAME = ConsoleUi
NUNIT_TESTER = $(NUNIT_TESTER_NAME).exe
NUNIT_TESTER_CSFILES = $(srcdir)/$(NUNIT_TESTER_NAME).cs $(srcdir)/AssemblyInfo.cs

$(ASSEMBLY): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(NUNIT_FLAGS) -out:$@ -target:library -r:System.Xml $(LINK_BANSHEE_SERVICES_DEPS) $(ASSEMBLY_CSFILES)

$(NUNIT_TESTER): $(NUNIT_TESTER_CSFILES)
	$(MCS) $(MCS_FLAGS) -out:$@ $(NUNIT_FLAGS) $(NUNIT_TESTER_CSFILES)

if ENABLE_TESTS
run-test: $(NUNIT_TESTER) $(ASSEMBLY)
	MONO_PATH="$(RUN_PATH)" mono --debug $(NUNIT_TESTER) $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(NUNIT_TESTER) TestResult.xml
DISTCLEANFILES = *.mdb *.dll *.exe
MAINTAINERCLEANFILES = Makefile.in

