//
// SourceActions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using Mono.Unix;
using Gtk;

using Hyena;

using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.Playlist.Gui;
using Banshee.Playlists.Formats;
using Banshee.SmartPlaylist;

namespace Banshee.Gui
{
    public class SourceActions : BansheeActionGroup
    {
        public IHasSourceView SourceView {
            get;
            set;
        }

        public event Action<Source> Updated;

        public Source ActionSource {
            get { return ((SourceView == null) ? null :  SourceView.HighlightedSource) ?? ActiveSource; }
        }

        public override PrimarySource ActivePrimarySource {
            get { return (SourceView.HighlightedSource as PrimarySource) ?? base.ActivePrimarySource; }
        }

        public SourceActions () : base ("Source")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("NewPlaylistAction", null,
                    Catalog.GetString ("_New Playlist"), "<control>N",
                    Catalog.GetString ("Create a new empty playlist"), OnNewPlaylist),

                new ActionEntry ("NewSmartPlaylistAction", null,
                    Catalog.GetString ("New _Smart Playlist..."), "",
                    Catalog.GetString ("Create a new smart playlist"), OnNewSmartPlaylist),

                /*new ActionEntry ("NewSmartPlaylistFromSearchAction", null,
                    Catalog.GetString ("New _Smart Playlist _From Search"), null,
                    Catalog.GetString ("Create a new smart playlist from the current search"), OnNewSmartPlaylistFromSearch),*/

                new ActionEntry ("SourceContextMenuAction", null,
                    String.Empty, null, null, OnSourceContextMenu),

                new ActionEntry ("ImportSourceAction", null,
                    Catalog.GetString ("Import to Library"), null,
                    Catalog.GetString ("Import source to library"), OnImportSource),

                new ActionEntry ("RenameSourceAction", null,
                    Catalog.GetString ("Rename"), "F2", Catalog.GetString ("Rename"), OnRenameSource),

                new ActionEntry ("ExportPlaylistAction", null,
                    Catalog.GetString ("Export Playlist..."), null,
                    Catalog.GetString ("Export a playlist"), OnExportPlaylist),

                new ActionEntry ("UnmapSourceAction", null,
                    Catalog.GetString ("Unmap"), "<shift>Delete", Catalog.GetString("Unmap"), OnUnmapSource),

                new ActionEntry ("SourcePropertiesAction", null,
                    Catalog.GetString ("Source Properties"), null, null, OnSourceProperties),

                new ActionEntry ("SortChildrenAction", Stock.SortDescending,
                    Catalog.GetString ("Sort Children by"), null, null,
                    OnSortChildrenMenu),

                new ActionEntry ("OpenSourceSwitcher", null,
                    Catalog.GetString ("Switch Source"), "G",
                    Catalog.GetString ("Switch to a source by typing its name"),
                    null),

                new ActionEntry ("SourcePreferencesAction", null, Catalog.GetString ("Preferences"), null,
                    Catalog.GetString ("Edit preferences related to this source"), OnSourcePreferences),

            });

            this["NewSmartPlaylistAction"].ShortLabel = Catalog.GetString ("New _Smart Playlist");
            this["NewPlaylistAction"].IconName = Stock.New;
            this["UnmapSourceAction"].IconName = Stock.Delete;
            this["SourcePropertiesAction"].IconName = Stock.Properties;
            this["SortChildrenAction"].HideIfEmpty = false;
            this["SourcePreferencesAction"].IconName = Stock.Preferences;

            AddImportant (
                new ActionEntry ("RefreshSmartPlaylistAction", Stock.Refresh,
                    Catalog.GetString ("Refresh"), null,
                    Catalog.GetString ("Refresh this randomly sorted smart playlist"), OnRefreshSmartPlaylist)
            );

            //ServiceManager.SourceManager.SourceUpdated += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceViewChanged += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceAdded += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceRemoved += OnPlayerEngineStateChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
            Actions.GlobalActions["EditMenuAction"].Activated += HandleEditMenuActivated;
        }

#region State Event Handlers

        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            ThreadAssist.ProxyToMain (() => {
                UpdateActions ();

                if (last_source != null) {
                    last_source.Updated -= HandleActiveSourceUpdated;
                }

                if (ActiveSource != null) {
                    ActiveSource.Updated += HandleActiveSourceUpdated;
                }
            });
        }

        private void HandleEditMenuActivated (object sender, EventArgs args)
        {
            UpdateActions ();
        }

        private void HandleActiveSourceUpdated (object o, EventArgs args)
        {
            ThreadAssist.ProxyToMain (() => {
                UpdateActions (true);
            });
        }

#endregion

#region Action Event Handlers

        private void OnNewPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = new PlaylistSource (Catalog.GetString ("New Playlist"), ActivePrimarySource);
            playlist.Save ();
            playlist.PrimarySource.AddChildSource (playlist);
            playlist.NotifyUser ();
            SourceView.BeginRenameSource (playlist);
        }


        private void OnNewSmartPlaylist (object o, EventArgs args)
        {
            Editor ed = new Editor (ActivePrimarySource);
            ed.RunDialog ();
        }

        /*private void OnNewSmartPlaylistFromSearch (object o, EventArgs args)
        {
            Source active_source = ServiceManager.SourceManager.ActiveSource;
            QueryNode node = UserQueryParser.Parse (active_source.FilterQuery, BansheeQuery.FieldSet);

            if (node == null) {
                return;
            }

            // If the active source is a playlist or smart playlist, add that as a condition to the query
            QueryListNode list_node = null;
            if (active_source is PlaylistSource) {
                list_node = new QueryListNode (Keyword.And);
                list_node.AddChild (QueryTermNode.ParseUserQuery (BansheeQuery.FieldSet, String.Format ("playlistid:{0}", (active_source as PlaylistSource).DbId)));
                list_node.AddChild (node);
            } else if (active_source is SmartPlaylistSource) {
                list_node = new QueryListNode (Keyword.And);
                list_node.AddChild (QueryTermNode.ParseUserQuery (BansheeQuery.FieldSet, String.Format ("smartplaylistid:{0}", (active_source as SmartPlaylistSource).DbId)));
                list_node.AddChild (node);
            }

            SmartPlaylistSource playlist = new SmartPlaylistSource (active_source.FilterQuery);
            playlist.ConditionTree = list_node ?? node;
            playlist.Save ();
            ServiceManager.SourceManager.Library.AddChildSource (playlist);
            playlist.NotifyUpdated ();

            // TODO should begin editing the name after making it, but this changed
            // the ActiveSource to the new playlist and we don't want that.
            //SourceView.BeginRenameSource (playlist);
        }*/

        private void OnSourceContextMenu (object o, EventArgs args)
        {
            UpdateActions ();

            string path = ActionSource.Properties.Get<string> ("GtkActionPath") ?? "/SourceContextMenu";
            Gtk.Menu menu = Actions.UIManager.GetWidget (path) as Menu;
            if (menu == null || menu.Children.Length == 0) {
                SourceView.ResetHighlight ();
                UpdateActions ();
                return;
            }

            int visible_children = 0;
            foreach (Widget child in menu) {
                if (child.Visible) {
                    visible_children++;
                }
            }

            if (visible_children == 0) {
                SourceView.ResetHighlight ();
                UpdateActions ();
                return;
            }

            menu.Show ();
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
            menu.SelectionDone += delegate {
                SourceView.ResetHighlight ();
                UpdateActions ();
            };
        }

        private void OnImportSource (object o, EventArgs args)
        {
            ((IImportSource)ActionSource).Import ();
        }

        private void OnRenameSource (object o, EventArgs args)
        {
            SourceView.BeginRenameSource (ActionSource);
        }

        private void OnExportPlaylist (object o, EventArgs args)
        {
            AbstractPlaylistSource source = ActionSource as AbstractPlaylistSource;
            if (source == null) {
                return;
            }
            source.Activate ();

            PlaylistExportDialog chooser = new PlaylistExportDialog (source.Name, PrimaryWindow);

            string uri = null;
            PlaylistFormatDescription format = null;
            int response = chooser.Run ();
            if (response == (int) ResponseType.Ok) {
                uri = chooser.Uri;
                // Get the format that the user selected.
                format = chooser.GetExportFormat ();
            }
            chooser.Destroy ();

            if (uri == null) {
                // User cancelled export.
                return;
            }

            try {
                IPlaylistFormat playlist = (IPlaylistFormat)Activator.CreateInstance (format.Type);
                SafeUri suri = new SafeUri (uri);
                if (suri.IsLocalPath) {
                    playlist.BaseUri = new Uri (System.IO.Path.GetDirectoryName (suri.LocalPath));
                }
                playlist.Save (Banshee.IO.File.OpenWrite (new SafeUri (uri), true), source);
            } catch (Exception e) {
                Console.WriteLine (e);
                Log.Error (Catalog.GetString ("Could not export playlist"), e.Message, true);
            }
        }

        private void OnUnmapSource (object o, EventArgs args)
        {
            IUnmapableSource source = ActionSource as IUnmapableSource;
            if (source != null && source.CanUnmap && (!source.ConfirmBeforeUnmap || ConfirmUnmap (source)))
                source.Unmap ();
        }

        private void OnRefreshSmartPlaylist (object o, EventArgs args)
        {
            SmartPlaylistSource playlist = ActionSource as SmartPlaylistSource;

            if (playlist != null && playlist.CanRefresh) {
                playlist.RefreshAndReload ();
            }
        }

        private void OnSourceProperties (object o, EventArgs args)
        {
            if (ActionSource.Properties.Contains ("SourceProperties.GuiHandler")) {
                ActionSource.Properties.Get<Source.OpenPropertiesDelegate> ("SourceProperties.GuiHandler") ();
                return;
            }

            SmartPlaylistSource source = ActionSource as SmartPlaylistSource;
            if (source != null) {
                Editor ed = new Editor (source);
                ed.RunDialog ();
            }
        }

        private void OnSortChildrenMenu (object o, EventArgs args)
        {
            foreach (Widget proxy_widget in this["SortChildrenAction"].Proxies) {
                MenuItem menu = proxy_widget as MenuItem;
                if (menu == null) {
                    continue;
                }

                Menu submenu = BuildSortMenu (ActionSource);
                menu.Submenu = submenu;
                submenu.ShowAll ();
            }
        }

        private void OnSourcePreferences (object o, EventArgs args)
        {
            try {
                Banshee.Preferences.Gui.PreferenceDialog dialog = new Banshee.Preferences.Gui.PreferenceDialog ();
                dialog.ShowSourcePageId (ActionSource.PreferencesPageId);
                dialog.Run ();
                dialog.Destroy ();
            } catch (ApplicationException) {
            }
        }

#endregion

#region Utility Methods

        private Source last_source = null;
        private void UpdateActions ()
        {
            UpdateActions (false);
        }

        private void UpdateActions (bool force)
        {
            Source source = ActionSource;

            if ((force || source != last_source) && source != null) {
                IUnmapableSource unmapable = (source as IUnmapableSource);
                IImportSource import_source = (source as IImportSource);
                SmartPlaylistSource smart_playlist = (source as SmartPlaylistSource);
                PrimarySource primary_source = (source as PrimarySource) ?? (source.Parent as PrimarySource);

                UpdateAction ("UnmapSourceAction", unmapable != null, unmapable != null && unmapable.CanUnmap, source);
                UpdateAction ("RenameSourceAction", source.CanRename, true, null);
                UpdateAction ("ImportSourceAction", import_source != null, import_source != null && import_source.CanImport, source);
                UpdateAction ("ExportPlaylistAction", source is AbstractPlaylistSource, true, source);
                UpdateAction ("SourcePropertiesAction", source.HasProperties, true, source);
                UpdateAction ("SourcePreferencesAction", source.PreferencesPageId != null, true, source);
                UpdateAction ("RefreshSmartPlaylistAction", smart_playlist != null && smart_playlist.CanRefresh, true, source);
                this["OpenSourceSwitcher"].Visible = false;

                bool playlists_writable = primary_source != null && primary_source.SupportsPlaylists && !primary_source.PlaylistsReadOnly;
                UpdateAction ("NewPlaylistAction", playlists_writable, true, source);
                UpdateAction ("NewSmartPlaylistAction", playlists_writable, true, source);
                /*UpdateAction ("NewSmartPlaylistFromSearchAction", (source is LibrarySource || source.Parent is LibrarySource),
                        !String.IsNullOrEmpty (source.FilterQuery), source);*/

                IFilterableSource filterable_source = source as IFilterableSource;
                bool has_browser = filterable_source != null && filterable_source.AvailableFilters.Count > 0;

                ActionGroup browser_actions = Actions.FindActionGroup ("BrowserView");
                if (browser_actions != null) {
                    UpdateAction (browser_actions["BrowserTopAction"], has_browser);
                    UpdateAction (browser_actions["BrowserLeftAction"], has_browser);
                    UpdateAction (browser_actions["BrowserVisibleAction"], has_browser);
                }

                ActionGroup browser_config_actions = Actions.FindActionGroup ("BrowserConfiguration");
                if (browser_config_actions != null) {
                    // TODO: Be more specific than has_browser. Those actions have no effect
                    // on some sources that have a browser, like podcasts or radio.
                    UpdateAction (browser_config_actions["BrowserContentMenuAction"], has_browser);
                }

                last_source = source;
            }

            if (source != null) {
                UpdateAction ("SortChildrenAction", source.ChildSortTypes.Length > 0 && source.Children.Count > 1, true, source);
            }

            Action<Source> handler = Updated;
            if (handler != null) {
                handler (source);
            }
        }

        private static bool ConfirmUnmap (IUnmapableSource source)
        {
            string key = "no_confirm_unmap_" + source.GetType ().Name.ToLower ();
            bool do_not_ask = ConfigurationClient.Instance.Get<bool> ("sources", key, false);

            if (do_not_ask) {
                return true;
            }

            Hyena.Widgets.HigMessageDialog dialog = new Hyena.Widgets.HigMessageDialog (
                ServiceManager.Get<GtkElementsService> ().PrimaryWindow,
                Gtk.DialogFlags.Modal,
                Gtk.MessageType.Question,
                Gtk.ButtonsType.Cancel,
                String.Format (Catalog.GetString ("Are you sure you want to delete this {0}?"),
                    source.GenericName.ToLower ()),
                source.Name);

            dialog.AddButton (Gtk.Stock.Delete, Gtk.ResponseType.Ok, false);

            Gtk.Alignment alignment = new Gtk.Alignment (0.0f, 0.0f, 0.0f, 0.0f);
            alignment.TopPadding = 10;
            Gtk.CheckButton confirm_button = new Gtk.CheckButton (String.Format (Catalog.GetString (
                "Do not ask me this again"), source.GenericName.ToLower ()));
            confirm_button.Toggled += delegate {
                do_not_ask = confirm_button.Active;
            };
            alignment.Add (confirm_button);
            alignment.ShowAll ();
            dialog.LabelVBox.PackStart (alignment, false, false, 0);

            try {
                if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
                    ConfigurationClient.Instance.Set<bool> ("sources", key, do_not_ask);
                    return true;
                }

                return false;
            } finally {
                dialog.Destroy ();
            }
        }

        private Menu BuildSortMenu (Source source)
        {
            Menu menu = new Menu ();
            RadioMenuItem [] group = null;
            foreach (SourceSortType sort_type in source.ChildSortTypes) {
                RadioMenuItem item = new RadioMenuItem (group, sort_type.Label);
                group = item.Group;
                item.Active = (sort_type == source.ActiveChildSort);
                item.Toggled += BuildSortChangedHandler (source, sort_type);
                menu.Append (item);
            }

            menu.Append (new SeparatorMenuItem ());

            CheckMenuItem sort_types_item = new CheckMenuItem (Catalog.GetString ("Separate by Type"));
            sort_types_item.Active = source.SeparateChildrenByType;
            sort_types_item.Toggled += OnSeparateTypesChanged;
            menu.Append (sort_types_item);

            return menu;
        }

        private void OnSeparateTypesChanged (object o, EventArgs args)
        {
            CheckMenuItem item = o as CheckMenuItem;
            if (item != null) {
                ActionSource.SeparateChildrenByType = item.Active;
            }
        }

        private static EventHandler BuildSortChangedHandler (Source source, SourceSortType sort_type)
        {
            return delegate (object sender, EventArgs args) {
                RadioMenuItem item = sender as RadioMenuItem;
                if (item != null && item.Active) {
                    source.SortChildSources (sort_type);
                }
            };
        }

#endregion

    }
}
