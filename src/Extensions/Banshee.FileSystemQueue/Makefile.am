ASSEMBLY = Banshee.FileSystemQueue
TARGET = library
LINK = $(REF_EXTENSION_FILESYSTEMQUEUE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.FileSystemQueue/FileSystemQueueSource.cs 

RESOURCES =  \
	Banshee.FileSystemQueue.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml

if ENABLE_FILESYSTEMQUEUE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

