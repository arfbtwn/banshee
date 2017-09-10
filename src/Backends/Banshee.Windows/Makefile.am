ASSEMBLY = Banshee.Windows
TARGET = library
LINK = $(REF_BACKEND_WINDOWS)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.Windows/ExtensionsForGtkAction.cs \
	Banshee.Windows/GtkWindowThumbnailToolbarManager.cs \
	Banshee.Windows/VersionUpdater.cs \
	Banshee.Windows/WindowsService.cs \
	Banshee.Windows.Hardware/Device.cs \
	Banshee.Windows.Hardware/HardwareManager.cs \
	Banshee.Windows.Hardware/UsbDevice.cs \
	Banshee.Windows.Hardware/Volume.cs \
	Windows7Support/TaskbarInterop.cs \
	Windows7Support/ThumbnailToolbarButton.cs \
	Windows7Support/ThumbnailToolbar.cs \
	Windows7Support/ThumbnailToolbarManager.cs

RESOURCES = Banshee.Windows.addin.xml

if PLATFORM_WINDOWS
include $(top_srcdir)/build/build.rules.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

