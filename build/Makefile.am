SUBDIRS = pkg-config m4

TRANSLATOR_EXTRACTOR_ASSEMBLY = translator-extractor.exe
ADDIN_XML_STRING_EXTRACTOR_ASSEMBLY = addin-xml-string-extractor.exe
GOBJECT_INTPTR_CTOR_VERIFIER_ASSEMBLY = gobject-intptr-ctor-verifier.exe

ALL_TARGETS = \
	$(TRANSLATOR_EXTRACTOR_ASSEMBLY) \
	$(DLL_MAP_VERIFIER_ASSEMBLY) \
	$(ADDIN_XML_STRING_EXTRACTOR_ASSEMBLY) \
	$(GOBJECT_INTPTR_CTOR_VERIFIER_ASSEMBLY)

$(TRANSLATOR_EXTRACTOR_ASSEMBLY): TranslatorExtractor.cs
	if [ "x$(top_srcdir)" = "x$(top_builddir)" ]; then \
		$(MCS) -out:$@ $< && LC_ALL=en_US.UTF-8 $(MONO) $@ $(top_builddir)/po > \
			$(top_srcdir)/src/Core/Banshee.Core/Resources/translators.xml; \
	fi;

$(ADDIN_XML_STRING_EXTRACTOR_ASSEMBLY): AddinXmlStringExtractor.cs
	$(MCS) -r:System.Xml -out:$@ $<

$(GOBJECT_INTPTR_CTOR_VERIFIER_ASSEMBLY): GObjectIntPtrCtorVerifier.cs
	$(MCS) $(GLIBSHARP_LIBS) -out:$@ $<

EXTRA_DIST = \
	icon-theme-installer \
	private-icon-theme-installer \
	TranslatorExtractor.cs \
	DllMapVerifier.cs \
	AddinXmlStringExtractor.cs \
	GObjectIntPtrCtorVerifier.cs \
	dll-map-makefile-verifier \
	gnome-doc-utils.make

CLEANFILES = *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in

include $(top_srcdir)/build/build.rules.mk
