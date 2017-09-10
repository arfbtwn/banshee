ASSEMBLY = Banshee.InternetArchive
TARGET = library
LINK = $(REF_EXTENSION_INTERNETARCHIVE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.InternetArchive/Actions.cs \
	Banshee.InternetArchive/DetailsSource.cs \
	Banshee.InternetArchive/DetailsView.cs \
	Banshee.InternetArchive/HeaderFilters.cs \
	Banshee.InternetArchive/HomeSource.cs \
	Banshee.InternetArchive/HomeView.cs \
	Banshee.InternetArchive/Item.cs \
	Banshee.InternetArchive/SearchDescription.cs \
	Banshee.InternetArchive/SearchSource.cs \
	Banshee.InternetArchive/SearchView.cs \
	InternetArchive/Collection.cs \
	InternetArchive/Details.cs \
	InternetArchive/DetailsFile.cs \
	InternetArchive/DetailsReview.cs \
	InternetArchive/Field.cs \
	InternetArchive/FieldValue.cs \
	InternetArchive/JsonExtensions.cs \
	InternetArchive/MediaType.cs \
	InternetArchive/Search.cs \
	InternetArchive/SearchResult.cs \
	InternetArchive/SearchResults.cs \
	InternetArchive/Sort.cs

RESOURCES =  \
	Banshee.InternetArchive.addin.xml \
	Resources/DetailsSourceActiveUI.xml \
	Resources/GlobalUI.xml \
	Resources/HomeSourceActiveUI.xml \
	Resources/SearchSourceActiveUI.xml

if ENABLE_INTERNETARCHIVE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
