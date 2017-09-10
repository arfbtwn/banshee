ASSEMBLY = Banshee.SqlDebugConsole
TARGET = library
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.SqlDebugConsole/SqlActions.cs

RESOURCES =  \
	Banshee.SqlDebugConsole.addin.xml \
	Resources/GlobalUI.xml

if ENABLE_SQLDEBUGCONSOLE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
