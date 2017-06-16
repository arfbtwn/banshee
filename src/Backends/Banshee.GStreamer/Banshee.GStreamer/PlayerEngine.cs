//
// PlayerEngine.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using Hyena;
using Hyena.Data;

using Banshee.Base;
using Banshee.Streaming;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Preferences;
using Banshee.Collection;

namespace Banshee.GStreamer
{
    internal enum GstState
    {
        VoidPending = 0,
        Null = 1,
        Ready = 2,
        Paused = 3,
        Playing = 4
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerEosCallback (IntPtr player);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerErrorCallback (IntPtr player, uint domain, int code, IntPtr error, IntPtr debug);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerStateChangedCallback (IntPtr player, GstState old_state, GstState new_state, GstState pending_state);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerBufferingCallback (IntPtr player, int buffering_progress);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerVisDataCallback (IntPtr player, int channels, int samples, IntPtr data, int bands, IntPtr spectrum);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerNextTrackStartingCallback (IntPtr player);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerAboutToFinishCallback (IntPtr player);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr VideoPipelineSetupHandler (IntPtr player, IntPtr bus);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void VideoPrepareWindowHandler (IntPtr player);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BansheePlayerVolumeChangedCallback (IntPtr player, double newVolume);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void GstTaggerTagFoundCallback (IntPtr player, string tagName, ref GLib.Value value);

    public class PlayerEngine : Banshee.MediaEngine.PlayerEngine,
        IEqualizer, IVisualizationDataSource, ISupportClutter
    {
        internal const string LibBansheeLibrary = "libbanshee.dll";

        private uint GST_CORE_ERROR = 0;
        private uint GST_LIBRARY_ERROR = 0;
        private uint GST_RESOURCE_ERROR = 0;
        private uint GST_STREAM_ERROR = 0;

        private HandleRef handle;
        private bool is_initialized;

        private BansheePlayerEosCallback eos_callback;
        private BansheePlayerErrorCallback error_callback;
        private BansheePlayerStateChangedCallback state_changed_callback;
        private BansheePlayerBufferingCallback buffering_callback;
        private BansheePlayerVisDataCallback vis_data_callback;
        private VideoPipelineSetupHandler video_pipeline_setup_callback;
        private VideoPrepareWindowHandler video_prepare_window_callback;
        private GstTaggerTagFoundCallback tag_found_callback;
        private BansheePlayerNextTrackStartingCallback next_track_starting_callback;
        private BansheePlayerAboutToFinishCallback about_to_finish_callback;
        private BansheePlayerVolumeChangedCallback volume_changed_callback;

        private bool next_track_pending;
        private SafeUri pending_uri;
        private bool pending_maybe_video;

        private bool buffering_finished;
        private bool xid_is_set = false;
        private uint iterate_timeout_id = 0;

        private bool gapless_enabled;
        private EventWaitHandle next_track_set;

        private event VisualizationDataHandler data_available = null;
        public event VisualizationDataHandler DataAvailable {
            add {
                if (value == null) {
                    return;
                } else if (data_available == null) {
                    bp_set_vis_data_callback (handle, vis_data_callback);
                }

                data_available += value;
            }

            remove {
                if (value == null) {
                    return;
                }

                data_available -= value;

                if (data_available == null) {
                    bp_set_vis_data_callback (handle, null);
                }
            }
        }

        protected override bool DelayedInitialize {
            get { return true; }
        }


        public PlayerEngine ()
        {
            IntPtr ptr = bp_new ();

            if (ptr == IntPtr.Zero) {
                throw new ApplicationException (Catalog.GetString ("Could not initialize GStreamer library"));
            }

            handle = new HandleRef (this, ptr);

            bp_get_error_quarks (out GST_CORE_ERROR, out GST_LIBRARY_ERROR,
                out GST_RESOURCE_ERROR, out GST_STREAM_ERROR);

            eos_callback = new BansheePlayerEosCallback (OnEos);
            error_callback = new BansheePlayerErrorCallback (OnError);
            state_changed_callback = new BansheePlayerStateChangedCallback (OnStateChange);
            buffering_callback = new BansheePlayerBufferingCallback (OnBuffering);
            vis_data_callback = new BansheePlayerVisDataCallback (OnVisualizationData);
            video_pipeline_setup_callback = new VideoPipelineSetupHandler (OnVideoPipelineSetup);
            video_prepare_window_callback = new VideoPrepareWindowHandler (OnVideoPrepareWindow);
            tag_found_callback = new GstTaggerTagFoundCallback (OnTagFound);
            next_track_starting_callback = new BansheePlayerNextTrackStartingCallback (OnNextTrackStarting);
            about_to_finish_callback = new BansheePlayerAboutToFinishCallback (OnAboutToFinish);
            volume_changed_callback = new BansheePlayerVolumeChangedCallback (OnVolumeChanged);
            bp_set_eos_callback (handle, eos_callback);
            bp_set_error_callback (handle, error_callback);
            bp_set_state_changed_callback (handle, state_changed_callback);
            bp_set_buffering_callback (handle, buffering_callback);
            bp_set_tag_found_callback (handle, tag_found_callback);
            bp_set_next_track_starting_callback (handle, next_track_starting_callback);
            bp_set_video_pipeline_setup_callback (handle, video_pipeline_setup_callback);
            bp_set_video_prepare_window_callback (handle, video_prepare_window_callback);
            bp_set_volume_changed_callback (handle, volume_changed_callback);

            next_track_set = new EventWaitHandle (false, EventResetMode.ManualReset);
        }

        protected override void Initialize ()
        {
            if (ServiceManager.Get<Banshee.GStreamer.Service> () == null) {
                var service = new Banshee.GStreamer.Service ();
                ((IExtensionService)service).Initialize ();
            }

            if (!bp_initialize_pipeline (handle)) {
                bp_destroy (handle);
                handle = new HandleRef (this, IntPtr.Zero);
                throw new ApplicationException (Catalog.GetString ("Could not initialize GStreamer library"));
            }

            OnStateChanged (PlayerState.Ready);

            InstallPreferences ();
            ReplayGainEnabled = ReplayGainEnabledSchema.Get ();
            GaplessEnabled = GaplessEnabledSchema.Get ();
            Log.InformationFormat ("GStreamer version {0}, gapless: {1}, replaygain: {2}", gstreamer_version_string (), GaplessEnabled, ReplayGainEnabled);

            is_initialized = true;

            if (!bp_audiosink_has_volume (handle)) {
                Volume = (ushort)PlayerEngineService.VolumeSchema.Get ();
            }
        }

        public override void Dispose ()
        {
            bp_stop (handle, true);
            UninstallPreferences ();
            base.Dispose ();
            bp_destroy (handle);
            gstreamer_deinitialize ();
            handle = new HandleRef (this, IntPtr.Zero);
            is_initialized = false;
        }

        public override void Close (bool fullShutdown)
        {
            bp_stop (handle, fullShutdown);
            base.Close (fullShutdown);
        }

        protected override void OpenUri (SafeUri uri, bool maybeVideo)
        {
            // The GStreamer engine can use the XID of the main window if it ever
            // needs to bring up the plugin installer so it can be transient to
            // the main window.
            if (!xid_is_set) {
                var service = ServiceManager.Get ("GtkElementsService") as IPropertyStoreExpose;
                if (service != null) {
                    bp_set_application_gdk_window (handle, service.PropertyStore.Get<IntPtr> ("PrimaryWindow.RawHandle"));
                }
                xid_is_set = true;
            }

            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup (uri.AbsoluteUri);
            try {
                if (!bp_open (handle, uri_ptr, maybeVideo)) {
                    throw new ApplicationException ("Could not open resource");
                }
            } finally {
                GLib.Marshaller.Free (uri_ptr);
            }
        }

        public override void Play ()
        {
            bp_play (handle);
        }

        public override void Pause ()
        {
            bp_pause (handle);
        }

        public override void SetNextTrackUri (SafeUri uri, bool maybeVideo)
        {
            next_track_pending = false;
            if (next_track_set.WaitOne (0, false)) {
                // We're not waiting for the next track to be set.
                // This means that we've missed the window for gapless.
                // Save this URI to be played when we receive EOS.
                pending_uri = uri;
                pending_maybe_video = maybeVideo;
                return;
            }
            // If there isn't a next track for us, release the block on the about-to-finish callback.
            if (uri == null) {
                next_track_set.Set ();
                return;
            }
            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup (uri.AbsoluteUri);
            try {
                bp_set_next_track (handle, uri_ptr, maybeVideo);
            } finally {
                GLib.Marshaller.Free (uri_ptr);
                next_track_set.Set ();
            }
        }

        public override void Seek (uint position, bool accurate_seek)
        {
            bp_set_position (handle, (ulong)position, accurate_seek);
            OnEventChanged (PlayerEvent.Seek);
        }

        public override void VideoExpose (IntPtr window, bool direct)
        {
            bp_video_window_expose (handle, window, direct);
        }

        public override void VideoWindowRealize (IntPtr window)
        {
            bp_video_window_realize (handle, window);
        }

        public override IntPtr [] GetBaseElements ()
        {
            IntPtr [] elements = new IntPtr[3];

            if (bp_get_pipeline_elements (handle, out elements[0], out elements[1], out elements[2])) {
                return elements;
            }

            return null;
        }

        public override void NotifyMouseMove (double x, double y)
        {
            bp_dvd_mouse_move_notify (handle, x, y);
        }

        public override void NotifyMouseButtonPressed (int button, double x, double y)
        {
            bp_dvd_mouse_button_pressed_notify (handle, button, x, y);
        }

        public override void NotifyMouseButtonReleased (int button, double x, double y)
        {
            bp_dvd_mouse_button_released_notify (handle, button, x, y);
        }

        public override void NavigateToLeftMenu ()
        {
            bp_dvd_left_notify (handle);
        }

        public override void NavigateToRightMenu ()
        {
            bp_dvd_right_notify (handle);
        }

        public override void NavigateToUpMenu ()
        {
            bp_dvd_up_notify (handle);
        }

        public override void NavigateToDownMenu ()
        {
            bp_dvd_down_notify (handle);
        }

        public override void NavigateToMenu ()
        {
            bp_dvd_go_to_menu (handle);
        }

        public override void ActivateCurrentMenu ()
        {
            bp_dvd_activate_notify (handle);
        }

        public override void GoToNextChapter ()
        {
            bp_dvd_go_to_next_chapter (handle);
        }

        public override void GoToPreviousChapter ()
        {
            bp_dvd_go_to_previous_chapter (handle);
        }

        private void OnEos (IntPtr player)
        {
            StopIterating ();
            Close (false);
            OnEventChanged (PlayerEvent.EndOfStream);
            if (!next_track_pending &&
                (!GaplessEnabled || (CurrentTrack != null && CurrentTrack.HasAttribute (TrackMediaAttributes.VideoStream)))) {
                // We don't request next track in OnEoS if gapless playback is enabled and current track has no video stream contained.
                // The request next track is already called in OnAboutToFinish().
                OnEventChanged (PlayerEvent.RequestNextTrack);
            } else if (pending_uri != null) {
                Log.Warning ("[Gapless] EOS signalled while waiting for next track.  This means that Banshee " +
                    "was too slow at calculating what track to play next.  " +
                    "If this happens frequently, please file a bug");
                OnStateChanged (PlayerState.Loading);
                OpenUri (pending_uri, pending_maybe_video);
                Play ();
                pending_uri = null;
            } else if (!GaplessEnabled || (CurrentTrack != null && CurrentTrack.HasAttribute (TrackMediaAttributes.VideoStream))) {
                // This should be unreachable - the RequestNextTrack event is delegated to the main thread
                // and so blocks the bus callback from delivering the EOS message.
                //
                // Playback should continue as normal from here, when the RequestNextTrack message gets handled.
                Log.Warning ("[Gapless] EndOfStream message received before the next track has been set.  " +
                    "If this happens frequently, please file a bug");
            } else {
                Log.Debug ("[Gapless] Reach the last music under repeat off mode");
            }
        }

        private bool OnIterate ()
        {
            // Actual iteration.
            OnEventChanged (PlayerEvent.Iterate);
            // Run forever until we are stopped
            return true;
        }

        private void StartIterating ()
        {
            if (iterate_timeout_id > 0) {
                GLib.Source.Remove (iterate_timeout_id);
                iterate_timeout_id = 0;
            }

            iterate_timeout_id = GLib.Timeout.Add (200, OnIterate);
        }

        private void StopIterating ()
        {
            if (iterate_timeout_id > 0) {
                GLib.Source.Remove (iterate_timeout_id);
                iterate_timeout_id = 0;
            }
        }

        private void OnNextTrackStarting (IntPtr player)
        {
            if (GaplessEnabled) {
                // Must do it here because the next track is already playing.

                // If the state is anything other than loaded, assume we were just playing a track and should
                // EoS it and increment its playcount etc.
                if (CurrentState != PlayerState.Loaded && CurrentState != PlayerState.Loading) {
                    ServiceManager.PlayerEngine.IncrementLastPlayed (1.0);
                    OnEventChanged (PlayerEvent.EndOfStream);
                    OnEventChanged (PlayerEvent.StartOfStream);
                }
            }
        }

        private System.DateTime about_to_finish_time_stamp;

        private void OnAboutToFinish (IntPtr player)
        {
            // HACK: ugly workaround for GStreamer's bug http://bugzilla.gnome.org/722769
            // long story short, AboutToFinish signal firing twice for the same play of the same track
            // causes problems when Gapless Enabled because of RequestNextTrack event being fired twice
            if (about_to_finish_time_stamp == CurrentTrackTimeStamp) {
                return;
            }
            about_to_finish_time_stamp = CurrentTrackTimeStamp;


            // This is needed to make Shuffle-by-* work.
            // Shuffle-by-* uses the LastPlayed field to determine what track in the grouping to play next.
            // Therefore, we need to update this before requesting the next track.
            //
            // This will be overridden by IncrementLastPlayed () called by
            // PlaybackControllerService's EndOfStream handler.
            CurrentTrack.UpdateLastPlayed ();

            next_track_set.Reset ();
            pending_uri = null;
            next_track_pending = true;

            OnEventChanged (PlayerEvent.RequestNextTrack);
            // Gapless playback with Playbin2 requires that the about-to-finish callback does not return until
            // the next uri has been set.  Block here for a second or until the RequestNextTrack event has
            // finished triggering.
            if (!next_track_set.WaitOne (1000, false)) {
                Log.Debug ("[Gapless] Timed out while waiting for next_track_set to be raised");
                next_track_set.Set ();
            }
        }

        private void OnStateChange (IntPtr player, GstState old_state, GstState new_state, GstState pending_state)
        {
            // Start by clearing any timeout we might have
            StopIterating ();
            if (CurrentState != PlayerState.Loaded && old_state == GstState.Ready && new_state == GstState.Paused && pending_state == GstState.Playing) {
                if (ready_timeout != 0) {
                    Application.IdleTimeoutRemove (ready_timeout);
                    ready_timeout = 0;
                }
                OnStateChanged (PlayerState.Loaded);
                return;
            } else if (old_state == GstState.Paused && new_state == GstState.Playing && pending_state == GstState.VoidPending) {
                if (CurrentState == PlayerState.Loaded) {
                    OnEventChanged (PlayerEvent.StartOfStream);
                }
                OnStateChanged (PlayerState.Playing);
                // Start iterating only when going to playing
                StartIterating ();
                return;
            } else if (CurrentState == PlayerState.Playing && old_state == GstState.Playing && new_state == GstState.Paused) {
                OnStateChanged (PlayerState.Paused);
                return;
            } else if (new_state == GstState.Ready && pending_state == GstState.Playing) {
                if (ready_timeout == 0) {
                    ready_timeout = Application.RunTimeout (1000, OnReadyTimeout);
                }
                return;
            }
        }

        private uint ready_timeout;
        private bool OnReadyTimeout ()
        {
            ready_timeout = 0;
            var uri = CurrentUri;
            if (CurrentState == PlayerState.Loading && Position == 0 && uri != null && uri.IsLocalPath) {
                // This is a dirty workaround.  I was seeing the playback get stuck on track transition,
                // about one in 20 songs, where it would load the new track's duration, but be stuck at 0:00,
                // but if I moved the seek slider it would unstick it, hence setting Position...
                Log.WarningFormat ("Seem to be stuck loading {0}, so re-trying", uri);
                Position = 0;
            }

            return false;
        }

        private void OnError (IntPtr player, uint domain, int code, IntPtr error, IntPtr debug)
        {
            TrackInfo failed_track = CurrentTrack;
            Close (true);

            string error_message = error == IntPtr.Zero
                ? Catalog.GetString ("Unknown Error")
                : GLib.Marshaller.Utf8PtrToString (error);

            if (domain == GST_RESOURCE_ERROR) {
                GstResourceError domain_code = (GstResourceError) code;
                if (failed_track != null) {
                    switch (domain_code) {
                        case GstResourceError.NotFound:
                            failed_track.SavePlaybackError (StreamPlaybackError.ResourceNotFound);
                            break;
                        default:
                            break;
                    }
                }

                Log.Error (String.Format ("GStreamer resource error: {0}", domain_code), false);
            } else if (domain == GST_STREAM_ERROR) {
                GstStreamError domain_code = (GstStreamError) code;
                if (failed_track != null) {
                    switch (domain_code) {
                        case GstStreamError.CodecNotFound:
                            failed_track.SavePlaybackError (StreamPlaybackError.CodecNotFound);
                            break;
                        default:
                            break;
                    }
                }

                Log.Error (String.Format("GStreamer stream error: {0}", domain_code), false);
            } else if (domain == GST_CORE_ERROR) {
                GstCoreError domain_code = (GstCoreError) code;
                if (failed_track != null) {
                    switch (domain_code) {
                        case GstCoreError.MissingPlugin:
                            failed_track.SavePlaybackError (StreamPlaybackError.CodecNotFound);
                            break;
                        default:
                            break;
                    }
                }

                if (domain_code != GstCoreError.MissingPlugin) {
                    Log.Error (String.Format("GStreamer core error: {0}", (GstCoreError) code), false);
                }
            } else if (domain == GST_LIBRARY_ERROR) {
                Log.Error (String.Format("GStreamer library error: {0}", (GstLibraryError) code), false);
            }

            OnEventChanged (new PlayerEventErrorArgs (error_message));
        }

        private void OnBuffering (IntPtr player, int progress)
        {
            if (buffering_finished && progress >= 100) {
                return;
            }

            buffering_finished = progress >= 100;
            OnEventChanged (new PlayerEventBufferingArgs ((double) progress / 100.0));
        }

        private void OnTagFound (IntPtr player, string tagName, ref GLib.Value value)
        {
            OnTagFound (ProcessNativeTagResult (tagName, ref value));
        }

        private void OnVisualizationData (IntPtr player, int channels, int samples, IntPtr data, int bands, IntPtr spectrum)
        {
            VisualizationDataHandler handler = data_available;

            if (handler == null) {
                return;
            }

            float [] flat = new float[channels * samples];
            Marshal.Copy (data, flat, 0, flat.Length);

            float [][] cbd = new float[channels][];
            for (int i = 0; i < channels; i++) {
                float [] channel = new float[samples];
                Array.Copy (flat, i * samples, channel, 0, samples);
                cbd[i] = channel;
            }

            float [] spec = new float[bands];
            Marshal.Copy (spectrum, spec, 0, bands);

            try {
                handler (cbd, new float[][] { spec });
            } catch (Exception e) {
                Log.Error ("Uncaught exception during visualization data post.", e);
            }
        }

        private void OnVolumeChanged (IntPtr player, double newVolume)
        {
            OnEventChanged (PlayerEvent.Volume);
        }

        private static StreamTag ProcessNativeTagResult (string tagName, ref GLib.Value valueRaw)
        {
            if (tagName == String.Empty || tagName == null) {
                return StreamTag.Zero;
            }

            object value = null;

            try {
                value = valueRaw.Val;
            } catch {
                return StreamTag.Zero;
            }

            if (value == null) {
                return StreamTag.Zero;
            }

            StreamTag item;
            item.Name = tagName;
            item.Value = value;

            return item;
        }

        public override ushort Volume {
            get {
                return is_initialized
                    ? (ushort)Math.Round (bp_get_volume (handle) * 100.0)
                    : (ushort)0;
            }
            set {
                if (!is_initialized) {
                    return;
                }

                bp_set_volume (handle, value / 100.0);
                if (!bp_audiosink_has_volume (handle)) {
                    PlayerEngineService.VolumeSchema.Set ((int)value);
                }

                OnEventChanged (PlayerEvent.Volume);
            }
        }

        public override uint Position {
            get { return (uint)bp_get_position(handle); }
            set { Seek (value); }
        }

        public override bool CanSeek {
            get { return bp_can_seek (handle); }
        }

        public override uint Length {
            get { return (uint)bp_get_duration (handle); }
        }

        public override string Id {
            get { return "gstreamer"; }
        }

        public override string Name {
            get { return "GStreamer 0.10"; }
        }

        private bool? supports_equalizer = null;
        public override bool SupportsEqualizer {
            get {
                if (supports_equalizer == null) {
                    supports_equalizer = bp_equalizer_is_supported (handle);
                }

                return supports_equalizer.Value;
            }
        }

        public override VideoDisplayContextType VideoDisplayContextType {
            get { return bp_video_get_display_context_type (handle); }
        }

        public override IntPtr VideoDisplayContext {
            set { bp_video_set_display_context (handle, value); }
            get { return bp_video_get_display_context (handle); }
        }

        public double AmplifierLevel {
            set {
                double scale = Math.Pow (10.0, value / 20.0);
                bp_equalizer_set_preamp_level (handle, scale);
            }
        }

        public int [] BandRange {
            get {
                int min = -1;
                int max = -1;

                bp_equalizer_get_bandrange (handle, out min, out max);

                return new int [] { min, max };
            }
        }

        public uint [] EqualizerFrequencies {
            get {
                uint count = bp_equalizer_get_nbands (handle);
                double [] freq = new double[count];

                bp_equalizer_get_frequencies (handle, out freq);

                uint [] ret = new uint[count];
                for (int i = 0; i < count; i++) {
                    ret[i] = (uint)freq[i];
                }

                return ret;
            }
        }

        public void SetEqualizerGain (uint band, double gain)
        {
            bp_equalizer_set_gain (handle, band, gain);
        }

        private static string [] source_capabilities = { "file", "http", "cdda", "dvd", "vcd" };
        public override IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }

        private static string [] decoder_capabilities = { "ogg", "wma", "asf", "flac" };
        public override IEnumerable ExplicitDecoderCapabilities {
            get { return decoder_capabilities; }
        }

        private bool ReplayGainEnabled {
            get { return bp_replaygain_get_enabled (handle); }
            set { bp_replaygain_set_enabled (handle, value); }
        }

        private bool GaplessEnabled {
            get { return gapless_enabled; }
            set
            {
                gapless_enabled = value;
                if (value) {
                    bp_set_about_to_finish_callback (handle, about_to_finish_callback);
                } else {
                    bp_set_about_to_finish_callback (handle, null);
                }
            }
        }

        public override int SubtitleCount {
            get { return bp_get_subtitle_count (handle); }
        }

        public override int SubtitleIndex {
            set { bp_set_subtitle (handle, value); }
        }

        public override SafeUri SubtitleUri {
            set {
                if (value != null) {
                    IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup (value.AbsoluteUri);
                    try {
                        bp_set_subtitle_uri (handle, uri_ptr);
                    } finally {
                        GLib.Marshaller.Free (uri_ptr);
                    }
                }
            }
            get {
                IntPtr uri_ptr = IntPtr.Zero;
                try {
                    uri_ptr = bp_get_subtitle_uri (handle);
                    string uri = GLib.Marshaller.Utf8PtrToString (uri_ptr);
                    return new SafeUri(uri);
                } finally {
                    GLib.Marshaller.Free (uri_ptr);
                }
            }
        }

        public override string GetSubtitleDescription (int index)
        {
            IntPtr desc_ptr = IntPtr.Zero;
            try {
                desc_ptr = bp_get_subtitle_description (handle, index);
                return GLib.Marshaller.Utf8PtrToString (desc_ptr);
            } finally {
                GLib.Marshaller.Free (desc_ptr);
            }
        }

        public override bool InDvdMenu {
            get { return bp_dvd_is_menu (handle); }
        }

#region ISupportClutter

        private IntPtr clutter_video_sink;
        private IntPtr clutter_video_texture;
        private bool clutter_video_sink_enabled;

        public void EnableClutterVideoSink (IntPtr videoTexture)
        {
            clutter_video_sink_enabled = true;
            clutter_video_texture = videoTexture;
        }

        public void DisableClutterVideoSink ()
        {
            clutter_video_sink_enabled = false;
            clutter_video_texture = IntPtr.Zero;
        }

        public bool IsClutterVideoSinkInitialized {
            get { return
                clutter_video_sink_enabled &&
                clutter_video_texture != IntPtr.Zero &&
                clutter_video_sink != IntPtr.Zero;
            }
        }


        private IntPtr OnVideoPipelineSetup (IntPtr player, IntPtr bus)
        {
            try {
                if (clutter_video_sink_enabled) {
                    if (clutter_video_sink != IntPtr.Zero) {
                        // FIXME: does this get unreffed by the pipeline?
                    }

                    clutter_video_sink = clutter_gst_video_sink_new (clutter_video_texture);
                } else if (!clutter_video_sink_enabled && clutter_video_sink != IntPtr.Zero) {
                    clutter_video_sink = IntPtr.Zero;
                    clutter_video_texture = IntPtr.Zero;
                }
            } catch (Exception e) {
                Log.Error ("Clutter support could not be initialized", e);
                clutter_video_sink = IntPtr.Zero;
                clutter_video_texture = IntPtr.Zero;
                clutter_video_sink_enabled = false;
            }

            return clutter_video_sink;
        }

        private void OnVideoPrepareWindow (IntPtr player)
        {
            OnEventChanged (PlayerEvent.PrepareVideoWindow);
        }

#endregion

#region Preferences

        private PreferenceBase replaygain_preference;
        private PreferenceBase gapless_preference;

        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

            replaygain_preference = service["general"]["misc"].Add (new SchemaPreference<bool> (ReplayGainEnabledSchema,
                Catalog.GetString ("_Enable ReplayGain correction"),
                Catalog.GetString ("For tracks that have ReplayGain data, automatically scale (normalize) playback volume"),
                delegate { ReplayGainEnabled = ReplayGainEnabledSchema.Get (); }
            ));
            gapless_preference = service["general"]["misc"].Add (new SchemaPreference<bool> (GaplessEnabledSchema,
                    Catalog.GetString ("Enable _gapless playback"),
                    Catalog.GetString ("Eliminate the small playback gap on track change. Useful for concept albums and classical music"),
                    delegate { GaplessEnabled = GaplessEnabledSchema.Get (); }
            ));
        }

        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

            service["general"]["misc"].Remove (replaygain_preference);
            service["general"]["misc"].Remove (gapless_preference);
            replaygain_preference = null;
            gapless_preference = null;
        }

        public static readonly SchemaEntry<bool> ReplayGainEnabledSchema = new SchemaEntry<bool> (
            "player_engine", "replay_gain_enabled",
            false,
            "Enable ReplayGain",
            "If ReplayGain data is present on tracks when playing, allow volume scaling"
        );

        public static readonly SchemaEntry<bool> GaplessEnabledSchema = new SchemaEntry<bool> (
            "player_engine", "gapless_playback_enabled",
            true,
            "Enable gapless playback",
            "Eliminate the small playback gap on track change. Useful for concept albums and classical music"
        );


#endregion

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gstreamer_deinitialize ();

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr bp_new ();

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_initialize_pipeline (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_destroy (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_eos_callback (HandleRef player, BansheePlayerEosCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_error_callback (HandleRef player, BansheePlayerErrorCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_vis_data_callback (HandleRef player, BansheePlayerVisDataCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_state_changed_callback (HandleRef player,
            BansheePlayerStateChangedCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_buffering_callback (HandleRef player,
            BansheePlayerBufferingCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_video_pipeline_setup_callback (HandleRef player,
            VideoPipelineSetupHandler cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_tag_found_callback (HandleRef player,
            GstTaggerTagFoundCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_video_prepare_window_callback (HandleRef player,
           VideoPrepareWindowHandler cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_next_track_starting_callback (HandleRef player,
            BansheePlayerNextTrackStartingCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_about_to_finish_callback (HandleRef player,
            BansheePlayerAboutToFinishCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_open (HandleRef player, IntPtr uri, bool maybeVideo);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_stop (HandleRef player, bool nullstate);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_pause (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_play (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_set_next_track (HandleRef player, IntPtr uri, bool maybeVideo);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_volume (HandleRef player, double volume);

        [DllImport(PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern double bp_get_volume (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_volume_changed_callback (HandleRef player,
            BansheePlayerVolumeChangedCallback cb);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_can_seek (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_audiosink_has_volume (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_set_position (HandleRef player, ulong time_ms, bool accurate_seek);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong bp_get_position (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong bp_get_duration (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_get_pipeline_elements (HandleRef player, out IntPtr playbin,
            out IntPtr audiobin, out IntPtr audiotee);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_application_gdk_window (HandleRef player, IntPtr window);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern VideoDisplayContextType bp_video_get_display_context_type (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_video_set_display_context (HandleRef player, IntPtr displayContext);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr bp_video_get_display_context (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_video_window_expose (HandleRef player, IntPtr displayContext, bool direct);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_video_window_realize (HandleRef player, IntPtr window);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_get_error_quarks (out uint core, out uint library,
            out uint resource, out uint stream);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_equalizer_is_supported (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_equalizer_set_preamp_level (HandleRef player, double level);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_equalizer_set_gain (HandleRef player, uint bandnum, double gain);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_equalizer_get_bandrange (HandleRef player, out int min, out int max);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint bp_equalizer_get_nbands (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_equalizer_get_frequencies (HandleRef player,
            [MarshalAs (UnmanagedType.LPArray)] out double [] freq);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_replaygain_set_enabled (HandleRef player, bool enabled);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_replaygain_get_enabled (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr clutter_gst_video_sink_new (IntPtr texture);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern int bp_get_subtitle_count (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_subtitle (HandleRef player, int index);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_set_subtitle_uri (HandleRef player, IntPtr uri);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr bp_get_subtitle_uri (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr bp_get_subtitle_description (HandleRef player, int index);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern string gstreamer_version_string ();

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bp_dvd_is_menu (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_mouse_move_notify (HandleRef player, double x, double y);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_mouse_button_pressed_notify (HandleRef player, int button, double x, double y);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_mouse_button_released_notify (HandleRef player, int button, double x, double y);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_left_notify (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_right_notify (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_up_notify (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_down_notify (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_activate_notify (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_go_to_menu (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_go_to_next_chapter (HandleRef player);

        [DllImport (PlayerEngine.LibBansheeLibrary, CallingConvention = CallingConvention.Cdecl)]
        private static extern void bp_dvd_go_to_previous_chapter (HandleRef player);
   }
}
