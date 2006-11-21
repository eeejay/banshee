include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = BooBuddy
ASSEMBLY = $(ASSEMBLY_NAME).dll

BOO_MACRO_ASSEMBLY = BooMacros.dll

boobuddydir = $(pkglibdir)
boobuddy_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
    $(srcdir)/BooBuddy.Debugger/DebugAliasAttribute.cs \
    $(srcdir)/BooBuddy.Debugger/DebugAliasBuilder.cs \
	$(srcdir)/BooBuddy.Debugger/Reflection.cs \
	$(srcdir)/BooBuddyInterpreter.cs \
	$(srcdir)/BooBuddyWindow.cs \
	$(srcdir)/BooBuddyShell.cs

BOO_MACRO_SRC = \
	$(srcdir)/AliasMacro.boo

BOO_MACRO_SRC_RESOURCES = $(foreach resource,$(BOO_MACRO_SRC), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(BOO_MACRO_SRC)
	$(BUILD_LIB) -out:$@ $(LINK_GTK) $(LINK_BOO) $(BOO_MACRO_SRC_RESOURCES) $(ASSEMBLY_SOURCES)

# This is a build-time only check to ensure that the macros at least compile.
# The resulting assembly will not be used; the macros are compiled at runtime
# inside the interpreter.

if HAVE_BOOC
all: $(ASSEMBLY) $(BOO_MACRO_ASSEMBLY) 

$(BOO_MACRO_ASSEMBLY): $(BOO_MACRO_SRC)
	$(BOOC) -t:library -o:$@ -debug- -nologo $(BOO_MACRO_SRC)
endif

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(BOO_MACRO_ASSEMBLY)
MAINTAINERCLEANFILES = Makefile.in

