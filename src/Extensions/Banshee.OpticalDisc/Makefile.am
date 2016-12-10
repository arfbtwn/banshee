ASSEMBLY = Banshee.OpticalDisc
TARGET = library
LINK = $(REF_EXTENSION_OPTICALDISC)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.OpticalDisc.AudioCd/AudioCdDiscModel.cs \
	Banshee.OpticalDisc.AudioCd/AudioCdDuplicator.cs \
	Banshee.OpticalDisc.AudioCd/AudioCdRipper.cs \
	Banshee.OpticalDisc.AudioCd/AudioCdService.cs \
	Banshee.OpticalDisc.AudioCd/AudioCdSource.cs \
	Banshee.OpticalDisc.AudioCd/AudioCdTrackInfo.cs \
	Banshee.OpticalDisc.Dvd/DvdModel.cs \
	Banshee.OpticalDisc.Dvd/DvdService.cs \
	Banshee.OpticalDisc.Dvd/DvdSource.cs \
	Banshee.OpticalDisc.Dvd/DvdTrackInfo.cs \
	Banshee.OpticalDisc/DiscModel.cs \
	Banshee.OpticalDisc/DiscService.cs \
	Banshee.OpticalDisc/DiscSource.cs \
	Banshee.OpticalDisc/DiscTrackInfo.cs

RESOURCES =  \
	Banshee.OpticalDisc.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml \
	Resources/GlobalUI_Dvd.xml

if ENABLE_OPTICALDISC
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

