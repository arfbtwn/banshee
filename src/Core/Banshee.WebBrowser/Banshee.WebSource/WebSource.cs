//
// WebSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Mono.Unix;

using Gtk;

using Hyena;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Gui;
using Banshee.Configuration;
using Banshee.WebBrowser;

namespace Banshee.WebSource
{
    public abstract class WebSource : Source
    {
        private WebSourceContents source_contents;
        private WebView view;
        private BansheeActionGroup actions;

        public WebSource (string name, int order, string id) : base (name, name, order, id)
        {
            TypeUniqueId = id;
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);

            actions = new BansheeActionGroup (id);
            actions.Add (
                new ActionEntry ("ZoomIn"  + id, Stock.ZoomIn,  null, "<control>plus", null, (o, a) => view.ZoomIn ()),
                new ActionEntry ("ZoomOut" + id, Stock.ZoomOut, null, "<control>minus", null, (o, a) => view.ZoomOut ()),
                new ActionEntry ("Zoom100" + id, Stock.Zoom100, null, "<control>0", null, (o, a) => view.Zoom = 1f)
            );

            Properties.Set<string> ("ActiveSourceUIString", String.Format (@"
                <ui>
                  <menubar name=""MainMenu"" action=""MainMenuAction"">
                    <menu name=""ViewMenu"" action=""ViewMenuAction"">
                      <placeholder name=""ViewMenuAdditions"">
                        <separator/>
                        <menuitem action=""ZoomIn{0}""/>
                        <menuitem action=""ZoomOut{0}""/>
                        <menuitem action=""Zoom100{0}""/>
                        <separator/>
                      </placeholder>
                    </menu>
                  </menubar>
                </ui>", TypeUniqueId
            ));

            Properties.Set<BansheeActionGroup> ("ActiveSourceActions", actions);
        }

        public override void Activate ()
        {
            if (source_contents == null) {
                var shell = GetWidget ();

                // float isn't supported by GSettings apparently
                var zoom_conf = CreateSchema<double> ("webview_zoom", 1f, null, null);
                shell.View.Zoom = (float)zoom_conf.Get ();
                shell.View.ZoomChanged += z => zoom_conf.Set (z);
                view = shell.View;

                Properties.Set<ISourceContents> ("Nereid.SourceContents",
                    source_contents = new WebSourceContents (this, shell));
                Properties.Set<Banshee.Widgets.SearchEntry> ("Nereid.SearchEntry", shell.SearchEntry);

                // Add additional menu item keybindings
                var item = actions.ActionManager.UIManager.GetWidget ("/MainMenu/ViewMenu/ViewMenuAdditions/ZoomIn" + TypeUniqueId);
                item.AddAccelerator ("activate", actions.ActionManager.UIManager.AccelGroup,
                    (uint) Gdk.Key.KP_Add, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
                item.AddAccelerator ("activate", actions.ActionManager.UIManager.AccelGroup,
                    (uint) Gdk.Key.equal, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);

                item = actions.ActionManager.UIManager.GetWidget ("/MainMenu/ViewMenu/ViewMenuAdditions/ZoomOut" + TypeUniqueId);
                item.AddAccelerator ("activate", actions.ActionManager.UIManager.AccelGroup,
                    (uint) Gdk.Key.KP_Subtract, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
                item.AddAccelerator ("activate", actions.ActionManager.UIManager.AccelGroup,
                    (uint) Gdk.Key.underscore, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
            }

            base.Activate ();
        }

        protected abstract WebBrowserShell GetWidget ();

        public override int Count {
            get { return 0; }
        }

        private class WebSourceContents : ISourceContents
        {
            private WebSource source;
            private Widget widget;

            public WebSourceContents (WebSource source, Widget widget)
            {
                this.source = source;
                this.widget = widget;
            }

            public bool SetSource (ISource source)
            {
                return true;
            }

            public void ResetSource ()
            {
            }

            public Widget Widget {
                get { return widget; }
            }

            public ISource Source {
                get { return source; }
            }
        }
    }
}
