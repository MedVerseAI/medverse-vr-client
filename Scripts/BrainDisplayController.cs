using UnityEngine;

public class BrainDisplayController : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;

    [Header("Brain Position")]
    public float distance = 4f;
    public float verticalOffset = 1f;

    [Header("Brain Size")]
    [Range(0.01f, 1f)]
    public float brainScale = 0.15f;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError("BrainDisplayController: No Camera found!");
            return;
        }

        MeshRenderer[] renderers =
            GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogError("BrainDisplayController: No MeshRenderers found!");
            return;
        }

        // Calculate the center of the whole brain
        Bounds brainBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            brainBounds.Encapsulate(renderers[i].bounds);
        }

        // Target position in front of camera
        Vector3 targetPosition =
            targetCamera.transform.position
            + targetCamera.transform.forward * distance
            + targetCamera.transform.up * verticalOffset;

        // Move all parts to the target position
        Vector3 offset = targetPosition - brainBounds.center;

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.transform.position += offset;
        }

        // Calculate center again after moving
        brainBounds.center = targetPosition;

        // Scale every part around the brain center
        foreach (MeshRenderer renderer in renderers)
        {
            Transform t = renderer.transform;

            Vector3 direction = t.position - targetPosition;

            t.position = targetPosition + direction * brainScale;

            t.localScale *= brainScale;
        }

        Debug.Log(
            "Brain positioned successfully. Parts found: "
            + renderers.Length
        );
    }
}