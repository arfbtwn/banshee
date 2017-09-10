ASSEMBLY = Banshee.Sample
TARGET = library
LINK = $(REF_EXTENSION_SAMPLE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Sample/SampleSource.cs \
	Banshee.Sample/SampleSourceInterface.cs

RESOURCES = Banshee.Sample.addin.xml

if ENABLE_SAMPLE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

