SOURCES = $(wildcard ../*.cs *.cs)
ASSEMBLY = eval.exe

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	gmcs -out:$@ $(SOURCES)

run: all
	mono $(ASSEMBLY) list.sxp

clean:
	rm -f $(ASSEMBLY)
