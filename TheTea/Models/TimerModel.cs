using System;
using System.Diagnostics;
using Avalonia.Threading;
using ReactiveUI;
using Stateless;

namespace TheTea.Models
{
    public class TimerModel : ReactiveObject
    {
        private const byte StartHours = 0;
        private const byte CountIntervalSeconds = 1;

        private byte _remainingMinutes = 0;
        private byte _remainingSeconds = 0;

        private TimerCommand _exposedTransitionCommand = TimerCommand.Start;
        private TimerState _currentState = TimerState.Inactive;

        private DispatcherTimer? _dispatcherTimer;
        private TimeSpan _timeDurationRemaining;

        private readonly StateMachine<TimerState, TimerCommand> _stateMachine = 
            new(TimerState.Inactive);

        private readonly 
            StateMachine<TimerState, TimerCommand>.TriggerWithParameters<byte, byte> 
                _startTrigger;

        public TimerModel()
        {
            _startTrigger =
                _stateMachine.SetTriggerParameters<byte, byte>(TimerCommand.Start);

            // Configure permitted transitions between Timer states.

            _stateMachine.OnTransitioned(OnTransition);

            _stateMachine.Configure(TimerState.Inactive)
                         .Ignore(TimerCommand.Stop)
                         .Permit(TimerCommand.Start, TimerState.Active)
                         .OnEntryFrom(TimerCommand.Stop, () => OnStop())
                         .OnEntryFrom(TimerCommand.Dismiss, () => OnDismiss());

            _stateMachine.Configure(TimerState.Active)
                         .Ignore(TimerCommand.Start)
                         .Permit(TimerCommand.Stop, TimerState.Inactive)
                         .Permit(TimerCommand.Remind, TimerState.Ringing)
                         .OnEntryFrom
                         (
                            _startTrigger, (min, sec) => OnStart(min, sec)
                         );

            _stateMachine.Configure(TimerState.Ringing)
                         .Ignore(TimerCommand.Remind)
                         .Permit(TimerCommand.Dismiss, TimerState.Inactive)
                         .OnEntry(() => OnRemind());
        }

        public void ExecuteStartTransition(byte startMinutes, byte startSeconds)
        {
            if (_stateMachine.CanFire(TimerCommand.Start))
            {
                _stateMachine.Fire(_startTrigger, startMinutes, startSeconds);
            }
            else
            {
                RaiseExceptionInvalidTransition(TimerCommand.Start);
            }
        }

        public void ExecuteTransition(TimerCommand command)
        {
            if (_stateMachine.CanFire(command))
            {
                _stateMachine.Fire(command);   
            }
            else
            {
                RaiseExceptionInvalidTransition(command);
            }
        }

        /* TODO:
         * 
         * The methods ExecuteStartTransition() and ExecuteTransition() share similar
         * code. Consider using method overloading instead? */

        private void RaiseExceptionInvalidTransition(TimerCommand command)
        {
            throw new
                InvalidOperationException
                (
                    $"Cannot transition from {_stateMachine.State} via {command}."
                );
        }

        private void OnTransition
        (
            StateMachine<TimerState, TimerCommand>.Transition transition
        )
        {
            #if DEBUG
                Trace
                .WriteLine
                    (
                        $"Transitioned from {transition.Source} to " +
                        $"{transition.Destination} via {transition.Trigger}."
                    );
            #endif

            CurrentState = transition.Destination;
        }

        public byte RemainingMinutes
        {
            get => _remainingMinutes;
            set => this.RaiseAndSetIfChanged(ref _remainingMinutes, value);
        }

        public byte RemainingSeconds
        {
            get => _remainingSeconds;
            set => this.RaiseAndSetIfChanged(ref _remainingSeconds, value);
        }

        public TimerCommand ExposedTransitionCommand
        {
            get => _exposedTransitionCommand;
            set => this.RaiseAndSetIfChanged(ref _exposedTransitionCommand, value); 
        }

        public TimerState CurrentState
        {
            get => _currentState;
            set => this.RaiseAndSetIfChanged(ref _currentState, value);
        }

        private void OnStart(byte startMinutes, byte startSeconds)
        {
            RemainingMinutes = startMinutes;
            RemainingSeconds = startSeconds;

            InitialiseTimer(startMinutes, startSeconds);
            _dispatcherTimer?.Start();

            ExposedTransitionCommand = TimerCommand.Stop;
        }

        private void OnStop()
        {
            _dispatcherTimer?.Stop();

            RemainingMinutes = 0;
            RemainingSeconds = 0;

            ExposedTransitionCommand = TimerCommand.Start;
        }

        private void OnRemind()
        {
            ExposedTransitionCommand = TimerCommand.Dismiss;
        }

        private void OnDismiss()
        {
            ExposedTransitionCommand = TimerCommand.Start;
        }

        private void InitialiseTimer(byte initialMinutes, byte initialSeconds)
        {
            _dispatcherTimer = new DispatcherTimer();
            _timeDurationRemaining =
                new
                    TimeSpan
                    (
                        StartHours,
                        initialMinutes,
                        initialSeconds
                    );

            _dispatcherTimer.Interval = TimeSpan.FromSeconds(CountIntervalSeconds);
            _dispatcherTimer.Tick += TimerTickEventHandler;
        }

        private void TimerTickEventHandler(object? sender, EventArgs e)
        {
            if (_timeDurationRemaining == TimeSpan.Zero)
            {
                _dispatcherTimer?.Stop();
                ExecuteTransition(TimerCommand.Remind);
            }
            else
            {
                _timeDurationRemaining =
                    _timeDurationRemaining
                    .Subtract
                    (
                        TimeSpan.FromSeconds(CountIntervalSeconds)
                    );

                RemainingMinutes = (byte)_timeDurationRemaining.Minutes;
                RemainingSeconds = (byte)_timeDurationRemaining.Seconds;
            }
        }
    }
}
