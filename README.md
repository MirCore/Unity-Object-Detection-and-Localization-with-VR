# Unity-Object-Detection-and-Localization-with-VR
Detect and localize objects from the front-facing camera image of a VR Headset in a 3D scene in Unity using Yolo and Barracuda.

![unityscene](https://user-images.githubusercontent.com/9919366/177161483-756bb08d-48f0-4af8-91ac-477b84bc63b7.png)

## Update March 2023

This project has been extended to track moving objects. A constant velocity Kalman filter has been implemented.

Currently the two systems, clustering or Kalman filter, work separately. Only one system works at a time.

## How to use this project

- Open a preset scene _Clustering VR_ or _Clustering Webcam_ for the Clustering algorithm or _Kalman Simulator_ or _Kalman Video_ for the Kalman Filter algorithm
- Select your (VR) webcam or video from the dropdown list in the _Object Detection Manager_ of the _GameManager_ GameObject


- (optional) change other settings in the _GameManager_
  - change the Localisation Method to Clustering or Kalman
  - Set the filter for the objects to find ("Labels To Find")
  - enable _Stereo Image_ for stereo image webcams (like Valve Index)
  - Set GameObjects to display detected objects (in Clustering-Mode) in the _Object Model Manager_


The _Kalman Simulator_ scene implements a simulation of people moving around. Useful for testing the Object Detection and the Kalman filter performance.

## System requirements
- Unity 2020.3 LTS or later (the project currently uses Unity 2021.3.15f1)

## YoloV4TinyBarracuda

YoloV4TinyBarracuda is an implementation of the YOLOv4-tiny object detection model on the Unity Barracuda neural network inference library.
Developed and provided by [keijiro]: [YoloV4TinyBarracuda]

[keijiro]: https://github.com/keijiro
[YoloV4TinyBarracuda]: https://github.com/keijiro/YoloV4TinyBarracuda
