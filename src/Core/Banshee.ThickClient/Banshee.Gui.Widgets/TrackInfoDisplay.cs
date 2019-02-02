//
// TrackInfoDisplay.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Larry Ewing <lewing@novell.com> (Is my hero)
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
using Cairo;

using Hyena;
using Hyena.Gui;
using Hyena.Gui.Theatrics;

using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Gui.Widgets
{
    public abstract class TrackInfoDisplay : Widget
    {
#pragma warning disable 660, 661
        class ImageInfo : IDisposable
        {
            public static implicit operator TrackInfo    (ImageInfo data) => data?._track;

            public static implicit operator ImageSurface (ImageInfo data) => data?.LoadImage ();

            readonly TrackInfoDisplay _display;
            readonly TrackInfo        _track;

            ImageSurface _image;

            public ImageInfo (TrackInfoDisplay display, TrackInfo track)
            {
                _display = display;
                _track   = track;
            }

            ImageSurface LoadImage ()
            {
                if (null == _track) {
                    return null;
                }

                if (null == _image) {
                    var id   = _track.ArtworkId;
                    var size = _display.ArtworkSizeRequest;

                    _image = _display.ArtworkManager.LookupScaleSurface (id, size);
                }

                if (null != _image) {
                    return _image;
                } else {
                    var attr = _track.MediaAttributes;

                    return _display.MissingImage (attr.HasFlag (TrackMediaAttributes.VideoStream));
                }
            }

            public void Dispose ()
            {
                using (_image) { }
            }
        }
#pragma warning restore 660, 661

        private ArtworkManager artwork_manager;
        protected ArtworkManager ArtworkManager {
            get { return artwork_manager; }
        }

        private ImageInfo current_image;
        protected ImageSurface CurrentImage {
            get { return current_image; }
        }

        private ImageInfo incoming_image;
        protected ImageSurface IncomingImage {
            get { return incoming_image; }
        }

        protected string MissingAudioIconName { get; set; }
        protected string MissingVideoIconName { get; set; }

        private ImageSurface missing_audio_image;
        protected ImageSurface MissingAudioImage {
            get { return missing_audio_image ?? (missing_audio_image
                = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (MissingIconSizeRequest, MissingAudioIconName), true)); }
        }

        private ImageSurface missing_video_image;
        protected ImageSurface MissingVideoImage {
            get { return missing_video_image ?? (missing_video_image
                = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (MissingIconSizeRequest, MissingVideoIconName), true)); }
        }

        private Cairo.Color? background_color;
        protected virtual Cairo.Color BackgroundColor {
            get {
                if (!background_color.HasValue) {
                    background_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetBackgroundColor (StateFlags.Normal));
                }
                return background_color.Value;
            }
        }

        private Cairo.Color? text_color;
        protected virtual Cairo.Color TextColor {
            get {
                if (!text_color.HasValue) {
                    text_color = CairoExtensions.GdkRGBAToCairoColor (StyleContext.GetColor (StateFlags.Normal));
                }
                return text_color.Value;
            }
        }

        private Cairo.Color? text_light_color;
        protected virtual Cairo.Color TextLightColor {
            get {
                if (!text_light_color.HasValue) {
                    text_light_color = Hyena.Gui.Theming.GtkTheme.GetCairoTextMidColor (Parent);
                }
                return text_light_color.Value;
            }
        }

        private   TrackInfo current_track  => current_image;
        protected TrackInfo CurrentTrack   => current_track;

        private   TrackInfo incoming_track => incoming_image;
        protected TrackInfo IncomingTrack  => incoming_track;

        private uint idle_timeout_id = 0;
        private SingleActorStage stage = new SingleActorStage ();

        protected TrackInfoDisplay (IntPtr native) : base (native)
        {
        }

        public TrackInfoDisplay ()
        {
            MissingAudioIconName = "audio-x-generic";
            MissingVideoIconName = "video-x-generic";
            stage.Iteration += OnStageIteration;

            artwork_manager = ServiceManager.Get<ArtworkManager> ();

            Connected = true;
            HasWindow = false;
        }

        private bool connected;
        public bool Connected {
            get { return connected; }
            set {
                if (value == connected)
                    return;

                if (ServiceManager.PlayerEngine != null) {
                    connected = value;
                    if (value) {
                        var current = ServiceManager.PlayerEngine.CurrentState;
                        ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                            PlayerEvent.StartOfStream |
                            PlayerEvent.TrackInfoUpdated |
                            PlayerEvent.StateChange);
                    } else {
                        ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
                    }
                }
            }
        }

        public static Widget GetEditable (TrackInfoDisplay display)
        {
            return CoverArtEditor.For (display,
                (x, y) => display.IsWithinCoverart (x, y),
                () => display.CurrentTrack,
                () => {}
            );
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (idle_timeout_id > 0) {
                    GLib.Source.Remove (idle_timeout_id);
                }

                Connected = false;

                stage.Iteration -= OnStageIteration;
                stage = null;

                InvalidateCache ();
            }

            base.Dispose (disposing);
        }

        protected override void OnRealized ()
        {
            Window = Parent.Window;
            base.OnRealized ();
        }

        protected override void OnUnrealized ()
        {
            base.OnUnrealized ();
            InvalidateCache ();
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            var before = ArtworkSizeRequest;

            base.OnSizeAllocated (allocation);

            var after = ArtworkSizeRequest;

            if (before == after) {
                return;
            }

            ResetMissingImages ();

            using (current_image) { }
            current_image = null;

            LoadCurrentTrack (true);
        }

        protected override void OnStyleUpdated ()
        {
            base.OnStyleUpdated ();

            // these will be refreshed to new values, on demand, via their properties
            text_color = null;
            background_color = null;
            text_light_color = null;

            ResetMissingImages ();

            OnThemeChanged ();
        }

        private void ResetMissingImages ()
        {
            using (missing_audio_image) { }
            missing_audio_image = null;

            using (missing_video_image) { }
            missing_video_image = null;
        }

        protected virtual void OnThemeChanged ()
        {
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            bool idle = incoming_track == null && current_track == null;
            if (!Visible || !IsMapped || (idle && !CanRenderIdle)) {
                return false;
            }

            if (idle) {
                RenderIdle (cr);
            } else {
                RenderAnimation (cr);
            }

            return true;
        }

        protected virtual bool CanRenderIdle {
            get { return false; }
        }

        protected virtual void RenderIdle (Cairo.Context cr)
        {
        }

        private void RenderAnimation (Cairo.Context cr)
        {
            if (stage.Actor == null) {
                // We are not in a transition, just render
                RenderStage (cr, current_track, current_image);
                return;
            }

            if (current_track == null) {
                // Fade in the whole stage, nothing to fade out
                cr.PushGroup ();
                RenderStage (cr, incoming_track, incoming_image);
                cr.PopGroupToSource ();

                cr.PaintWithAlpha (stage.Actor.Percent);
                return;
            }

            // Draw the old cover art more and more translucent
            cr.PushGroup ();
            RenderCoverArt (cr, current_image);
            cr.PopGroupToSource ();
            cr.PaintWithAlpha (1.0 - stage.Actor.Percent);

            // Draw the new cover art more and more opaque
            cr.PushGroup ();
            RenderCoverArt (cr, incoming_image);
            cr.PopGroupToSource ();
            cr.PaintWithAlpha (stage.Actor.Percent);

            bool same_artist_album = incoming_track != null ? incoming_track.ArtistAlbumEqual (current_track) : false;
            bool same_track = incoming_track != null ? incoming_track.Equals (current_track) : false;

            if (same_artist_album) {
                RenderTrackInfo (cr, incoming_track, same_track, true);
            }

            // Don't xfade the text since it'll look bad (overlapping words); instead, fade
            // the old out, and then the new in
            if (stage.Actor.Percent <= 0.5) {
                // Fade out old text
                cr.PushGroup ();
                RenderTrackInfo (cr, current_track, !same_track, !same_artist_album);
                cr.PopGroupToSource ();

                cr.PaintWithAlpha (1.0 - (stage.Actor.Percent * 2.0));
            } else {
                // Fade in new text
                cr.PushGroup ();
                RenderTrackInfo (cr, incoming_track, !same_track, !same_artist_album);
                cr.PopGroupToSource ();

                cr.PaintWithAlpha ((stage.Actor.Percent - 0.5) * 2.0);
            }
        }

        private void RenderStage (Cairo.Context cr, TrackInfo track, ImageSurface image)
        {
            RenderCoverArt (cr, image);
            RenderTrackInfo (cr, track, true, true);
        }

        protected virtual void RenderCoverArt (Cairo.Context cr, ImageSurface image)
        {
            var missing = IsMissingImage (image);

            ArtworkRenderer.RenderThumbnail (cr, image,
                dispose:     false,
                x:           0,
                y:           ArtworkOffset,
                width:       ArtworkSizeRequest,
                height:      ArtworkSizeRequest,
                drawBorder: !missing,
                radius:      0.0,
                fill:        missing,
                fillColor:   BackgroundColor);
        }

        protected virtual bool IsWithinCoverart (int x, int y)
        {
            return x >= 0 &&
                y >= ArtworkOffset &&
                x <= ArtworkSizeRequest &&
                y <= (ArtworkOffset + ArtworkSizeRequest);
        }

        protected bool IsMissingImage (ImageSurface pb)
        {
            return pb == null || pb == MissingAudioImage || pb == MissingVideoImage;
        }

        protected virtual void InvalidateCache ()
        {
        }

        protected abstract void RenderTrackInfo (Cairo.Context cr, TrackInfo track, bool renderTrack, bool renderArtistAlbum);

        private int ArtworkOffset {
            get { return (Allocation.Height - ArtworkSizeRequest) / 2; }
        }

        protected virtual int ArtworkSizeRequest {
            get { return Allocation.Height; }
        }

        protected virtual int MissingIconSizeRequest {
            get { return ArtworkSizeRequest; }
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            if (args.Event == PlayerEvent.StartOfStream) {
                LoadCurrentTrack ();
            } else if (args.Event == PlayerEvent.TrackInfoUpdated) {
                LoadCurrentTrack (true);
            } else if (args.Event == PlayerEvent.StateChange) {
                PlayerEventStateChangeArgs state = (PlayerEventStateChangeArgs)args;
                if (state.Current == PlayerState.Idle) {
                    if (idle_timeout_id == 0) {
                        idle_timeout_id = GLib.Timeout.Add (100, IdleTimeout);
                    }
                }
            }
        }

        private bool IdleTimeout ()
        {
            if (ServiceManager.PlayerEngine == null ||
                ServiceManager.PlayerEngine.CurrentTrack == null ||
                ServiceManager.PlayerEngine.CurrentState == PlayerState.Idle) {

                using (current_image)
                using (incoming_image)
                {
                }

                current_image = incoming_image = null;

                if (stage != null && stage.Actor == null) {
                    stage.Reset ();
                }
            }

            idle_timeout_id = 0;
            return false;
        }

        private void LoadCurrentTrack ()
        {
            LoadCurrentTrack (false);
        }

        private void LoadCurrentTrack (bool force_reload)
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;

            if (force_reload || track != incoming_track) {
                using (incoming_image) { }

                incoming_image = new ImageInfo (this, track);
            }

            if (stage.Actor == null) {
                stage.Reset ();
            }
        }

        private ImageSurface MissingImage (bool is_video)
        {
            return is_video ? MissingVideoImage : MissingAudioImage;
        }

        private double last_fps = 0.0;

        private void OnStageIteration (object o, EventArgs args)
        {
            Invalidate ();

            if (stage.Actor != null) {
                last_fps = stage.Actor.FramesPerSecond;
                return;
            }

            InvalidateCache ();

            if (ApplicationContext.Debugging) {
                Log.DebugFormat ("TrackInfoDisplay RenderAnimation: {0:0.00} FPS", last_fps);
            }

            using (current_image) { }
            current_image = incoming_image;
            incoming_image = null;

            OnArtworkChanged ();
        }

        protected virtual void Invalidate ()
        {
            QueueDraw ();
        }

        protected virtual void OnArtworkChanged ()
        {
        }

        protected virtual string GetFirstLineText (TrackInfo track)
        {
            return WrapWithDefaultTextColor (String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (track.DisplayTrackTitle)));
        }

        protected virtual string GetSecondLineText (TrackInfo track)
        {
            string markup = null;
            Banshee.Streaming.RadioTrackInfo radio_track = track as Banshee.Streaming.RadioTrackInfo;

            if ((track.MediaAttributes & TrackMediaAttributes.Podcast) != 0) {
                // Translators: {0} and {1} are for markup so ignore them, {2} and {3}
                // are Podcast Name and Published Date, respectively;
                // e.g. 'from BBtv published 7/26/2007'
                markup = MarkupFormat (Catalog.GetString ("{0}from{1} {2} {0}published{1} {3}"),
                    track.DisplayAlbumTitle, track.ReleaseDate.ToShortDateString ());
            } else if (radio_track != null && radio_track.ParentTrack != null) {
                // This is complicated because some radio streams send tags when the song changes, and we
                // want to display them if they do.  But if they don't, we want it to look good too, so we just
                // display the station name for the second line.
                string by_from = GetByFrom (
                    track.ArtistName == radio_track.ParentTrack.ArtistName ? null : track.ArtistName, track.DisplayArtistName,
                    track.AlbumTitle == radio_track.ParentTrack.AlbumTitle ? null : track.AlbumTitle, track.DisplayAlbumTitle, false
                );

                if (String.IsNullOrEmpty (by_from)) {
                    // simply: "Chicago Public Radio" or whatever the artist name is
                    markup = GLib.Markup.EscapeText (radio_track.ParentTrack.ArtistName ?? Banshee.Streaming.RadioTrackInfo.UnknownStream);
                } else {
                    // Translators: {0} and {1} are markup so ignore them, {2} is the name of the radio station
                    string on = MarkupFormat (Catalog.GetString ("{0}on{1} {2}"), radio_track.ParentTrack.TrackTitle);

                    // Translators: {0} is the "from {album} by {artist}" type string, and {1} is the "on {radio station name}" string
                    markup = String.Format (Catalog.GetString ("{0} {1}"), by_from, on);
                }
            } else {
                markup = GetByFrom (track.ArtistName, track.DisplayArtistName, track.AlbumTitle, track.DisplayAlbumTitle, true);
            }

            return WrapWithDefaultTextColor (markup);
        }

        private string WrapWithDefaultTextColor (string markup)
        {
            return String.Format ("<span color=\"{0}\">{1}</span>",
                CairoExtensions.ColorGetHex (TextColor, false),
                markup);
        }

        private string MarkupFormat (string fmt, params string [] args)
        {
            string [] new_args = new string [args.Length + 2];
            new_args[0] = String.Format ("<span color=\"{0}\" size=\"small\">",
                CairoExtensions.ColorGetHex (TextLightColor, false));
            new_args[1] = "</span>";

            for (int i = 0; i < args.Length; i++) {
                new_args[i + 2] = GLib.Markup.EscapeText (args[i]);
            }

            return String.Format (fmt, new_args);
        }

        private string GetByFrom (string artist, string display_artist, string album, string display_album, bool unknown_ok)
        {
            bool has_artist = !String.IsNullOrEmpty (artist);
            bool has_album = !String.IsNullOrEmpty (album);

            string markup = null;
            if (has_artist && has_album) {
                // Translators: {0} and {1} are for markup so ignore them, {2} and {3}
                // are Artist Name and Album Title, respectively;
                // e.g. 'by Parkway Drive from Killing with a Smile'
                markup = MarkupFormat (Catalog.GetString ("{0}by{1} {2} {0}from{1} {3}"), display_artist, display_album);
            } else if (has_album) {
                // Translators: {0} and {1} are for markup so ignore them, {2} is for Album Title;
                // e.g. 'from Killing with a Smile'
                markup = MarkupFormat (Catalog.GetString ("{0}from{1} {2}"), display_album);
            } else if (has_artist || unknown_ok) {
                // Translators: {0} and {1} are for markup so ignore them, {2} is for Artist Name;
                // e.g. 'by Parkway Drive'
                markup = MarkupFormat (Catalog.GetString ("{0}by{1} {2}"), display_artist);
            }
            return markup;
        }
    }
}
