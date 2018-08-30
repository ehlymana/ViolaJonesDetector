# Viola-Jones Detector

An app used for detecting faces using the Viola-Jones algorithm.

## Executing the single-threaded version of the detector

By executing the *FaceDetection.exe* file located in *FaceDetection\FaceDetection\bin\Release* folder.

## Executing the multi-threaded version of the detector

By executing the *FaceDetection.exe* file located in *FaceDetection Multithreading\FaceDetection\bin\Release* folder.

## Attributes of the detector

1. Detector training on images of faces;
2. Detector training on non-face images;
3. Face detection on a single image;
4. Face detection on multiple images;
5. Accuracy improvement based on the results of face detection on a single image;
6. Displaying statistical data.

## Additional information

- All actions are executed only on **.jpg** images.
- Detector can detect faces only if they are in frontal position, and if they are not rotated.
- Brightness or hue have no importance in the detection process.
- The images used for detection need to be at least 24x24 px in size.
- Success criterium is 90% (9 positive and 1 negative detection).
- 4 types of Haar-characteristics are used to determine whether a face exists in the image or not:
1. Nose characteristic;
2. Eyebrow characteristic;
3. Eye characteristic;
4. Face frame characteristic.

*Faculty of Electrical Engineering*
*University of Sarajevo*
*2018.*
