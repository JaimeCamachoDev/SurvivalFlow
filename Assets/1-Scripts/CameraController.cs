using UnityEngine;

/// <summary>
/// Permite mover la cámara en el plano XZ y hacer zoom con la rueda del ratón
/// para inspeccionar distintas zonas del mapa.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    public enum Mode { Free, FollowHerbivore, FollowCarnivore }
    public Mode mode = Mode.Free;
    Transform followTarget;
    Vector3 followOffset;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed,
                                              minZoom, maxZoom);
        }
        else
        {
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed,
                                          minZoom, maxZoom);
        }

        if (mode == Mode.Free)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(h, 0f, v) * moveSpeed * Time.deltaTime;
            transform.position += move;
        }
        else
        {
            if (followTarget == null)
            {
                followTarget = mode == Mode.FollowHerbivore ?
                    SurvivorTracker.GetOldestHerbivoreTransform() :
                    SurvivorTracker.GetOldestCarnivoreTransform();
                if (followTarget != null)
                    followOffset = transform.position - followTarget.position;
            }
            if (followTarget != null)
                transform.position = followTarget.position + followOffset;
        }
    }

    public void SetFreeMode()
    {
        mode = Mode.Free;
        followTarget = null;
    }

    public void FollowBestHerbivore()
    {
        mode = Mode.FollowHerbivore;
        followTarget = null;
    }

    public void FollowBestCarnivore()
    {
        mode = Mode.FollowCarnivore;
        followTarget = null;
    }
}
