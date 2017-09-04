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
using Mono.Unix;
using Hyena.Gui;
using Hyena.Widgets;

namespace Banshee.WebBrowser
{
    public class NavigationControl : HBox
    {
        private Button back_button = new Button (new Image (Stock.GoBack, IconSize.Button)) { Relief = ReliefStyle.None };
        private Button forward_button = new Button (new Image (Stock.GoForward, IconSize.Button)) { Relief = ReliefStyle.None };
        private Button reload_button = new Button (new Image (Stock.Refresh, IconSize.Button)) { Relief = ReliefStyle.None };
        private Button home_button = new Button (new Image (Stock.Home, IconSize.Button)) { Relief = ReliefStyle.None };
        private Menu shortcut_menu = new Menu ();
        private MenuButton shortcut_menu_button;

        public event EventHandler GoHomeEvent;

        public NavigationControl ()
        {
            back_button.Clicked += (o, e) => {
                if (web_view != null && web_view.CanGoBack) {
                    web_view.GoBack ();
                }
            };

            forward_button.Clicked += (o, e) => {
                if (web_view != null && web_view.CanGoForward) {
                    web_view.GoForward ();
                }
            };

            reload_button.Clicked += (o, e) => {
                if (web_view != null) {
                    web_view.Reload (!GtkUtilities.NoImportantModifiersAreSet ());
                }
            };

            home_button.Clicked += (o, e) => {
                var handler = GoHomeEvent;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            };

            shortcut_menu_button = new MenuButton (home_button, shortcut_menu, true);

            UpdateNavigation ();

            PackStart (back_button, false, false, 0);
            PackStart (forward_button, false, false, 0);
            PackStart (reload_button, false, false, 5);
            PackStart (shortcut_menu_button, false, false, 0);

            ShowAll ();
            ClearLinks ();
        }

        public void ClearLinks ()
        {
            while (shortcut_menu.Children.Length > 0) {
                shortcut_menu.Remove (shortcut_menu.Children[0]);
            }

            shortcut_menu_button.ArrowVisible = false;
        }

        public MenuItem AddLink (string name, string url)
        {
            var link = new MenuItem (name) { Visible = true };

            if (url != null) {
                link.Activated += (o, a) => WebView.LoadUri (url);
            }

            shortcut_menu.Append (link);
            shortcut_menu_button.ArrowVisible = true;
            return link;
        }

        private OssiferWebView web_view;
        public OssiferWebView WebView {
            get { return web_view; }
            set {
                if (web_view == value) {
                    return;
                } else if (web_view != null) {
                    web_view.LoadStatusChanged -= OnOssiferWebViewLoadStatusChanged;
                }

                web_view = value;

                if (web_view != null) {
                    web_view.LoadStatusChanged += OnOssiferWebViewLoadStatusChanged;
                }

                UpdateNavigation ();
            }
        }

        public void UpdateNavigation ()
        {
            if (web_view != null) {
                back_button.Sensitive = web_view.CanGoBack;
                forward_button.Sensitive = web_view.CanGoForward;
                home_button.Sensitive = true;
                reload_button.Sensitive = true;
            } else {
                back_button.Sensitive = false;
                forward_button.Sensitive = false;
                home_button.Sensitive = false;
                reload_button.Sensitive = false;
            }
        }

        private void OnOssiferWebViewLoadStatusChanged (object o, EventArgs args)
        {
            if (web_view.LoadStatus == OssiferLoadStatus.Committed ||
                web_view.LoadStatus == OssiferLoadStatus.Failed) {
                UpdateNavigation ();
            }

            if (web_view.LoadStatus == OssiferLoadStatus.Committed &&
                web_view.Uri.StartsWith ("https", StringComparison.InvariantCultureIgnoreCase) &&
                web_view.SecurityLevel != OssiferSecurityLevel.Secure) {
                string message = Catalog.GetString (
                    "This page is blocked because it is probably not the one you are looking for!");
                // Translators: {0} is the URL of the web page that was requested
                string details = String.Format (Catalog.GetString ("The security certificate for {0} is invalid."),
                                                web_view.Uri);
                web_view.LoadHtml ($"{message}<br>{details}", null);
            }
        }
    }
}
