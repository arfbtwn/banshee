// 
// NavigationControl.cs
// 
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
// 
// Copyright 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;
using Hyena.Gui;
using Hyena.Widgets;
using Mono.Unix;

namespace Banshee.WebBrowser
{
    public class NavigationControl : HBox
    {
        private readonly Button back_button =
            new Button (new Image (Stock.GoBack, IconSize.Button)) {Relief = ReliefStyle.None};

        private readonly Button forward_button =
            new Button (new Image (Stock.GoForward, IconSize.Button)) {Relief = ReliefStyle.None};

        private readonly Button home_button =
            new Button (new Image (Stock.Home, IconSize.Button)) {Relief = ReliefStyle.None};

        private readonly Button reload_button =
            new Button (new Image (Stock.Refresh, IconSize.Button)) {Relief = ReliefStyle.None};

        private readonly Menu shortcut_menu = new Menu ();
        private readonly MenuButton shortcut_menu_button;

        private readonly Button zoom_100_button =
            new Button (new Image (Stock.Zoom100, IconSize.Button)) {Relief = ReliefStyle.None};

        private readonly Button zoom_in_button =
            new Button (new Image (Stock.ZoomIn, IconSize.Button)) {Relief = ReliefStyle.None};

        private readonly Button zoom_out_button =
            new Button (new Image (Stock.ZoomOut, IconSize.Button)) {Relief = ReliefStyle.None};

        private OssiferWebView web_view;

        public NavigationControl ()
        {
            back_button.Clicked += (o, e) => {
                if (web_view != null && web_view.CanGoBack) web_view.GoBack ();
            };

            forward_button.Clicked += (o, e) => {
                if (web_view != null && web_view.CanGoForward) web_view.GoForward ();
            };

            reload_button.Clicked += (o, e) => web_view?.Reload (!GtkUtilities.NoImportantModifiersAreSet ());

            home_button.Clicked += (o, e) => {
                var handler = GoHomeEvent;
                handler?.Invoke (this, EventArgs.Empty);
            };

            zoom_in_button.Clicked += (o, e) => web_view?.ZoomIn ();

            zoom_out_button.Clicked += (o, e) => web_view?.ZoomOut ();

            zoom_100_button.Clicked += (o, e) => {
                if (web_view != null)
                    web_view.Zoom = 1f;
            };

            shortcut_menu_button = new MenuButton (home_button, shortcut_menu, true);

            UpdateNavigation ();

            PackStart (back_button, false, false, 0);
            PackStart (forward_button, false, false, 0);
            PackStart (reload_button, false, false, 5);
            PackStart (shortcut_menu_button, false, false, 0);
            PackStart (zoom_in_button, false, false, 0);
            PackStart (zoom_out_button, false, false, 0);
            PackStart (zoom_100_button, false, false, 0);

            ShowAll ();
            ClearLinks ();
        }

        public OssiferWebView WebView {
            private get => web_view;
            set {
                if (web_view == value) return;
                if (web_view != null) web_view.LoadStatusChanged -= OnOssiferWebViewLoadStatusChanged;

                web_view = value;

                if (web_view != null) web_view.LoadStatusChanged += OnOssiferWebViewLoadStatusChanged;

                UpdateNavigation ();
            }
        }

        public event EventHandler GoHomeEvent;

        public void ClearLinks ()
        {
            while (shortcut_menu.Children.Length > 0) shortcut_menu.Remove (shortcut_menu.Children[0]);

            shortcut_menu_button.ArrowVisible = false;
        }

        public MenuItem AddLink (string name, string url)
        {
            var link = new MenuItem (name) {Visible = true};

            if (url != null) link.Activated += (o, a) => WebView.LoadUri (url);

            shortcut_menu.Append (link);
            shortcut_menu_button.ArrowVisible = true;
            return link;
        }

        private void UpdateNavigation ()
        {
            if (web_view != null) {
                back_button.Sensitive = web_view.CanGoBack;
                forward_button.Sensitive = web_view.CanGoForward;
                home_button.Sensitive = true;
                reload_button.Sensitive = true;
            }
            else {
                back_button.Sensitive = false;
                forward_button.Sensitive = false;
                home_button.Sensitive = false;
                reload_button.Sensitive = false;
            }
        }

        private void OnOssiferWebViewLoadStatusChanged (object o, EventArgs args)
        {
            if (web_view.LoadStatus == OssiferLoadStatus.Committed ||
                web_view.LoadStatus == OssiferLoadStatus.Failed) UpdateNavigation ();

            if (web_view.LoadStatus == OssiferLoadStatus.Committed &&
                web_view.Uri.StartsWith ("https", StringComparison.InvariantCultureIgnoreCase) &&
                web_view.SecurityLevel != OssiferSecurityLevel.Secure) {
                var message = Catalog.GetString (
                    "This page is blocked because it is probably not the one you are looking for!");
                // Translators: {0} is the URL of the web page that was requested
                var details = string.Format (Catalog.GetString ("The security certificate for {0} is invalid."),
                    web_view.Uri);
                web_view.LoadString ($"{message}<br>{details}", "text/html", "UTF-8", null);
            }
        }
    }
}
