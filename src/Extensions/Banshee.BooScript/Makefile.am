ASSEMBLY = Banshee.BooScript
TARGET = library
LINK = $(REF_EXTENSION_BOOSCRIPT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.BooScript/BooScriptService.cs

RESOURCES =  \
	Banshee.BooScript.addin.xml \
	Resources/duration-status-formats.boo \
	Resources/filename-transform.boo \
	Resources/vorbis-oga-extension.boo

if ENABLE_BOOSCRIPT
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
