ASSEMBLY = Banshee.Emusic.Store
TARGET = library
LINK = $(REF_EXTENSION_EMUSIC_STORE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Emusic.Store/StoreSource.cs \
	Banshee.Emusic.Store/StoreView.cs \
	Banshee.Emusic.Store/StoreWebBrowserShell.cs

RESOURCES = Banshee.Emusic.Store.addin.xml

if HAVE_LIBWEBKIT
if ENABLE_EMUSIC_STORE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
else
include $(top_srcdir)/build/build.dist.mk
endif
