ASSEMBLY = Banshee.Bpm
TARGET = library
LINK = $(REF_EXTENSION_BPM)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Bpm/BpmDetectJob.cs \
	Banshee.Bpm/BpmEntry.cs \
	Banshee.Bpm/BpmService.cs \
	Banshee.Bpm/BpmTapAdapter.cs \
	Banshee.Bpm/BpmTrackEditorModifier.cs

RESOURCES = Banshee.Bpm.addin.xml

if ENABLE_BPM
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

