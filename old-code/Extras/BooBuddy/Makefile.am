ASSEMBLY = BooBuddy
TARGET = library
LINK = $(REF_BOOBUDDY)
SOURCES =  \
	BooBuddy.Debugger/DebugAliasAttribute.cs \
	BooBuddy.Debugger/DebugAliasBuilder.cs \
	BooBuddy.Debugger/Reflection.cs \
	BooBuddy/BooBuddyInterpreter.cs \
	BooBuddy/BooBuddyShell.cs \
	BooBuddy/BooBuddyWindow.cs

RESOURCES = \
	BooBuddy/AliasMacro.boo

include $(top_srcdir)/build/build.mk

# This is a build-time only check to ensure that the macros at least compile.
# The resulting assembly will not be used; the macros are compiled at runtime
# inside the interpreter.

if HAVE_BOOC
BOO_MACRO_ASSEMBLY = BooMacros.dll

all: $(BOO_MACRO_ASSEMBLY) 

$(BOO_MACRO_ASSEMBLY): $(RESOURCES_EXPANDED_FULL)
	$(BOOC) -t:library -o:$@ -debug- -nologo $(RESOURCES_EXPANDED_FULL)
endif

