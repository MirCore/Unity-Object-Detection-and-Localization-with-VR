# Unity-Object-Detection-and-Localization-with-VR
Detect and localize objects from the front-facing camera image of a VR Headset in a 3D Scene in Unity using Yolo and Barracuda.

![unityscene](https://user-images.githubusercontent.com/9919366/177161483-756bb08d-48f0-4af8-91ac-477b84bc63b7.png)

## How to use this project

- Open a scene _ObjectLocator VR_ or _ObjectLocator Webcam_
- Select your (VR) webcam from the dropdown in the _ObjectDetector_ GameObject
  - set the correct resolution
- (optional) change further settings in the _ObjectLocator_ GameObject
  - enable _Stereo Image_ for stereo image webcams (like Valve Index)

## System requirements
- Unity 2020.3 LTS or later (the project is currently using Unity 2021.3.4f1)

## YoloV4TinyBarracuda

YoloV4TinyBarracuda is an implementation of the YOLOv4-tiny object detection model on the Unity Barracuda neural network inference library.
Developed and provided by [keijiro]: [YoloV4TinyBarracuda]

[keijiro]: https://github.com/keijiro:
[YoloV4TinyBarracuda]: https://github.com/keijiro/YoloV4TinyBarracuda
