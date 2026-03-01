using System.Reflection;
using Ibitoyama.BodyShooter;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class PoseResultBridge : MonoBehaviour
  {
    [SerializeField] private BodyPoseInputAdapter inputAdapter;
    [SerializeField] private PoseLandmarkerRunner poseLandmarkerRunner;

    private PoseLandmarkerResultAnnotationController _annotationController;
    private FieldInfo _currentTargetField;

    private void Awake()
    {
      if (inputAdapter == null)
      {
        inputAdapter = FindFirstObjectByType<BodyPoseInputAdapter>();
      }

      if (poseLandmarkerRunner == null)
      {
        poseLandmarkerRunner = GetComponent<PoseLandmarkerRunner>();
      }

      ResolveAnnotationController();
    }

    private void Update()
    {
      if (inputAdapter == null || _annotationController == null || _currentTargetField == null)
      {
        return;
      }

      var boxed = _currentTargetField.GetValue(_annotationController);
      if (boxed is PoseLandmarkerResult result)
      {
        OnPoseResult(result);
      }
    }

    public void OnPoseResult(PoseLandmarkerResult result)
    {
      inputAdapter?.PushPoseResult(result);
    }

    public void Configure(BodyPoseInputAdapter adapter, PoseLandmarkerRunner runner = null)
    {
      inputAdapter = adapter;
      if (runner != null)
      {
        poseLandmarkerRunner = runner;
      }
      ResolveAnnotationController();
    }

    private void ResolveAnnotationController()
    {
      if (poseLandmarkerRunner == null)
      {
        return;
      }

      var controllerField = typeof(PoseLandmarkerRunner).GetField("_poseLandmarkerResultAnnotationController", BindingFlags.Instance | BindingFlags.NonPublic);
      if (controllerField == null)
      {
        return;
      }

      _annotationController = controllerField.GetValue(poseLandmarkerRunner) as PoseLandmarkerResultAnnotationController;
      if (_annotationController == null)
      {
        return;
      }

      _currentTargetField = typeof(PoseLandmarkerResultAnnotationController).GetField("_currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
    }
  }
}
