namespace TeaTimer.ViewModels
{
    using Avalonia.Controls;
    using Avalonia.Platform;
    using ReactiveUI;
    using LibVLCSharp.Shared;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reactive.Linq;
    using System.Threading;
    using TeaTimer.Models;

    public class TimerWindowViewModel : ViewModelBase
    {
        // TODO: After implementing a settings GUI, make these defaults configurable.
        private const byte TimerMinutesUserInputDefault = 3;
        private const byte TimerSecondsUserInputDefault = 0;

        private const byte SpeakRemainingSecondsThresholdUpper = 5;
        private const byte SpeakRemainingSecondsThresholdLower = 1;

        private const string SoundsDirectoryPath = "avares://TeaTimer/Assets/Sounds/";
        private const string SoundsAlarmFilename = "Alarm";
        private const string SoundsFilenameExtension = ".flac";

        private byte? _timerMinutesUserInput = TimerMinutesUserInputDefault;
        private byte? _timerSecondsUserInput = TimerSecondsUserInputDefault;

        private readonly string _timeSeparator =
            CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;

        private readonly TimerModel _teaTimer = new();

        private readonly ObservableAsPropertyHelper<byte> _timerRemainingMinutes =
            ObservableAsPropertyHelper<byte>.Default(0);

        private readonly ObservableAsPropertyHelper<byte> _timerRemainingSeconds =
            ObservableAsPropertyHelper<byte>.Default(0);

        private readonly ObservableAsPropertyHelper<TimerState> _timerCurrentState =
         ObservableAsPropertyHelper<TimerState>.Default(TimerState.Inactive);

        private readonly ObservableAsPropertyHelper<TimerCommand> 
            _timerExposedTransitionCommand =
                ObservableAsPropertyHelper<TimerCommand>.Default(TimerCommand.Start);

        private static readonly LibVLC _libVlc = new("--quiet");
        private static MediaPlayer? _soundPlayer;
        private static IDisposable? _alarmLoopSequenceSubscription;

        /* This application is affected by the LibVLCSharp bug
         * 'Memory usage grows when switching to new Media in MediaPlayer'.
         * 
         * See: https://code.videolan.org/videolan/LibVLCSharp/-/issues/442
         * (Issue Status is currently Open.)
         * 
         * This is out of my control for now. ¯\_(ツ)_/¯
         * 
         * I've applied lossless compression to the source sound files as a palliative 
         * measure. */

        private static readonly string _alarmSoundFileUriString =
            string
            .Concat
            (
                SoundsDirectoryPath, SoundsAlarmFilename, SoundsFilenameExtension
            );
        private static readonly Uri _alarmSoundFileUri = new(_alarmSoundFileUriString);

        private static readonly StreamMediaInput alarmSoundFileStream =
            new
            (
                AssetLoader.Open(_alarmSoundFileUri)
            );
        private static readonly Media alarmSound = new(_libVlc, alarmSoundFileStream);

        public TimerWindowViewModel()
        {
            #if DEBUG
                Trace.WriteLine("Using debug configuration.");
            #endif

            /* Monitor the TimerModel instance for changes to its RemainingMinutes,
             * RemainingSeconds, and ExposedTransitionCommand properties. When any of
             * these properties change, update this View Model accordingly.
             * 
             * ExposedTransitionCommand is used to determine the next state transition
             * trigger that can be enacted by the user. Currently, this is achieved by
             * clicking the state transition button. Note that the TimerModel instance
             * may also transition between states independently of user input, such as
             * when entering its ringing state. */

            _timerRemainingMinutes =
                this.WhenAnyValue(viewModel => viewModel._teaTimer.RemainingMinutes)
                    .ToProperty
                    (
                        this, viewModel => viewModel.TimerRemainingMinutes
                    );

            _timerRemainingSeconds =
                this.WhenAnyValue(viewModel => viewModel._teaTimer.RemainingSeconds)
                    .ToProperty
                    (
                        this, viewModel => viewModel.TimerRemainingSeconds
                    );

            _timerExposedTransitionCommand =
                this.WhenAnyValue
                    (
                        viewModel => viewModel._teaTimer.ExposedTransitionCommand
                    )
                    .ToProperty
                    (
                        this, viewModel => viewModel.TimerExposedTransitionCommand
                    );

            /* I'm using ObservableForProperty() here as opposed to WhenAnyValue(),
             * because the latter will fire when the TimerModel instance enters its
             * initial state, whereas I need the application to react only to changes in
             * state. */

            this.ObservableForProperty(viewModel => viewModel._teaTimer.CurrentState)
                .Subscribe(newState => HandleTimerStateChanged(newState.Value));
        }

        public byte? TimerMinutesUserInput
        {
            get => _timerMinutesUserInput;
            set => this.RaiseAndSetIfChanged(ref _timerMinutesUserInput, value);
        }

        public byte? TimerSecondsUserInput
        {
            get => _timerSecondsUserInput;
            set => this.RaiseAndSetIfChanged(ref _timerSecondsUserInput, value);
        }

        public byte TimerRemainingMinutes
        {
            get => _timerRemainingMinutes.Value;
        }

        public byte TimerRemainingSeconds
        {
            get
            {
                byte remainingMinutes = _timerRemainingMinutes.Value;
                byte remainingSeconds = _timerRemainingSeconds.Value;

                if (WillSpeakRemainingSeconds(remainingMinutes, remainingSeconds))
                {
                    SpeakRemainingSeconds(remainingSeconds.ToString());
                }

                return remainingSeconds;
            }
        }

        public TimerState TimerCurrentState
        {
            get => _timerCurrentState.Value;
        }

        public TimerCommand TimerExposedTransitionCommand
        { 
            get => _timerExposedTransitionCommand.Value;
        }

        public string TimeSeparator
        {
            get => _timeSeparator;
        }

        public void ButtonChangeTimerStateClick()
        {
            ExecuteTimerStateTransition();
        }

        private void ExecuteTimerStateTransition()
        {
            TimerCommand nextTransitionCommand = TimerExposedTransitionCommand;

            if (nextTransitionCommand.Equals(TimerCommand.Start))
            {
                // Handle null user input.
                byte sanitisedTimerMinutesUserInput = TimerMinutesUserInput ?? 0;
                byte sanitisedTimerSecondsUserInput = TimerSecondsUserInput ?? 0;

                if 
                (
                    sanitisedTimerMinutesUserInput > 0 || 
                    sanitisedTimerSecondsUserInput > 0
                )
                {
                    _teaTimer
                    .ExecuteStartTransition
                    (
                        sanitisedTimerMinutesUserInput, sanitisedTimerSecondsUserInput
                    );
                }
            }
            else
            {
                _teaTimer.ExecuteTransition(nextTransitionCommand);
            }
        }

        private static void HandleTimerStateChanged(TimerState newState)
        {
            switch (newState)
            {
                case TimerState.Inactive:
                {
                    /* I am calling Stop() in its own thread, so as not to block the
                     * caller's thread. If I were to instead call the method without
                     * assigning it to a new thread, this would cause the GUI button
                     * that triggered the state change to become visually unresponsive
                     * until the current sound finishes playing. */

                    if (_soundPlayer != null)
                    {
                        ThreadPool
                        .QueueUserWorkItem
                        (
                            delegate
                            {
                                _soundPlayer.Stop();
                                _soundPlayer.Media?.Dispose();
                            }
                        );
                    };
                    _alarmLoopSequenceSubscription?.Dispose();

                    break;
                }
                case TimerState.Ringing:
                {
                    SoundAlarm();

                    break;
                }
            };
        }

        private static bool WillSpeakRemainingSeconds
        (
            byte remainingMinutes, byte remainingSeconds
        )
        {
            return
                remainingMinutes == 0 &&
                remainingSeconds >= SpeakRemainingSecondsThresholdLower &&
                remainingSeconds <= SpeakRemainingSecondsThresholdUpper;
        }

        private static async void SpeakRemainingSeconds(string remainingSecondsString)
        {
            string currentSoundFileUriString = 
                string
                .Concat
                (
                    SoundsDirectoryPath, remainingSecondsString, SoundsFilenameExtension
                );
            Uri currentSoundFileUri = new(currentSoundFileUriString);

            StreamMediaInput currentSoundFileStream = 
                new
                (
                    AssetLoader.Open(currentSoundFileUri)
                );

            /* I've adapted the below LibVLCSharp functionality from official sample
             * code available at:
             * 
             * https://github.com/mfkl/libvlcsharp-samples/blob/master/Speech/Program.cs */

            using Media sound = new(_libVlc, currentSoundFileStream);

            /* TODO:
             *  
             *  The StreamMediaInput instance 'currentSoundFileStream' will leak memory
             *  if not disposed of. However, if disposal is implemented by means of a
             *  synchronous 'using' statement, the object will be freed immediately, 
             *  before the sound has actually been played. Obviously, this behaviour 
             *  results in the sound not playing.
             *  
             *  My temporary solution to this problem is to await the EndReached
             *  EventHandler of the MediaPlayer instance '_soundPlayer', and manually
             *  dispose of the StreamMediaInput instance when playback has finished.
             *  
             *  I think this behaviour could be better implemented through the new 
             *  IAsyncDisposable interface introduced in C# 8.0, in an 'await using' 
             *  statement */

            _soundPlayer = new(sound);
            _soundPlayer.Play();

            await
            Observable
            .FromEventPattern<EventArgs>
            (
                handler => _soundPlayer.EndReached += handler,
                handler => _soundPlayer.EndReached -= handler
            )
            .Do
            (
                delegate
                {
                    currentSoundFileStream.Dispose();

                    #if DEBUG
                        Trace
                        .WriteLine
                        (
                            "Disposed of StreamMediaInput for " +
                            $"{currentSoundFileUriString}."
                        );
                    #endif
                }
            );
        }

        private static void SoundAlarm()
        {
            _soundPlayer = new(alarmSound);

            /* Loop playback of the alarm sound. LibVLCSharp does not conveniently
             * support this feature.
             *
             * See: https://stackoverflow.com/a/56488835
             *
             * I have converted the MediaPlayer EndReached event into a Reactive UI
             * observable.
             *
             * See: https://www.reactiveui.net/docs/handbook/events.html#how-do-i-convert-my-own-c-events-into-observables */

            /* TODO:
             *  I am, upon the timer entering an Inactive state, explicitly handling 
             *  disposal of this ReactiveUI subscription. Is this necessary? */

            _alarmLoopSequenceSubscription =
                Observable
                .FromEventPattern<EventArgs>
                (
                    handler => _soundPlayer.EndReached += handler,
                    handler => _soundPlayer.EndReached -= handler
                )
                .Subscribe
                (
                    delegate 
                    {
                        ThreadPool
                        .QueueUserWorkItem
                        (
                            delegate
                            {
                                _soundPlayer.Stop();
                                _soundPlayer.Play();
                            }
                        );
                    }
                );

            _soundPlayer.Play();
        }
    }
}
