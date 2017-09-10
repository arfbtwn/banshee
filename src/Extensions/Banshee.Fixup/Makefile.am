ASSEMBLY = Banshee.Fixup
TARGET = library
LINK = $(REF_EXTENSION_FIXUP)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Fixup/AlbumDuplicateSolver.cs \
	Banshee.Fixup/ArtistDuplicateSolver.cs \
	Banshee.Fixup/ColumnCellSolutionOptions.cs \
	Banshee.Fixup/FixActions.cs \
	Banshee.Fixup/FixSource.cs \
	Banshee.Fixup/GenreDuplicateSolver.cs \
	Banshee.Fixup/Problem.cs \
	Banshee.Fixup/ProblemModel.cs \
	Banshee.Fixup/Solver.cs \
	Banshee.Fixup/Tests.cs \
	Banshee.Fixup/View.cs

RESOURCES =  \
	Banshee.Fixup.addin.xml \
	Resources/ActiveUI.xml \
	Resources/GlobalUI.xml

if ENABLE_FIXUP
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

