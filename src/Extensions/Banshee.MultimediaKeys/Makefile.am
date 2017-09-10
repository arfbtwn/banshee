ASSEMBLY = Banshee.MultimediaKeys
TARGET = library
LINK = $(REF_EXTENSION_MULTIMEDIAKEYS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.MultimediaKeys/MultimediaKeysService.cs 

RESOURCES = Banshee.MultimediaKeys.addin.xml

if ENABLE_GNOME
if ENABLE_MULTIMEDIAKEYS
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
else
include $(top_srcdir)/build/build.dist.mk
endif
