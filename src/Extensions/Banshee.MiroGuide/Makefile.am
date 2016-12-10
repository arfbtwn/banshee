ASSEMBLY = Banshee.MiroGuide
TARGET = library
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS) $(LINK_BANSHEE_WEBBROWSER_DEPS) 

SOURCES =  \
	Banshee.MiroGuide/MiroGuideSource.cs \
	Banshee.MiroGuide/View.cs

RESOURCES = Banshee.MiroGuide.addin.xml

if HAVE_LIBWEBKIT
if ENABLE_MIROGUIDE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
else
include $(top_srcdir)/build/build.dist.mk
endif
