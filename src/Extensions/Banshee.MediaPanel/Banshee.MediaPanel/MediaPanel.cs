//
// MediaPanel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009-2010 Novell, Inc.
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

using Banshee.Base;
using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.MediaPanel
{
    public class MediaPanel : IDisposable
    {
        class Window : Gtk.Window
        {
            readonly GtkElementsService _elements;

            public Window (IntPtr ptr) : base(ptr) { }

            public Window () : base("Bashee Media Panel")
            {
                _elements = ServiceManager.Get<GtkElementsService>();

                WindowPosition = WindowPosition.Center;
                DefaultWidth   = 1000;
                DefaultHeight  = 500;
            }

            protected override bool OnDeleteEvent(Gdk.Event evnt)
            {
                if (_elements.PrimaryWindowClose?.Invoke() ?? false) {
                    return true;
                }

                ServiceStack.Application.Shutdown ();
                return true;
            }
        }

        public static MediaPanel Instance { get; private set; }

        private Window window_panel;

        public MediaPanelContents Contents { get; private set; }

        public MediaPanel ()
        {
            if (Instance != null) {
                throw new InvalidOperationException ("Only one MediaPanel instance should exist");
            }

            Instance = this;

            window_panel = new Window ();
        }

        public void Dispose ()
        {
        }

        public void BuildContents ()
        {
            var timer = Log.DebugTimerStart ();
            Contents = new MediaPanelContents ();
            Contents.ShowAll ();
            Log.DebugTimerPrint (timer, "Media panel contents created: {0}");

            if (window_panel != null) {
                window_panel.Add (Contents);
                window_panel.Present ();
            }
        }

        public void Show ()
        {
            if (window_panel != null) {
                window_panel.Show ();
            }
        }

        public void Hide ()
        {
            if (window_panel != null) {
                window_panel.Hide ();
            }
        }
    }
}
