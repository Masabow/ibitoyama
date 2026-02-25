using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Receives normalized pose keypoints over UDP and triggers a simple hands-up action.
/// Unity-only local mock mode is supported by default, with optional UDP input for future expansion.
/// </summary>
public class PoseUdpReceiver : MonoBehaviour
{
    [Header("Input Source")]
    public bool useUdpInput = false;

    [Header("Network")]
    public int listenPort = 5005;

    [Header("Filtering")]
    [Range(0.0f, 1.0f)]
    public float confidenceThreshold = 0.5f;

    [Range(0.0f, 1.0f)]
    public float smoothing = 0.35f;

    [Header("Local Mock (when useUdpInput = false)")]
    public float mockCycleSeconds = 4.0f;

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
    private volatile bool _running;

    private Vector2 _leftWrist;
    private Vector2 _rightWrist;
    private Vector2 _nose;
    private bool _barrierActive;

    [Serializable]
    private class PosePayload
    {
        public float timestamp;
        public Keypoints keypoints;
    }

    [Serializable]
    private class Keypoints
    {
        public float[] left_wrist;
        public float[] right_wrist;
        public float[] nose;
    }

    private void Start()
    {
        if (!useUdpInput)
        {
            Debug.Log("PoseUdpReceiver running with local mock input (UDP disabled)");
            return;
        }

        _udpClient = new UdpClient(listenPort);
        _running = true;

        _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        _receiveThread.Start();

        Debug.Log($"PoseUdpReceiver listening on :{listenPort}");
    }

    private void Update()
    {
        if (useUdpInput)
        {
            while (_queue.TryDequeue(out var json))
            {
                ApplyPayload(json);
            }
        }
        else
        {
            ApplyLocalMock();
        }

        bool handsUp = _leftWrist.y < _nose.y && _rightWrist.y < _nose.y;
        if (handsUp != _barrierActive)
        {
            _barrierActive = handsUp;
            Debug.Log(_barrierActive ? "Barrier ON" : "Barrier OFF");
        }
    }

    private void OnDestroy()
    {
        _running = false;
        _udpClient?.Close();

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join(200);
        }
    }

    private void ReceiveLoop()
    {
        var any = new IPEndPoint(IPAddress.Any, 0);

        while (_running)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref any);
                string json = Encoding.UTF8.GetString(data);
                _queue.Enqueue(json);
            }
            catch (SocketException)
            {
                // ignore expected shutdown path
            }
            catch (ObjectDisposedException)
            {
                // ignore expected shutdown path
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Pose receive error: {ex.Message}");
            }
        }
    }

    private void ApplyPayload(string json)
    {
        var payload = JsonUtility.FromJson<PosePayload>(json);
        if (payload == null || payload.keypoints == null)
        {
            return;
        }

        TryUpdate(ref _leftWrist, payload.keypoints.left_wrist);
        TryUpdate(ref _rightWrist, payload.keypoints.right_wrist);
        TryUpdate(ref _nose, payload.keypoints.nose);
    }

    private void ApplyLocalMock()
    {
        float cycle = Mathf.Max(1.0f, mockCycleSeconds);
        float phase = (Time.time % cycle) / cycle;
        float baseX = 0.5f + 0.06f * Mathf.Sin(phase * 2.0f * Mathf.PI);
        float noseY = 0.25f;

        bool handsUp = phase >= 0.35f && phase <= 0.55f;
        float leftY = handsUp ? noseY - 0.06f : 0.48f + 0.02f * Mathf.Sin(phase * 6.0f * Mathf.PI);
        float rightY = handsUp ? noseY - 0.05f : 0.48f + 0.02f * Mathf.Cos(phase * 6.0f * Mathf.PI);

        TryUpdate(ref _leftWrist, new[] { baseX - 0.16f, leftY, 0.95f });
        TryUpdate(ref _rightWrist, new[] { baseX + 0.16f, rightY, 0.95f });
        TryUpdate(ref _nose, new[] { baseX, noseY, 0.98f });
    }

    private void TryUpdate(ref Vector2 current, float[] kp)
    {
        if (kp == null || kp.Length < 3)
        {
            return;
        }

        float confidence = kp[2];
        if (confidence < confidenceThreshold)
        {
            return;
        }

        var next = new Vector2(kp[0], kp[1]);
        current = Vector2.Lerp(current, next, smoothing);
    }
}
