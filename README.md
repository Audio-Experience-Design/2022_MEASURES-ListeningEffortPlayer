# Listening Effort Player

VR app for Pico Neo 3 headset for the Listening Effort project.

Earlier versions of this project were built for the HTC Vive. The last compatible version for HTC Vive can be found on the `htc-vive` branch.

## Build instructions

### Pico Neo 3

- Install Unity 2021.3.26f1.
- In Build Settings, set platform to Android.
- Plug in Pico, enable developer permissions if needed, then select Pico as Run Device in Build Settings.
- Build and run.

## Run modes

### Scripted mode (Pico Neo 3 only)

Scripts may be defined in YAML and selected in the app. Audio will be played back on headset using the 3DTI Toolkit. The app will then automatically cycle through all specified videos and invite the participant to record their response using the headset microphone. Full session data is logged.

### OSC mode

In OSC mode, the app is fully controlled by a Max patch over an OSC connection. Audio is not played, scripts are not read and data is logged.

An example Max patch demonstrating this can be found in `Utilities/Unity controller.maxpat`. (On the `htc-vive` branch this may be in a different location).

## Known issues

- When the app is opened, it will check all video files to ensure they can be opened. This takes a long time, and it may be necessary to cache these findings and simply confirm the filename and timestamp haven't changed on previously validated videos.
- While videos are being checked, it remains possible to select a script. If the script references videos that haven't yet been checked, then the script will fail validation with an error stating that these videos cannot be found. To avoid this, wait until the video check is complete before selecting a script. (If you've already selected the script, then change to a different script then change back to the intended script to retrigger validation.)

## Speech Transcription

The app in the `master` branch currently asks participants to record their audio response but does not perform any transcription on this.

Research was conducted on the feasibility of automatic transcription of the recorded speech. The full findings can be read in [2024-02-15 Report on feasibility of speech recognition.pdf](./2024-02-15%20Report%20on%20feasibility%20of%20speech%20recognition.pdf).

The prototypes mentioned in the report can be found as follows:

### On-device transcription using Whispercpp.

Within the `speech-recognition` branch.

### Post-experiment transcription

A python implementation using Whispercpp.py can be found in the `master` branch under the `Utilities/LogPostprocessor` directory.

