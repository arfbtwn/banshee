/*
 * Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using DBus;
using Gdk;
using Gtk;
using Window = Gtk.Window;

namespace Notifications
{
    public enum Urgency : byte
    {
        Low = 0,
        Normal,
        Critical
    }

    public class ActionArgs : EventArgs
    {
        public ActionArgs (string action)
        {
            Action = action;
        }

        public string Action { get; }
    }

    public class CloseArgs : EventArgs
    {
        public CloseArgs (CloseReason reason)
        {
            Reason = reason;
        }

        public CloseReason Reason { get; }
    }

    public delegate void ActionHandler (object o, ActionArgs args);

    public delegate void CloseHandler (object o, CloseArgs args);

    public class Notification
    {
        private readonly IDictionary<string, ActionTuple> action_map = new Dictionary<string, ActionTuple> ();

        private readonly string app_name;
        private Widget attach_widget;
        private readonly IDictionary<string, object> hints = new Dictionary<string, object> ();
        private string icon = string.Empty;

        private readonly INotifications nf;
        private bool shown;
        private StatusIcon status_icon;
        private string summary = string.Empty, body = string.Empty;
        private int timeout = -1;

        private bool updates_pending;

        static Notification ()
        {
            BusG.Init ();
        }

        public Notification ()
        {
            nf = Global.DBusObject;

            nf.NotificationClosed += OnClosed;
            nf.ActionInvoked += OnActionInvoked;

            var app_asm = Assembly.GetEntryAssembly () ?? Assembly.GetCallingAssembly ();

            app_name = app_asm.GetName ().Name;
        }

        public Notification (string summary, string body) : this ()
        {
            this.summary = summary;
            this.body = body;
        }

        public Notification (string summary, string body, string icon) : this (summary, body)
        {
            this.icon = icon;
        }

        public Notification (string summary, string body, Pixbuf icon) : this (summary, body)
        {
            SetPixbufHint (icon);
        }

        public Notification (string summary, string body, Pixbuf icon, Widget widget) : this (summary, body, icon)
        {
            AttachToWidget (widget);
        }

        public Notification (string summary, string body, string icon, Widget widget) : this (summary, body, icon)
        {
            AttachToWidget (widget);
        }

        public Notification (string summary, string body, Pixbuf icon, StatusIcon status_icon) : this (summary, body,
            icon)
        {
            AttachToStatusIcon (status_icon);
        }

        public Notification (string summary, string body, string icon, StatusIcon status_icon) : this (summary, body,
            icon)
        {
            AttachToStatusIcon (status_icon);
        }


        public string Summary {
            set {
                summary = value;
                Update ();
            }
            get => summary;
        }

        public string Body {
            set {
                body = value;
                Update ();
            }
            get => body;
        }

        public int Timeout {
            set {
                timeout = value;
                Update ();
            }
            get => timeout;
        }

        public Urgency Urgency {
            set {
                hints["urgency"] = (byte) value;
                Update ();
            }
            get => hints.ContainsKey ("urgency") ? (Urgency) hints["urgency"] : Urgency.Normal;
        }

        public string Category {
            set {
                hints["category"] = value;
                Update ();
            }
            get => hints.ContainsKey ("category") ? (string) hints["category"] : string.Empty;
        }

        public Pixbuf Icon {
            set {
                SetPixbufHint (value);
                icon = string.Empty;
                Update ();
            }
        }

        public string IconName {
            set {
                icon = value;
                hints.Remove ("icon_data");
                Update ();
            }
        }

        public uint Id { get; private set; }

        public Widget AttachWidget {
            get => attach_widget;
            set => AttachToWidget (value);
        }

        public StatusIcon StatusIcon {
            get => status_icon;
            set => AttachToStatusIcon (value);
        }

        public event EventHandler Closed;

        private void SetPixbufHint (Pixbuf pixbuf)
        {
            var icon_data = new IconData
            {
                Width = pixbuf.Width,
                Height = pixbuf.Height,
                Rowstride = pixbuf.Rowstride,
                HasAlpha = pixbuf.HasAlpha,
                BitsPerSample = pixbuf.BitsPerSample,
                NChannels = pixbuf.NChannels
            };

            var len = (icon_data.Height - 1) * icon_data.Rowstride + icon_data.Width *
                      ((icon_data.NChannels * icon_data.BitsPerSample + 7) / 8);
            icon_data.Pixels = new byte[len];
            Marshal.Copy (pixbuf.Pixels, icon_data.Pixels, 0, len);

            hints["icon_data"] = icon_data;
        }

        public void AttachToWidget (Widget widget)
        {
            if (widget == null) throw new ArgumentNullException (nameof(widget));

            int x, y;

            widget.Window.GetOrigin (out x, out y);

            if (widget.GetType () != typeof(Window) || !widget.GetType ().IsSubclassOf (typeof(Window))) {
                x += widget.Allocation.X;
                y += widget.Allocation.Y;
            }

            x += widget.Allocation.Width / 2;
            y += widget.Allocation.Height / 2;

            SetGeometryHints (widget.Screen, x, y);
            attach_widget = widget;
            status_icon = null;
        }

        public void AttachToStatusIcon (StatusIcon status_icon)
        {
            Screen screen;
            Rectangle rect;
            Orientation orientation;
            int x, y;

            if (!status_icon.GetGeometry (out screen, out rect, out orientation)) return;

            x = rect.X + rect.Width / 2;
            y = rect.Y + rect.Height / 2;

            SetGeometryHints (screen, x, y);

            this.status_icon = status_icon;
            attach_widget = null;
        }

        public void SetGeometryHints (Screen screen, int x, int y)
        {
            hints["x"] = x;
            hints["y"] = y;
            hints["xdisplay"] = screen.MakeDisplayName ();
            Update ();
        }

        private void Update ()
        {
            if (shown && !updates_pending) {
                updates_pending = true;
                GLib.Timeout.Add (100, delegate
                {
                    if (updates_pending) {
                        Show ();
                        updates_pending = false;
                    }
                    return false;
                });
            }
        }

        public void Show ()
        {
            string[] actions;
            lock (action_map) {
                actions = new string[action_map.Keys.Count * 2];
                var i = 0;
                foreach (var pair in action_map) {
                    actions[i++] = pair.Key;
                    actions[i++] = pair.Value.Label;
                }
            }
            Id = nf.Notify (app_name, Id, icon, summary, body, actions, hints, timeout);
            shown = true;
        }

        public void Close ()
        {
            nf.CloseNotification (Id);
            Id = 0;
            shown = false;
        }

        private void OnClosed (uint id, uint reason)
        {
            if (Id == id) {
                Id = 0;
                shown = false;
                Closed?.Invoke (this, new CloseArgs ((CloseReason) reason));
            }
        }

        public void AddAction (string action, string label, ActionHandler handler)
        {
            if (Global.Capabilities != null &&
                Array.IndexOf (Global.Capabilities, "actions") > -1) {
                lock (action_map) {
                    action_map[action] = new ActionTuple (label, handler);
                }
                Update ();
            }
        }

        public void RemoveAction (string action)
        {
            lock (action_map) {
                action_map.Remove (action);
            }
            Update ();
        }

        public void ClearActions ()
        {
            lock (action_map) {
                action_map.Clear ();
            }
            Update ();
        }

        private void OnActionInvoked (uint id, string action)
        {
            lock (action_map) {
                if (Id == id && action_map.ContainsKey (action))
                    action_map[action].Handler (this, new ActionArgs (action));
            }
        }

        public void AddHint (string name, object value)
        {
            hints[name] = value;
            Update ();
        }

        public void RemoveHint (string name)
        {
            hints.Remove (name);
            Update ();
        }

        private struct IconData
        {
            public int Width;
            public int Height;
            public int Rowstride;
            public bool HasAlpha;
            public int BitsPerSample;
            public int NChannels;
            public byte[] Pixels;
        }

        private struct ActionTuple
        {
            public readonly string Label;
            public readonly ActionHandler Handler;

            public ActionTuple (string label, ActionHandler handler)
            {
                Label = label;
                Handler = handler;
            }
        }
    }
}
