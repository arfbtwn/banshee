ASSEMBLY = Banshee.MiniMode
TARGET = library
LINK = $(REF_EXTENSION_MINIMODE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.MiniMode/MiniModeService.cs \
	Banshee.MiniMode/MiniModeWindow.cs \
	Banshee.MiniMode/SourceModel.cs

RESOURCES = Banshee.MiniMode.addin.xml

if ENABLE_MINIMODE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

