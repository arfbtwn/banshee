//
// SearchEntry.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright 2007 Novell, Inc.
// Copyright 2011 Bertrand Lorentz
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
using Gtk;

namespace Banshee.Widgets
{
    public class SearchEntry : Entry
    {
        private Menu menu;
        private int active_filter_id = -1;

        private uint changed_timeout_id = 0;

        private string empty_message;
        private bool ready = false;

        private event EventHandler filter_changed;
        private event EventHandler entry_changed;

        public new event EventHandler Changed {
            add { entry_changed += value; }
            remove { entry_changed -= value; }
        }

        public event EventHandler FilterChanged {
            add { filter_changed += value; }
            remove { filter_changed -= value; }
        }

        public uint ChangeTimeoutMs { get; set; }

        public bool ShowSearchIcon { get; set; }

        public Menu Menu {
            get { return menu; }
        }

        protected SearchEntry (IntPtr raw) : base (raw)
        {
        }

        public SearchEntry () : base ()
        {
            ChangeTimeoutMs = 25;

            menu = new Menu ();
        }

        private void ShowMenu (uint time)
        {
            if (menu.Children.Length > 0) {
                menu.ShowAll ();
                menu.Popup (null, null, null, 0, time);
            }
        }

        private void ShowHideButtons ()
        {
            if (ShowSearchIcon || (menu != null && menu.Children.Length > 0)) {
                SetIconFromIconName (EntryIconPosition.Primary, "edit-find");
                PrimaryIconSensitive = PrimaryIconActivatable = true;
            } else {
                SetIconFromIconName (EntryIconPosition.Primary, null);
            }
            if (Text.Length > 0) {
                SetIconFromIconName (EntryIconPosition.Secondary, "edit-clear");
                SecondaryIconSensitive = SecondaryIconActivatable = true;
            } else {
                SetIconFromIconName (EntryIconPosition.Secondary, null);
            }
        }

        private bool toggling = false;

        private void OnMenuItemActivated (object o, EventArgs args)
        {
            if (toggling || !(o is FilterMenuItem)) {
                return;
            }

            toggling = true;
            FilterMenuItem item = (FilterMenuItem)o;

            foreach (MenuItem child_item in menu) {
                if (!(child_item is FilterMenuItem)) {
                    continue;
                }

                FilterMenuItem filter_child = (FilterMenuItem)child_item;
                if (filter_child != item) {
                    filter_child.Active = false;
                }
            }

            item.Active = true;
            ActiveFilterID = item.ID;
            toggling = false;
        }

        protected override void OnChanged ()
        {
            ShowHideButtons ();

            if (changed_timeout_id > 0) {
                GLib.Source.Remove (changed_timeout_id);
            }

            if (Ready) {
                changed_timeout_id = GLib.Timeout.Add (ChangeTimeoutMs, OnChangedTimeout);
            }
        }

        private bool OnChangedTimeout ()
        {
            if (!Ready) {
                return false;
            }

            EventHandler handler = entry_changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }

            return false;
        }

        protected override void OnIconPress (EntryIconPosition icon_pos, Gdk.Event evnt)
        {
            var evnt_button = evnt as Gdk.EventButton;
            if (evnt_button == null) {
                return;
            }

            if (evnt_button.Button != 1) {
                return;
            }

            HasFocus = true;

            if (icon_pos == EntryIconPosition.Primary) {
                ShowMenu (evnt_button.Time);
            } else if (icon_pos == EntryIconPosition.Secondary) {
                active_filter_id = 0;
                Text = String.Empty;
            }
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            if (evnt.Key == Gdk.Key.Escape) {
                active_filter_id = 0;
                Text = String.Empty;
                return true;
            }
            return base.OnKeyPressEvent (evnt);
        }

        protected override void OnShown ()
        {
            base.OnShown ();
            ShowHideButtons ();
        }

        // TODO: GTK+ 3.2 adds a placeholder-text property, but for now
        // we have to handle it ourselves
        protected override bool OnDrawn (Cairo.Context cr)
        {
            if(Text.Length > 0 || HasFocus || EmptyMessage == null) {
                return base.OnDrawn (cr);
            }

            Layout.SetMarkup (EmptyMessage);

            Gdk.RGBA color;
            if (!StyleContext.LookupColor ("placeholder_text_color", out color)) {
                color = StyleContext.GetColor (StateFlags.Insensitive);
            }
            Pango.Attribute attr = new Pango.AttrForeground (Convert.ToUInt16 (color.Red * 65535),
                Convert.ToUInt16 (color.Green * 65535), Convert.ToUInt16 (color.Blue * 65535));
            Layout.Attributes.Insert (attr);

            return base.OnDrawn (cr);
        }

        protected virtual void OnFilterChanged ()
        {
            EventHandler handler = filter_changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }

            if (IsQueryAvailable) {
                OnChanged ();
            }
        }

        public void AddFilterOption (int id, string label)
        {
            if (id < 0) {
                throw new ArgumentException ("id", "must be >= 0");
            }

            FilterMenuItem item = new FilterMenuItem (id, label);
            item.Activated += OnMenuItemActivated;
            menu.Append (item);

            if (ActiveFilterID < 0) {
                item.Activate ();
            }

            SetIconSensitive (EntryIconPosition.Primary, true);
        }

        public void AddFilterSeparator ()
        {
            menu.Append (new SeparatorMenuItem ());
        }

        public void RemoveFilterOption (int id)
        {
            FilterMenuItem item = FindFilterMenuItem (id);
            if (item != null) {
                menu.Remove (item);
            }
        }

        public void ActivateFilter (int id)
        {
            FilterMenuItem item = FindFilterMenuItem (id);
            if (item != null) {
                item.Activate ();
            }
        }

        private FilterMenuItem FindFilterMenuItem (int id)
        {
            foreach (MenuItem item in menu) {
                if (item is FilterMenuItem && ((FilterMenuItem)item).ID == id) {
                    return (FilterMenuItem)item;
                }
            }

            return null;
        }

        public string GetLabelForFilterID (int id)
        {
            FilterMenuItem item = FindFilterMenuItem (id);
            if (item == null) {
                return null;
            }

            return item.Label;
        }

        public void CancelSearch ()
        {
            Text = String.Empty;
            ActivateFilter (0);
        }

        public int ActiveFilterID {
            get { return active_filter_id; }
            private set {
                if (value == active_filter_id) {
                    return;
                }

                active_filter_id = value;
                OnFilterChanged ();
            }
        }

        public string EmptyMessage {
            get {
                return Sensitive ? empty_message : String.Empty;
            }
            set {
                empty_message = value;
                QueueDraw ();
            }
        }

        public string Query {
            get { return Text.Trim (); }
            set { Text = String.IsNullOrEmpty (value) ? String.Empty : value.Trim (); }
        }

        public bool IsQueryAvailable {
            get { return Query != null && Query != String.Empty; }
        }

        public bool Ready {
            get { return ready; }
            set { ready = value; }
        }

        protected override void OnStateChanged (Gtk.StateType previous_state)
        {
            base.OnStateChanged (previous_state);

            Sensitive = State != StateType.Insensitive;
        }

        private class FilterMenuItem : CheckMenuItem
        {
            private int id;

            public FilterMenuItem (int id, string label) : base(label)
            {
                this.id = id;
                DrawAsRadio = true;
            }

            protected FilterMenuItem (IntPtr ptr) : base (ptr)
            {
            }

            public int ID {
                get { return id; }
            }
        }
    }
}
