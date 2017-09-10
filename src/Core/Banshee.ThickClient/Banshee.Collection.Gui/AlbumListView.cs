//
// AlbumListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Preferences;
using Banshee.Configuration;
using Banshee.Library;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Gui;

using Mono.Unix;

namespace Banshee.Collection.Gui
{
    public class AlbumListView : TrackFilterListView<AlbumInfo>
    {
        private ColumnCellAlbum renderer;
        private Column classic_layout_column;
        private DataViewLayoutGrid grid_layout;
        private bool? album_grid_rendered = null;

        public AlbumListView () : base ()
        {
            renderer = new ColumnCellAlbum ();
            grid_layout = new DataViewLayoutGrid () {
                ChildAllocator = () => new DataViewChildAlbum (),
                View = this
            };
            grid_layout.ChildCountChanged += (o, e) => {
                var artwork_manager = ServiceManager.Get<ArtworkManager> ();
                if (artwork_manager != null && e.Value > 0) {
                    int size = (int)((DataViewChildAlbum)grid_layout[0]).ImageSize;
                    artwork_manager.ChangeCacheSize (size, e.Value);
                }
            };
            ViewLayout = grid_layout;

            DisableAlbumGridPref = new SchemaPreference<bool> (DisableAlbumGrid,
                    Catalog.GetString ("Disable album grid"),
                    Catalog.GetString ("Disable album grid and show the classic layout instead"),
                    ToggleAlbumGrid);

            ServiceManager.SourceManager.SourceRemoved += UninstallPreferences;
            ServiceManager.SourceManager.SourceAdded += InstallPreferences;
            if (ServiceManager.SourceManager.MusicLibrary != null) {
                InstallPreferences ();
            }

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.TrackInfoUpdated);
            Banshee.Metadata.MetadataService.Instance.ArtworkUpdated += OnArtworkUpdated;
        }

        protected AlbumListView (IntPtr ptr) : base () {}

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
                Banshee.Metadata.MetadataService.Instance.ArtworkUpdated -= OnArtworkUpdated;
            }
            base.Dispose (disposing);
        }

        private void ToggleAlbumGrid ()
        {
            if (album_grid_rendered.HasValue &&
                !DisableAlbumGrid.Get ().Equals (album_grid_rendered.Value)) {
                return;
            }

            if (DisableAlbumGrid.Get ()) {
                SwitchToAlbumList ();
            } else {
                SwitchToAlbumGrid ();
            }
        }

        private PreferenceBase disable_album_grid;
        private MusicLibrarySource music_lib = null;

        private void InstallPreferences (Sources.SourceAddedArgs args)
        {
            if (!args.Source.Equals (ServiceManager.SourceManager.MusicLibrary)) {
                return;
            }

            InstallPreferences ();
        }

        private void InstallPreferences ()
        {
            music_lib = ServiceManager.SourceManager.MusicLibrary;
            disable_album_grid = music_lib.PreferencesPage["misc"].FindOrAdd (DisableAlbumGridPref);

            ServiceManager.SourceManager.SourceAdded -= InstallPreferences;
        }

        private void UninstallPreferences (Sources.SourceEventArgs args)
        {
            if (!args.Source.Equals (music_lib)) {
                return;
            }

            music_lib.PreferencesPage["misc"].Remove (disable_album_grid);
            music_lib = null;

            ServiceManager.SourceManager.SourceRemoved -= UninstallPreferences;
        }

        protected void SwitchToAlbumList ()
        {
            ViewLayout = null;
            if (classic_layout_column == null) {
                classic_layout_column = new Column ("Album", renderer, 1.0);
            }
            column_controller.Add (classic_layout_column);
            ColumnController = column_controller;
            album_grid_rendered = false;
        }

        protected void SwitchToAlbumGrid ()
        {
            if (classic_layout_column != null) {
                column_controller.Remove (classic_layout_column);
            }
            ColumnController = null;
            ViewLayout = grid_layout;
            album_grid_rendered = true;
        }

        private static SchemaPreference<bool> DisableAlbumGridPref = null;

        private static readonly SchemaEntry<bool> DisableAlbumGrid = new SchemaEntry<bool> (
            "player_window", "disable_album_grid",
            false,
            "Disable album grid",
            "Disable album grid and show the classic layout instead"
        );

        protected override bool OnWidgetEvent (Gdk.Event evnt)
        {
            if (album_grid_rendered == null && evnt.Type == Gdk.EventType.Expose) {
                ToggleAlbumGrid ();
            }
            return base.OnWidgetEvent (evnt);
        }

        protected override Gdk.Size OnMeasureChild ()
        {
            return ViewLayout != null
                ? base.OnMeasureChild ()
                : new Gdk.Size (0, renderer.ComputeRowHeight (this));
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            // TODO: a) Figure out if the track that changed is actually in view
            //       b) xfade the artwork if it is, that'd be slick
            QueueDraw ();
        }

        private void OnArtworkUpdated (IBasicTrackInfo track)
        {
            QueueDraw ();
        }
    }
}
