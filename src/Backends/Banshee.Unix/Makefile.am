ASSEMBLY = Banshee.Unix
TARGET = library
LINK = $(REF_BACKEND_UNIX)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.IO.Unix/DemuxVfs.cs \
	Banshee.IO.Unix/Directory.cs \
	Banshee.IO.Unix/File.cs \
	Banshee.IO.Unix/Provider.cs

RESOURCES = Banshee.Unix.addin.xml

if PLATFORM_UNIX
include $(top_srcdir)/build/build.mk
EXTRA_DIST += Banshee.Unix.dll.config
module_SCRIPTS += Banshee.Unix.dll.config
else
include $(top_srcdir)/build/build.dist.mk
endif

