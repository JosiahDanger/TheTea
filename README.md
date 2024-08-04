# TheTea
Bare-bones desktop tea timer app built with Avalonia.

# Existing Features
- App theme adapts automatically to that of your operating system.
- Audio output:
  - When the timer decrements to five seconds, the app will begin speaking each numeral until it reaches zero.
  - The app will speak to alert the user that their tea is ready.

# Under the Hood
I have developed this app in order to teach myself Avalonia and ReactiveUI / ReactiveX.
- The app follows the MVVM architechtural pattern: the ViewModel monitors the timer Model for changes in its state, and updates the View accordingly.
- The aforementioned timer Model is implemented as a state machine using the [Stateless](https://github.com/dotnet-state-machine/stateless) library.
- Sound playback is achieved through [LibVLCSharp](https://github.com/videolan/libvlcsharp).

# Planned Features
- A toast notification will be displayed when the user's tea is ready.
- 'One More Minute' button:
  - A button will be added to the app GUI. The button will become enabled when the timer is counting down. Upon clicking the button, one minute will be added to the remaining time.
- Settings menu:
  - The user will be able to configure application settings. Their preferences will be restored automatically upon opening the app.
  - A setting will be added in which the user will be able to configure the audio output volume of the app.

# Issues

This app is affected by the LibVLCSharp bug '[Memory usage grows when switching to new Media in MediaPlayer](https://code.videolan.org/videolan/LibVLCSharp/-/issues/442)', issue Status: Open. This is out of my control for now.

I am new to ReactiveUI / ReactiveX, and I feel that I have a lot to learn. In particular, I am unsure of when I should explicitly dispose of IDisposable objects. Please let me know if I have misused any ReactiveUI operators.

In addition, I would appreciate feedback regarding the structure of my application. Please inform me if you come across any instances of bad programming practice. I would like to prioritise neat and readable code.
