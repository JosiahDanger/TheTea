# TheTea
Bare-bones desktop tea timer app built with Avalonia

---
**TL;DR**

Do you simply [want a Windows executable](https://old.reddit.com/r/github/comments/1at9br4/i_am_new_to_github_and_i_have_lots_to_say/?share_id=rjJKZS1aIO04c9zK5J3vL)? Please see [Releases](https://github.com/JosiahDanger/TheTea/releases/), and download 'TheTea.Windows.x64.zip'.

You will need to install as a prerequisite the [x64 .NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

---

If you are a professional here to see an example of my work as a software developer, please see [DateToday](https://github.com/JosiahDanger/DateToday) instead.

I find that when I am brewing tea, I often become occupied with another task, and subsequently forget to remove the tea bag from my mug. I chose to develop this app in order to solve this problem. In doing so, I saw an opportunity to teach myself Avalonia and ReactiveUI / ReactiveX.

&nbsp;

![Screenshot](https://github.com/user-attachments/assets/38713bc6-c537-41af-9405-e5a32b0e445b)

# Existing Features
- App theme adapts automatically to that of your operating system.
- Audio output:
  - When the timer decrements to five seconds, the app will begin speaking each numeral until it reaches zero.
  - The app will speak to alert the user that their tea is ready.

# Under the Hood
- .NET 8.0 !
- The app follows the MVVM architechtural pattern: the ViewModel monitors the timer Model for changes in its state, and updates the View accordingly.
- The aforementioned timer Model is implemented as a state machine using the [Stateless](https://github.com/dotnet-state-machine/stateless) library.
- Sound playback is achieved through [LibVLCSharp](https://github.com/videolan/libvlcsharp).

# Planned Features
- A toast notification will be displayed when the user's tea is ready.
- 'One More Minute' button:
  - A button will be added to the app GUI. The button will become enabled when the timer is counting down. Upon clicking the button, one minute will be added to the remaining time.
- The decorative border inside the application window will change visually according to the state of the timer.
- Settings menu:
  - The user will be able to configure application settings. Their preferences will be restored automatically upon opening the app.
  - A setting will be added in which the user will be able to configure the audio output volume of the app.

# Issues

This app is affected by the LibVLCSharp bug '[Memory usage grows when switching to new Media in MediaPlayer](https://code.videolan.org/videolan/LibVLCSharp/-/issues/442)', issue Status: Open. This is out of my control for now.
