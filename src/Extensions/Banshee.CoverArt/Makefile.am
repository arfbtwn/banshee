ASSEMBLY = Banshee.CoverArt
TARGET = library
LINK = $(REF_EXTENSION_COVERART)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.CoverArt/CoverArtJob.cs \
	Banshee.CoverArt/CoverArtService.cs

RESOURCES = Banshee.CoverArt.addin.xml

if ENABLE_COVERART
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
