ASSEMBLY = Banshee.LibraryWatcher
TARGET = library
LINK = $(REF_EXTENSION_LIBRARYWATCHER)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.LibraryWatcher/LibraryWatcherService.cs \
	Banshee.LibraryWatcher/SourceWatcher.cs

RESOURCES = Banshee.LibraryWatcher.addin.xml

if ENABLE_LIBRARYWATCHER
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
