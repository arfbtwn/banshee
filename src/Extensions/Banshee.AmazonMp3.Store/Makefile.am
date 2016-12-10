ASSEMBLY = Banshee.AmazonMp3.Store
TARGET = library
LINK = $(REF_EXTENSION_AMAZONMP3_STORE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.AmazonMp3.Store/StoreSource.cs \
	Banshee.AmazonMp3.Store/StoreSourcePreferences.cs \
	Banshee.AmazonMp3.Store/StoreView.cs \
	Banshee.AmazonMp3.Store/StoreWebBrowserShell.cs

RESOURCES = Banshee.AmazonMp3.Store.addin.xml

if HAVE_LIBWEBKIT
if ENABLE_AMAZONMP3_STORE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
else
include $(top_srcdir)/build/build.dist.mk
endif

EXTRA_DIST += ThemeIcons/amazon-mp3-store-source.svg
