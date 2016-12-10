ASSEMBLY = Banshee.Emusic
TARGET = library
LINK = $(REF_EXTENSION_EMUSIC)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Emusic/DownloadManager/DownloadManagerInterface.cs \
	Banshee.Emusic/DownloadManager/DownloadUserJob.cs \
	Banshee.Emusic/EmusicImport.cs \
	Banshee.Emusic/EmusicService.cs

RESOURCES = Banshee.Emusic.addin.xml

mimedir = $(datadir)/mime/packages
mime_DATA = banshee-emx.xml

if ENABLE_EMUSIC
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

EXTRA_DIST += banshee-emx.xml

