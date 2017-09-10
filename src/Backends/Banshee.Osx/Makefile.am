ASSEMBLY = Banshee.Osx
ASSEMBLY_BUILD_FLAGS = -unsafe
TARGET = library
LINK = $(REF_BACKEND_OSX)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.Hardware.Osx/CdromDevice.cs \
	Banshee.Hardware.Osx/Device.cs \
	Banshee.Hardware.Osx/DiscVolume.cs \
	Banshee.Hardware.Osx/LowLevel/CoreFoundation.cs \
	Banshee.Hardware.Osx/LowLevel/DiskArbitration.cs \
	Banshee.Hardware.Osx/LowLevel/IOKit.cs \
	Banshee.Hardware.Osx/LowLevel/OsxDiskArbiter.cs \
	Banshee.Hardware.Osx/LowLevel/OsxUsbData.cs \
	Banshee.Hardware.Osx/UsbDevice.cs \
	Banshee.Hardware.Osx/UsbVolume.cs \
	Banshee.Hardware.Osx/Volume.cs \
	Banshee.OsxBackend/HardwareManager.cs \
	Banshee.OsxBackend/OsxFileChooserDialog.cs \
	Banshee.OsxBackend/OsxService.cs \
	OsxIntegration.GtkOsxApplication/GtkOsxApplication.cs

RESOURCES =  \
	Banshee.Osx.addin.xml \
	Resources/osx_accel_map

EXTRA_BUNDLE = $(MONOMAC_ASSEMBLIES)

if PLATFORM_DARWIN
include $(top_srcdir)/build/build.mk

define EXTRA_INSTALL_DATA_HOOK
	for ASM in $(MONOMAC_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;
endef

define EXTRA_UNINSTALL_HOOK
	for ASM in $(MONOMAC_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;
endef

else
include $(top_srcdir)/build/build.dist.mk
MAINTAINERCLEANFILES += Makefile.in
endif

