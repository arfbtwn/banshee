ASSEMBLY = Banshee.InternetRadio
TARGET = library
LINK = $(REF_EXTENSION_INTERNETRADIO)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.InternetRadio/InternetRadioSource.cs \
	Banshee.InternetRadio/InternetRadioSourceContents.cs \
	Banshee.InternetRadio/StationEditor.cs \
	Banshee.InternetRadio/XspfMigrator.cs 

RESOURCES =  \
	Banshee.InternetRadio.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml

if ENABLE_INTERNETRADIO
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

