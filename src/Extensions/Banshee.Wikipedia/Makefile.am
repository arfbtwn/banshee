ASSEMBLY = Banshee.Wikipedia
TARGET = library
LINK = $(REF_EXTENSION_WIKIPEDIA)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Wikipedia/ContextPage.cs \
	Banshee.Wikipedia/WikipediaView.cs

RESOURCES = Banshee.Wikipedia.addin.xml

if ENABLE_WIKIPEDIA
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
