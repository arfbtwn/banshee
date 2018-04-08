//
// LibrarySource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Collections;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;

namespace Banshee.Library
{
    public abstract class LibrarySource : PrimarySource
    {
        // Deprecated, don't use in new code
        internal static readonly Banshee.Configuration.SchemaEntry<string> OldLocationSchema = new Banshee.Configuration.SchemaEntry<string> ("library", "base_location", null, null, null);
        internal static readonly Banshee.Configuration.SchemaEntry<bool> OldImportSetting = new Banshee.Configuration.SchemaEntry<bool> ("library", "copy_on_import", false, null, null);
        internal static readonly Banshee.Configuration.SchemaEntry<bool> OldRenameSetting = new Banshee.Configuration.SchemaEntry<bool> ("library", "move_on_info_save", false, null, null);

        private Banshee.Configuration.SchemaEntry<string> base_dir_schema;
        private Banshee.Configuration.SchemaEntry<bool> copy_on_import;
        private Banshee.Configuration.SchemaEntry<bool> move_files;

        public LibrarySource (string label, string name, int order) : base (label, label, name, order)
        {
            Properties.SetString ("GtkActionPath", "/LibraryContextMenu");
            Properties.SetString ("RemoveSelectedTracksActionLabel", Catalog.GetString ("Remove From Library"));
            Properties.SetString ("RemovePlayingTrackActionLabel", Catalog.GetString ("Remove From Library"));
            IsLocal = true;
            base_dir_schema = CreateSchema<string> ("library-location", null, "The base directory under which files for this library are stored", null);
            copy_on_import = CreateSchema<bool> ("copy-on-import", false, "Copy files on import", "Copy and rename files to library directory when importing");
            move_files = CreateSchema<bool>("move-on-info-save", false, "Move files on info save", "Move files within the library directory when saving track info");

            AfterInitialized ();

            Section library_section = PreferencesPage.Add (new Section ("library-location", SectionName, 2));

            library_section.Add (base_dir_schema);

            if (this.HasCopyOnImport || this.HasMoveFiles) {
                var file_system = PreferencesPage.FindOrAdd (
                    new Section ("file-system", Catalog.GetString ("File Organization"), 5));

                if (this.HasCopyOnImport) {
                    file_system.Add (new SchemaPreference<bool> (
                        copy_on_import, Catalog.GetString ("Co_py files to media folder when importing")));
                }

                if (this.HasMoveFiles) {
                    file_system.Add (new SchemaPreference<bool> (
                        move_files,
                        Catalog.GetString ("_Update file and folder names"),
                        Catalog.GetString ("Rename files and folders according to media metadata")
                    ));
                }
            }
        }

        public string AttributesCondition {
            get { return String.Format ("((CoreTracks.Attributes & {0}) == {0} AND (CoreTracks.Attributes & {1}) == 0)", (int)MediaTypes, (int)NotMediaTypes); }
        }

        private string sync_condition;
        public string SyncCondition {
            get { return sync_condition; }
            protected set { sync_condition = value; }
        }

        private TrackMediaAttributes media_types;
        protected TrackMediaAttributes MediaTypes {
            get { return media_types; }
            set { media_types = value; }
        }

        private TrackMediaAttributes not_media_types = TrackMediaAttributes.None;
        protected TrackMediaAttributes NotMediaTypes {
            get { return not_media_types; }
            set { not_media_types = value; }
        }

        public override string PreferencesPageId {
            get { return UniqueId; }
        }

        public override bool HasEditableTrackProperties {
            get { return true; }
        }

        public override string BaseDirectory {
            get {
                string dir = base_dir_schema.Get ();
                if (dir == null) {
                    BaseDirectory = dir = DefaultBaseDirectory;
                }
                return dir;
            }
            protected set {
                base_dir_schema.Set (value);
                base.BaseDirectory = value;
            }
        }

        public abstract string DefaultBaseDirectory { get; }

        public bool CopyOnImport {
            get { return copy_on_import.Get (); }

            protected set { copy_on_import.Set (value); }
        }

        public virtual bool HasCopyOnImport {
            get { return false; }
        }

        public bool MoveFiles {
            get { return move_files.Get (); }

            protected set { move_files.Set (value); }
        }

        public virtual bool HasMoveFiles {
            get { return false; }
        }

        public override bool Indexable {
            get { return true; }
        }

        protected virtual string SectionName {
            get {
                // Translators: {0} is the library name, eg 'Music Library' or 'Podcasts'
                return String.Format (Catalog.GetString ("{0} Folder"), Name);
            }
        }

        /*public override void CopyTrackTo (DatabaseTrackInfo track, SafeUri uri, UserJob job)
        {
            Banshee.IO.File.Copy (track.Uri, uri, false);
        }*/

        public override void AddTrack (DatabaseTrackInfo track)
        {
            // Ignore if already have it
            if (track.PrimarySourceId == DbId)
                return;

            PrimarySource source = track.PrimarySource;

            // If it's from a local primary source, change its PrimarySource
            if (source.IsLocal || source is LibrarySource) {
                track.PrimarySource = this;

                if (!(source is LibrarySource)) {
                    track.CopyToLibraryIfAppropriate (false);
                }

                track.Save (false);

                // TODO optimize, remove this?  I think it makes moving items
                // between local libraries very slow.
                //source.NotifyTracksChanged ();
            } else {
                // Figure out where we should put it if were to copy it
                var pattern = this.PathPattern ?? MusicLibrarySource.MusicFileNamePattern;
                string path = pattern.BuildFull (BaseDirectory, track);
                SafeUri uri = new SafeUri (path);

                // Make sure it's not already in the library
                // TODO optimize - no need to recreate this long [] every time
                if (DatabaseTrackInfo.ContainsUri (uri, new long [] {DbId})) {
                    return;
                }

                // Since it's not, copy it and create a new TrackInfo object
                track.PrimarySource.CopyTrackTo (track, uri, AddTrackJob);

                // Create a new entry in CoreTracks for the copied file
                DatabaseTrackInfo new_track = new DatabaseTrackInfo (track);
                new_track.Uri = uri;
                new_track.PrimarySource = this;
                new_track.Save (false);
            }
        }
    }
}
