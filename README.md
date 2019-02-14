# Tablet VR - Unity controller
Controller for virtual reality tasks for execution of custom tasks. The virtual environments (aka scenes) are displayed in several monitor tablets around the field of view of the subject.

![alt text](http://www.interphaser.com/images/content/smoothwalk-hardware-setup-labeled.png "Tablet based VR")

## Prerequisites
* [Unity 3D][Unity 3D]
* [Android Studio][Android Studio]
* [Java SDK][Java SDK]
* [Arduino IDE][Arduino IDE]
* Treadmill system with Arduino Mega 2560.
* Port UDP 32000 enabled in the firewall/network for MATLAB, Unity, and any future compilations.

Code was last built and tested with
* Unity 2018.a.0f2
* Java JDK1.8.0_172
* Android Studio 3.1.2
	* Android SDK Platform 27 revision 3
	* Android SDK Build-Tools 28-rc2 version 28.0.0 rc2
* PC
	* OS: Windows 10
	* CPU: Intel i7-6700HQ
	* RAM: 16 GB
	* Graphics: GEFORCE GTX 960M
	* SSD: Samsung SM951
* Tablets
	* OS: Android 5.0
	* SoC: Qualcomm Snapdragon 410 APQ8016

## Installation
* Install Arduino and upload [Arduino.ino](Arduino/Arduino.ino) to an Arduino Mega 2560.
* Install Java SDK and add <java-installation-path>/bin to the System Environment Variables.
* Install Android Studio.
* Install Unity, adding support for Android.
* Import the [Unity package](Unity/Tablet VR - 20180615.unitypackage) or else open Unity, create a new project, copy the [Assets](Unity/Assets) folder.
* In Unity, go to `File.. Build Settings.. Player Settings..` and change `Api Compatibility Level` to `.NET 4.x`

## Version History
### 0.1.0
* Initial Release: Library.

## License
© 2018 [Leonardo Molina][Leonardo Molina]

This project is licensed under the [GNU GPLv3 License][LICENSE.md].

[Java SDK]: http://www.oracle.com/technetwork/java/javase/downloads/index.html
[Unity 3D]: https://unity3d.com/unity
[Android Studio]: https://developer.android.com/studio
[Arduino IDE]: https://arduino.cc
[Leonardo Molina]: https://github.com/leomol
[LICENSE.md]: LICENSE.md