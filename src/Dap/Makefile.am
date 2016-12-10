if ENABLE_DAP
SUBDIRS = \
	Banshee.Dap \
	Banshee.Dap.AppleDevice \
	Banshee.Dap.MassStorage \
	Banshee.Dap.Mtp \
	Banshee.Dap.Karma
endif

run:
	@pushd $(top_builddir); \
	make run; \
	popd;

MAINTAINERCLEANFILES = Makefile.in
