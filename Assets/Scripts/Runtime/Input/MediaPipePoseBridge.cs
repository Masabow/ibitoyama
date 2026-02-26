using UnityEngine;

#if MEDIAPIPE_UNITY_PLUGIN
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
#endif

/// <summary>
/// Bridge component for feeding MediaPipe Pose Landmarker output into PoseUdpReceiver.
/// Attach this to the same GameObject as your MediaPipe runner and call PushResult from runner callback.
/// </summary>
public class MediaPipePoseBridge : MonoBehaviour
{
    public PoseUdpReceiver target;
    public int personIndex = 0;

#if MEDIAPIPE_UNITY_PLUGIN
    public void PushResult(PoseLandmarkerResult result)
    {
        if (target == null || result.poseLandmarks == null || result.poseLandmarks.Count <= personIndex)
        {
            return;
        }

        var landmarks = result.poseLandmarks[personIndex].landmarks;
        if (landmarks == null || landmarks.Count <= 16)
        {
            return;
        }

        // BlazePose indices: nose=0, left_wrist=15, right_wrist=16
        NormalizedLandmark nose = landmarks[0];
        NormalizedLandmark leftWrist = landmarks[15];
        NormalizedLandmark rightWrist = landmarks[16];

        // MediaPipe y is top->bottom in normalized image space. This project expects the same convention.
        target.ApplyExternalPose(
            nose.x, nose.y, nose.visibility,
            leftWrist.x, leftWrist.y, leftWrist.visibility,
            rightWrist.x, rightWrist.y, rightWrist.visibility);
    }
#else
    private void Start()
    {
        Debug.LogWarning("MediaPipePoseBridge is disabled because MEDIAPIPE_UNITY_PLUGIN is not defined.");
    }
#endif
}
