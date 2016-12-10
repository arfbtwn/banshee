ASSEMBLY = Banshee.Dap.MassStorage
TARGET = library
LINK = $(REF_DAP_MASS_STORAGE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Dap.MassStorage/AmazonMp3GroupSource.cs \
	Banshee.Dap.MassStorage/AndroidDevice.cs \
	Banshee.Dap.MassStorage/CustomMassStorageDevice.cs \
	Banshee.Dap.MassStorage/DeviceMapper.cs \
	Banshee.Dap.MassStorage/KeyValueParser.cs \
	Banshee.Dap.MassStorage/MaemoDevice.cs \
	Banshee.Dap.MassStorage/MassStorageDevice.cs \
	Banshee.Dap.MassStorage/MassStorageSource.cs \
	Banshee.Dap.MassStorage/NookDevice.cs \
	Banshee.Dap.MassStorage/RockBoxDevice.cs \
	Banshee.Dap.MassStorage/SymbianDevice.cs \
	Banshee.Dap.MassStorage/WebOSDevice.cs

RESOURCES = Banshee.Dap.MassStorage.addin.xml

if ENABLE_DAP_MASS_STORAGE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

