//
// PlayerInterface.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2007-2010 Novell, Inc.
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
using System.Linq;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;
using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.MediaEngine;
using Banshee.Configuration;

using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Gui.Dialogs;
using Banshee.Widgets;
using Banshee.Collection.Gui;
using Banshee.Sources.Gui;

namespace Nereid
{
    public class PlayerInterface : BaseClientWindow, IClientWindow, IDBusObjectName, IService, IDisposable, IHasSourceView
    {
        const string CONFIG_NAMESPACE = "player_window";
        static readonly SchemaEntry<int> WidthSchema = WindowConfiguration.NewWidthSchema (CONFIG_NAMESPACE, 1024);
        static readonly SchemaEntry<int> HeightSchema = WindowConfiguration.NewHeightSchema (CONFIG_NAMESPACE, 700);
        static readonly SchemaEntry<int> XPosSchema = WindowConfiguration.NewXPosSchema (CONFIG_NAMESPACE);
        static readonly SchemaEntry<int> YPosSchema = WindowConfiguration.NewYPosSchema (CONFIG_NAMESPACE);
        static readonly SchemaEntry<bool> MaximizedSchema = WindowConfiguration.NewMaximizedSchema (CONFIG_NAMESPACE);

        // Major Layout Components
        private VBox primary_vbox;
        private Table header_table;
        private MainMenu main_menu;
        private Toolbar header_toolbar;
        private HBox footer_toolbar;
        private HPaned views_pane;
        private ViewContainer view_container;
        private VBox source_box;
        private Widget track_info_container;
        private CoverArtDisplay cover_art_display;
        private Widget cover_art_container;
        private ConnectedSeekSlider seek_slider;
        private TaskStatusIcon task_status;
        private Alignment search_entry_align;

        // Major Interaction Components
        private SourceView source_view;
        private CompositeTrackSourceContents composite_view;
        private ObjectListSourceContents object_view;
        private Label status_label;

        public MainMenu MainMenu {
            get { return main_menu; }
        }

        public Toolbar HeaderToolbar {
            get { return header_toolbar; }
        }

        public Table HeaderTable {
            get { return header_table; }
        }

        protected PlayerInterface (IntPtr ptr) : base (ptr)
        {
        }

        private void SetSimple (bool simple)
        {
            var widgets = new Widget [] { main_menu, source_box, footer_toolbar, track_info_container };
            foreach (var w in widgets.Where (w => w != null)) {
                w.Visible = !simple;
            }
        }

        public PlayerInterface () :
            base (Catalog.GetString ("Banshee Media Player"),
                  new WindowConfiguration (WidthSchema, HeightSchema, XPosSchema, YPosSchema, MaximizedSchema))
        {
        }

        protected override void Initialize ()
        {
            InitialShowPresent ();
        }

        private bool interface_constructed;

        protected override void OnShown ()
        {
            if (interface_constructed) {
                base.OnShown ();
                return;
            }

            interface_constructed = true;
            uint timer = Log.DebugTimerStart ();

            BuildPrimaryLayout ();
            ConnectEvents ();

            ActionService.SourceActions.SourceView = this;
            composite_view.TrackView.HasFocus = true;

            Log.DebugTimerPrint (timer, "Constructed Nereid interface: {0}");

            base.OnShown ();
        }

#region System Overrides

        protected override void Dispose (bool disposing)
        {
            lock (this) {
                ServiceManager.SourceManager.ActiveSourceChanged -= OnActiveSourceChanged;
                ServiceManager.SourceManager.SourceUpdated -= OnSourceUpdated;

                if (disposing) {
                    Hide ();
                }
                base.Dispose (disposing);
            }
        }

#endregion

#region Interface Construction

        private void BuildPrimaryLayout ()
        {
            primary_vbox = new VBox ();

            BuildHeader ();
            BuildViews ();
            BuildFooter ();

            search_entry_align = new Alignment (1.0f, 0.5f, 0f, 0f);
            var box = new HBox () { Spacing = 2 };
            var grabber = new GrabHandle ();
            var search_entry = view_container.SearchEntry;
            grabber.ControlWidthOf (search_entry, 150, 350, false);

            int search_entry_width = SearchEntryWidth.Get ();

            // -1 indicates that height should be preserved
            search_entry.SetSizeRequest (search_entry_width, -1);

            search_entry.SizeAllocated += (o, a) => {
                SearchEntryWidth.Set (search_entry.Allocation.Width);
            };

            box.PackStart (grabber, false, false, 0);
            box.PackStart (view_container.SearchEntry, false, false, 0);
            search_entry_align.Child = box;

            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/SearchEntry", search_entry_align);
            search_entry_align.Visible = view_container.SearchSensitive = true;
            search_entry_align.ShowAll ();

            primary_vbox.Show ();
            Add (primary_vbox);
        }

        private void BuildHeader ()
        {
            header_table = new Table (2, 2, false);
            header_table.Show ();
            header_table.Vexpand = false;
            primary_vbox.PackStart (header_table, false, false, 0);

            main_menu = new MainMenu ();

            if (!PlatformDetection.IsMac) {
                main_menu.Show ();
                header_table.Attach (main_menu, 0, 1, 0, 1,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            }

            Alignment toolbar_alignment = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
            toolbar_alignment.TopPadding = 3u;
            toolbar_alignment.BottomPadding = 3u;

            header_toolbar = (Toolbar)ActionService.UIManager.GetWidget ("/HeaderToolbar");
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
            header_toolbar.Show ();

            toolbar_alignment.Add (header_toolbar);
            toolbar_alignment.Show ();

            header_table.Attach (toolbar_alignment, 0, 2, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink, 0, 0);

            var next_button = new NextButton (ActionService);
            next_button.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/NextArrowButton", next_button);

            seek_slider = new ConnectedSeekSlider () { Resizable = ShowSeekSliderResizer.Get () };
            seek_slider.SeekSlider.WidthRequest = SeekSliderWidth.Get ();
            seek_slider.SeekSlider.SizeAllocated += (o, a) => {
                SeekSliderWidth.Set (seek_slider.SeekSlider.Allocation.Width);
            };
            seek_slider.ShowAll ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/SeekSlider", seek_slider);

            var track_info_display = new ClassicTrackInfoDisplay ();
            track_info_display.Show ();
            track_info_container = TrackInfoDisplay.GetEditable (track_info_display);
            track_info_container.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/TrackInfoDisplay", track_info_container, true);

            var volume_button = new ConnectedVolumeButton ();
            volume_button.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/VolumeButton", volume_button);
        }

        private void BuildViews ()
        {
            source_box = new VBox ();

            views_pane = new HPaned ();
            PersistentPaneController.Control (views_pane, SourceViewWidth);
            view_container = new ViewContainer ();

            source_view = new SourceView ();
            composite_view = new CompositeTrackSourceContents ();

            Container source_scroll;
            Hyena.Widgets.ScrolledWindow window;
            if (ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
                window = new Hyena.Widgets.SmoothScrolledWindow ();
            } else {
                window = new Hyena.Widgets.ScrolledWindow ();
            }

            window.Add (source_view);
            source_scroll = window;

            composite_view.TrackView.HeaderVisible = false;
            view_container.Content = composite_view;

            source_box.PackStart (source_scroll, true, true, 0);
            source_box.PackStart (new UserJobTileHost (), false, false, 0);

            UpdateCoverArtDisplay ();

            source_view.SetSizeRequest (125, -1);
            view_container.SetSizeRequest (425, -1);

            views_pane.Pack1 (source_box, false, false);
            views_pane.Pack2 (view_container, true, false);

            source_box.ShowAll ();
            view_container.Show ();
            views_pane.Show ();

            primary_vbox.PackStart (views_pane, true, true, 0);
        }

        private void UpdateCoverArtDisplay ()
        {
            if (ShowCoverArt.Get ()) {
                if (cover_art_display == null && source_box != null) {
                    cover_art_display = new CoverArtDisplay () { Visible = true };
                    source_box.SizeAllocated += OnSourceBoxSizeAllocated;
                    cover_art_display.HeightRequest = SourceViewWidth.Get ();
                    source_box.PackStart (cover_art_container = TrackInfoDisplay.GetEditable (cover_art_display), false, false, 4);
                    source_box.ShowAll ();
                }
            } else if (cover_art_display != null) {
                cover_art_display.Hide ();
                source_box.Remove (cover_art_container);
                source_box.SizeAllocated -= OnSourceBoxSizeAllocated;
                cover_art_display.Dispose ();
                cover_art_display = null;
            }
        }

        private void OnSourceBoxSizeAllocated (object o, EventArgs args)
        {
            cover_art_display.HeightRequest = source_box.Allocation.Width;
        }

        private void BuildFooter ()
        {
            footer_toolbar = new HBox () { BorderWidth = 2 };

            task_status = new Banshee.Gui.Widgets.TaskStatusIcon ();

            EventBox status_event_box = new EventBox ();
            status_event_box.ButtonPressEvent += OnStatusBoxButtonPress;

            status_label = new Label ();
            status_event_box.Add (status_label);

            HBox status_hbox = new HBox (true, 0);
            status_hbox.PackStart (status_event_box, false, false, 0);

            Alignment status_align = new Alignment (0.5f, 0.5f, 1.0f, 1.0f);
            status_align.Add (status_hbox);

            RepeatActionButton repeat_button = new RepeatActionButton ();
            repeat_button.SizeAllocated += delegate (object o, Gtk.SizeAllocatedArgs args) {
                status_align.LeftPadding = (uint)args.Allocation.Width;
            };

            footer_toolbar.PackStart (task_status, false, false, 0);
            footer_toolbar.PackStart (status_align, true, true, 0);
            footer_toolbar.PackStart (repeat_button, false, false, 0);

            footer_toolbar.ShowAll ();
            primary_vbox.PackStart (footer_toolbar, false, true, 0);
        }

        private void OnStatusBoxButtonPress (object o, ButtonPressEventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source != null) {
                source.CycleStatusFormat ();
                UpdateSourceInformation ();
            }
        }

#endregion

#region Events and Logic Setup

        protected override void ConnectEvents ()
        {
            base.ConnectEvents ();

            // Service events
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            ServiceManager.SourceManager.SourceUpdated += OnSourceUpdated;

            ActionService.TrackActions ["SearchForSameArtistAction"].Activated += OnProgrammaticSearch;
            ActionService.TrackActions ["SearchForSameAlbumAction"].Activated += OnProgrammaticSearch;

            (ActionService.ViewActions ["ShowCoverArtAction"] as Gtk.ToggleAction).Active = ShowCoverArt.Get ();
            ActionService.ViewActions ["ShowCoverArtAction"].Activated += (o, a) => {
                ShowCoverArt.Set ((o as Gtk.ToggleAction).Active);
                UpdateCoverArtDisplay ();
            };

            // UI events
            view_container.SearchEntry.Changed += OnSearchEntryChanged;
            // TODO: Check that this still works, it was using SizeRequested
            views_pane.SizeAllocated += delegate {
                SourceViewWidth.Set (views_pane.Position);
            };

            source_view.RowActivated += delegate {
                Source source = ServiceManager.SourceManager.ActiveSource;
                var handler = source.Properties.Get<System.Action> ("ActivationAction");
                if (handler != null) {
                    handler ();
                } else if (source is ITrackModelSource) {
                    ServiceManager.PlaybackController.NextSource = (ITrackModelSource)source;
                    // Allow changing the play source without stopping the current song by
                    // holding ctrl when activating a source. After the song is done, playback will
                    // continue from the new source.
                    if (GtkUtilities.NoImportantModifiersAreSet (Gdk.ModifierType.ControlMask)) {
                        ServiceManager.PlaybackController.Next ();
                    }
                }
            };
        }

#endregion

#region Service Event Handlers

        private void OnProgrammaticSearch (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            view_container.SearchEntry.Ready = false;
            view_container.SearchEntry.Query = source.FilterQuery;
            view_container.SearchEntry.Ready = true;
        }

        private Source previous_source = null;
        private TrackListModel previous_track_model = null;
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                Source source = ServiceManager.SourceManager.ActiveSource;

                search_entry_align.Visible = view_container.SearchSensitive = source != null && source.CanSearch;

                if (source == null) {
                    return;
                }

                view_container.SearchEntry.Ready = false;
                view_container.SearchEntry.CancelSearch ();

                /* Translators: this is a verb (command), not a noun (things) */
                var msg = source.Properties.Get<string> ("SearchEntryDescription") ?? Catalog.GetString ("Search");
                view_container.SearchEntry.EmptyMessage = msg;
                view_container.SearchEntry.TooltipText = msg;

                if (source.FilterQuery != null) {
                    view_container.SearchEntry.Query = source.FilterQuery;
                    view_container.SearchEntry.ActivateFilter ((int)source.FilterType);
                }

                if (view_container.Content != null) {
                    view_container.Content.ResetSource ();
                }

                if (previous_track_model != null) {
                    previous_track_model.Reloaded -= HandleTrackModelReloaded;
                    previous_track_model = null;
                }

                if (source is ITrackModelSource) {
                    previous_track_model = (source as ITrackModelSource).TrackModel;
                    previous_track_model.Reloaded += HandleTrackModelReloaded;
                }

                if (previous_source != null) {
                    previous_source.Properties.PropertyChanged -= OnSourcePropertyChanged;
                }

                previous_source = source;
                previous_source.Properties.PropertyChanged += OnSourcePropertyChanged;

                UpdateSourceContents (source);

                UpdateSourceInformation ();
                view_container.SearchEntry.Ready = true;

                SetSimple (source.Properties.Get<bool> ("Nereid.SimpleUI"));
            });
        }

        private void OnSourcePropertyChanged (object o, PropertyChangeEventArgs args)
        {
            switch (args.PropertyName) {
                case "Nereid.SourceContents":
                    ThreadAssist.ProxyToMain (delegate {
                        UpdateSourceContents (previous_source);
                    });
                    break;

                case "FilterQuery":
                    var source = ServiceManager.SourceManager.ActiveSource;
                    var search_entry = source.Properties.Get<SearchEntry> ("Nereid.SearchEntry") ?? view_container.SearchEntry;
                    if (!search_entry.HasFocus) {
                        ThreadAssist.ProxyToMain (delegate {
                            view_container.SearchEntry.Ready = false;
                            view_container.SearchEntry.Query = source.FilterQuery;
                            view_container.SearchEntry.Ready = true;
                        });
                    }
                    break;
                case "Nereid.SimpleUI":
                    SetSimple (ServiceManager.SourceManager.ActiveSource.Properties.Get<bool> ("Nereid.SimpleUI"));
                    break;
            }
        }

        private void UpdateSourceContents (Source source)
        {
            if (source == null) {
                return;
            }

            // Connect the source models to the views if possible
            ISourceContents contents = source.GetProperty<ISourceContents> ("Nereid.SourceContents",
                source.GetInheritedProperty<bool> ("Nereid.SourceContentsPropagate"));

            view_container.ClearHeaderWidget ();
            view_container.ClearFooter ();

            if (contents != null) {
                if (view_container.Content != contents) {
                    view_container.Content = contents;
                }
                view_container.Content.SetSource (source);
                view_container.Show ();
            } else if (source is ITrackModelSource) {
                view_container.Content = composite_view;
                view_container.Content.SetSource (source);
                view_container.Show ();
            } else if (source is Hyena.Data.IObjectListModel) {
                if (object_view == null) {
                    object_view = new ObjectListSourceContents ();
                }

                view_container.Content = object_view;
                view_container.Content.SetSource (source);
                view_container.Show ();
            } else {
                view_container.Hide ();
            }

            // Associate the view with the model
            if (view_container.Visible && view_container.Content is ITrackModelSourceContents) {
                ITrackModelSourceContents track_content = view_container.Content as ITrackModelSourceContents;
                source.Properties.Set<IListView<TrackInfo>>  ("Track.IListView", track_content.TrackView);
            }

            var title_widget = source.Properties.Get<Widget> ("Nereid.SourceContents.TitleWidget");
            if (title_widget != null) {
                Hyena.Log.WarningFormat ("Nereid.SourceContents.TitleWidget is no longer used (from {0})", source.Name);
            }

            Widget header_widget = null;
            if (source.Properties.Contains ("Nereid.SourceContents.HeaderWidget")) {
                header_widget = source.Properties.Get<Widget> ("Nereid.SourceContents.HeaderWidget");
            }

            if (header_widget != null) {
                view_container.SetHeaderWidget (header_widget);
            }

            Widget footer_widget = null;
            if (source.Properties.Contains ("Nereid.SourceContents.FooterWidget")) {
                footer_widget = source.Properties.Get<Widget> ("Nereid.SourceContents.FooterWidget");
            }

            if (footer_widget != null) {
                view_container.SetFooter (footer_widget);
            }
        }

        private void OnSourceUpdated (SourceEventArgs args)
        {
            if (args.Source == ServiceManager.SourceManager.ActiveSource) {
                ThreadAssist.ProxyToMain (delegate {
                    UpdateSourceInformation ();
                });
            }
        }

#endregion

#region UI Event Handlers

        private void OnSearchEntryChanged (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source == null)
                return;

            source.FilterType = (TrackFilterType)view_container.SearchEntry.ActiveFilterID;
            source.FilterQuery = view_container.SearchEntry.Query;
        }

#endregion

#region Implement Interfaces

        // IHasSourceView
        public Source HighlightedSource {
            get { return source_view.HighlightedSource; }
        }

        public void BeginRenameSource (Source source)
        {
            source_view.BeginRenameSource (source);
        }

        public void ResetHighlight ()
        {
            source_view.ResetHighlight ();
        }

        public override Box ViewContainer {
            get { return view_container; }
        }

#endregion

#region Gtk.Window Overrides

        private bool accel_group_active = true;

        private void OnEntryFocusOutEvent (object o, FocusOutEventArgs args)
        {
            if (!accel_group_active) {
                AddAccelGroup (ActionService.UIManager.AccelGroup);
                accel_group_active = true;
            }

            (o as Widget).FocusOutEvent -= OnEntryFocusOutEvent;
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            bool focus_search = false;

            bool disable_keybindings = Focus is Gtk.Entry;
            if (!disable_keybindings) {
                var widget = Focus;
                while (widget != null) {
                    if (widget is IDisableKeybindings) {
                        disable_keybindings = true;
                        break;
                    }
                    widget = widget.Parent;
                }
            }

            // Don't disable them if ctrl is pressed, unless ctrl-a is pressed
            if (evnt.Key != Gdk.Key.a) {
                disable_keybindings &= (evnt.State & Gdk.ModifierType.ControlMask) == 0 &&
                    evnt.Key != Gdk.Key.Control_L && evnt.Key != Gdk.Key.Control_R;
            }

            if (disable_keybindings) {
                if (accel_group_active) {
                    RemoveAccelGroup (ActionService.UIManager.AccelGroup);
                    accel_group_active = false;

                    // Reinstate the AccelGroup as soon as the focus leaves the entry
                    Focus.FocusOutEvent += OnEntryFocusOutEvent;
                }
            } else {
                if (!accel_group_active) {
                    AddAccelGroup (ActionService.UIManager.AccelGroup);
                    accel_group_active = true;
                }
            }

            switch (evnt.Key) {
                case Gdk.Key.f:
                    if (Gdk.ModifierType.ControlMask == (evnt.State & Gdk.ModifierType.ControlMask)) {
                        focus_search = true;
                    }
                    break;

                case Gdk.Key.S:
                case Gdk.Key.s:
                case Gdk.Key.slash:
                    if (!disable_keybindings) {
                        focus_search = true;
                    }
                    break;
                case Gdk.Key.F3:
                    focus_search = true;
                    break;
                case Gdk.Key.F11:
                    ActionService.ViewActions["FullScreenAction"].Activate ();
                    break;
            }

            // The source might have its own custom search entry - use it if so
            var src = ServiceManager.SourceManager.ActiveSource;
            var search_entry = src.Properties.Get<SearchEntry> ("Nereid.SearchEntry") ?? view_container.SearchEntry;
            if (focus_search && search_entry.Visible && !source_view.EditingRow) {
                search_entry.GrabFocus ();
                search_entry.HasFocus = true;
                return true;
            }

            return base.OnKeyPressEvent (evnt);
        }

#endregion

#region Popup Status Bar
#if false

        private Gdk.FilterReturn OnGdkEventFilter (IntPtr xevent, Gdk.Event gdkevent)
        {
            if (!IsRealized || !IsMapped) {
                return Gdk.FilterReturn.Continue;
            }

            Gdk.ModifierType mask;
            int x, y;
            GdkWindow.GetPointer (out x, out y, out mask);
            return Gdk.FilterReturn.Continue;
        }

#endif
#endregion

#region Helper Functions

        private void HandleTrackModelReloaded (object sender, EventArgs args)
        {
            ThreadAssist.ProxyToMain (UpdateSourceInformation);
        }

        private void UpdateSourceInformation ()
        {
            if (status_label == null) {
                return;
            }

            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source == null) {
                status_label.Text = String.Empty;
                return;
            }

            status_label.Text = source.GetStatusText ();

            // We need a bit longer delay between query character typed to search initiated
            // when the library is sufficiently big; see bgo #540835
            bool long_delay = source.FilteredCount > 6000 || (source.Parent ?? source).Count > 12000;
            view_container.SearchEntry.ChangeTimeoutMs = long_delay ? (uint)250 : (uint)25;
        }

#endregion

#region Configuration Schemas

        public static readonly SchemaEntry<int> SourceViewWidth = new SchemaEntry<int> (
            "player_window", "source_view_width",
            175,
            "Source View Width",
            "Width of Source View Column."
        );

        public static readonly SchemaEntry<bool> ShowCoverArt = new SchemaEntry<bool> (
            "player_window", "show_cover_art",
            false,
            "Show cover art",
            "Show cover art below source view if available"
        );

        private static readonly SchemaEntry<bool> ShowSeekSliderResizer = new SchemaEntry<bool> (
            "player_window", "show_seek_slider_resizer",
            true, "Show seek slider resize grip", ""
        );

        private static readonly SchemaEntry<int> SeekSliderWidth = new SchemaEntry<int> (
            "player_window", "seek_slider_width",
            175, "Width of seek slider in px", ""
        );

        private static readonly SchemaEntry<int> SearchEntryWidth = new SchemaEntry<int> (
            "player_window", "search_entry_width",
            200, "Width of search entry element in px", ""
        );

#endregion

        IDBusExportable IDBusExportable.Parent {
            get { return null; }
        }

        string IDBusObjectName.ExportObjectName {
            get { return "ClientWindow"; }
        }

        string IService.ServiceName {
            get { return "NereidPlayerInterface"; }
        }
    }
}
