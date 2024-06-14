using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 10f;
    public float minZoom = 1f;
    public float maxZoom = 25f;
    public float zoomFactor = 2f;

    private Camera cam;
    private float zoomMomentum;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController script must be attached to a Camera.");
        }
    }

    void Update()
    {
        if (!IsPointerOverUIElement())
        {
            ZoomCamera();
        }
    }

    private void ZoomCamera()
    {
        float scrollData = Input.GetAxis("Mouse ScrollWheel");
        zoomMomentum += scrollData;
        if (zoomMomentum != 0.0f)
        {
            // Calculate the direction vector from the camera to the mouse position
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = -cam.transform.position.z;
            Vector3 worldMousePosition = cam.ScreenToWorldPoint(mousePosition);

            // Calculate the zoom direction and target position
            Vector3 zoomDirection = (worldMousePosition - cam.transform.position).normalized;
            Vector3 targetPosition = cam.transform.position + zoomDirection * (scrollData * zoomFactor * Mathf.Sqrt(-cam.transform.position.z));

            // Clamp the camera's distance to the target position
            cam.transform.position = new Vector3(targetPosition.x, targetPosition.y, Mathf.Clamp(targetPosition.z, -maxZoom, -minZoom));

            zoomMomentum *= 0.95f;
        }
    }

    private bool IsPointerOverUIElement()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}