//
// StreamPositionLabel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

namespace Banshee.Widgets
{
    public enum StreamLabelState {
        Idle,
        Contacting,
        Loading,
        Buffering,
        Playing
    }

    public class StreamPositionLabel : Alignment
    {
        private double buffering_progress;
        private bool is_live;
        private SeekSlider seekRange;
        private string format_string = "<small>{0}</small>";
        private Pango.Layout layout;
        private StreamLabelState state;

        public StreamPositionLabel (SeekSlider seekRange) : base (0.0f, 0.0f, 1.0f, 1.0f)
        {
            AppPaintable = true;

            this.seekRange = seekRange;
            this.seekRange.ValueChanged += OnSliderUpdated;
        }

        protected StreamPositionLabel (IntPtr raw) : base (raw)
        {
        }

        protected override void OnRealized ()
        {
            base.OnRealized ();
            BuildLayouts ();
            UpdateLabel ();
        }

        private void BuildLayouts ()
        {
            if (layout != null) {
                layout.Dispose ();
            }

            last_text = null;
            layout = new Pango.Layout (PangoContext);
            layout.FontDescription = PangoContext.FontDescription;
            layout.Ellipsize = Pango.EllipsizeMode.None;
        }

        protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
        {
            base.OnGetPreferredHeight (out minimum_height, out natural_height);
            minimum_height = natural_height = 0;
            if (!IsRealized || layout == null) {
                return;
            }

            int width, height;
            layout.GetPixelSize (out width, out height);
            minimum_height = natural_height = height;
        }

        protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
        {
            base.OnGetPreferredWidth (out minimum_width, out natural_width);
            minimum_width = natural_width = 0;
            if (!IsRealized || layout == null) {
                return;
            }

            int width, height;
            layout.GetPixelSize (out width, out height);
            minimum_width = natural_width = width;
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            int bar_width = (int)((double)Allocation.Width * buffering_progress);
            bool render_bar = false;

            var border = StyleContext.GetBorder (StateFlags);
            if (bar_width > 0 && IsBuffering) {
                bar_width -= border.Left + border.Right;
                render_bar = true;

                StyleContext.Save ();
                StyleContext.AddClass ("trough");
                StyleContext.RenderBackground (cr, 0, 0, Allocation.Width, Allocation.Height);
                StyleContext.RenderFrame (cr, 0, 0, Allocation.Width, Allocation.Height);
                StyleContext.Restore ();

                if (bar_width > 0) {
                    StyleContext.Save ();
                    StyleContext.AddClass ("progressbar");
                    StyleContext.RenderActivity (cr,
                        border.Left, border.Top,
                        bar_width, Allocation.Height - (border.Top + border.Bottom));
                    StyleContext.Restore ();
                }
            }

            int width, height;
            layout.GetPixelSize (out width, out height);

            int x = (Allocation.Width - width) / 2;
            int y = (Allocation.Height - height) / 2;
            Gdk.Rectangle rect = new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height);

            if (render_bar) {
                // First render the text that is on the progressbar
                int full_bar_width = bar_width + border.Left;
                rect.Width = full_bar_width;

                cr.Save ();
                Gdk.CairoHelper.Rectangle (cr, rect);
                cr.Clip ();
                StyleContext.Save ();
                // The entry class is need so that the text get the "selected" color
                StyleContext.AddClass ("entry");
                StyleContext.AddClass ("progressbar");
                StyleContext.RenderLayout (cr, x, y, layout);
                StyleContext.Restore ();
                cr.Restore ();

                // Adjust the rectangle so that the rest of the text is rendered as expected
                rect.X += rect.Width;
                rect.Width = Allocation.Width - rect.Width;
            }

            cr.Save ();
            Gdk.CairoHelper.Rectangle (cr, rect);
            cr.Clip ();
            StyleContext.Save ();
            StyleContext.AddClass ("trough");
            StyleContext.RenderLayout (cr, x, y, layout);
            StyleContext.Restore ();
            cr.Restore ();

            return true;
        }

        private static string idle = Catalog.GetString ("Idle");
        private static string contacting = Catalog.GetString ("Contacting...");
        private static string loading = Catalog.GetString ("Loading...");

        private void UpdateLabel ()
        {
            if (!IsRealized || layout == null) {
                return;
            }

            if (IsBuffering) {
                double progress = buffering_progress * 100.0;
                UpdateLabel (String.Format ("{0}: {1}%", Catalog.GetString("Buffering"), progress.ToString ("0.0")));
            } else if (IsContacting) {
                UpdateLabel (contacting);
            } else if (IsLoading) {
                UpdateLabel (loading);
            } else if (IsIdle) {
                UpdateLabel (idle);
            } else if (seekRange.Duration == Int64.MaxValue) {
                UpdateLabel (FormatDuration ((long)seekRange.Value));
            } else if (seekRange.Value == 0 && seekRange.Duration == 0) {
                // nop
            } else {
                UpdateLabel (String.Format (Catalog.GetString ("{0} of {1}"),
                    FormatDuration ((long)seekRange.Value), FormatDuration ((long)seekRange.Adjustment.Upper)));
            }
        }

        private string last_text;
        private void UpdateLabel (string text)
        {
            if (!IsRealized || layout == null || text == last_text) {
                return;
            }

            last_text = text;
            layout.SetMarkup (String.Format (format_string, GLib.Markup.EscapeText (text)));
            QueueResize ();
        }

        private static string FormatDuration (long time)
        {
            time /= 1000;
            return (time > 3600 ?
                    String.Format ("{0}:{1:00}:{2:00}", time / 3600, (time / 60) % 60, time % 60) :
                    String.Format ("{0}:{1:00}", time / 60, time % 60));
        }

        private void OnSliderUpdated (object o, EventArgs args)
        {
            UpdateLabel ();
        }

        public double BufferingProgress {
            get { return buffering_progress; }
            set {
                buffering_progress = Math.Max (0.0, Math.Min (1.0, value));
                UpdateLabel ();
            }
        }

        public bool IsIdle {
            get { return StreamState == StreamLabelState.Idle; }
        }

        public bool IsBuffering {
            get { return StreamState == StreamLabelState.Buffering; }
        }

        public bool IsContacting {
            get { return StreamState == StreamLabelState.Contacting; }
        }

        public bool IsLoading {
            get { return StreamState == StreamLabelState.Loading; }
        }

        public StreamLabelState StreamState {
            get { return state; }
            set {
                if (state != value) {
                    state = value;
                    UpdateLabel ();
                }
            }
        }

        public bool IsLive {
            get { return is_live; }
            set {
                if (is_live != value) {
                    is_live = value;
                    UpdateLabel ();
                }
            }
        }

        public string FormatString {
            set {
                format_string = value;
                BuildLayouts ();
                UpdateLabel ();
            }
        }
    }
}
