using System.Collections;
using UnityEngine;

namespace MedVerse.VR
{
    /// <summary>
    /// Frames and scales an imported 3D anatomical model at comfortable arm's reach in front of the
    /// Meta Quest 3 XR camera using a single root pivot transform (preventing nested double-scaling).
    /// </summary>
    public class BrainDisplayController : MonoBehaviour
    {
        [Header("Camera")]
        public Camera targetCamera;

        [Header("Quest 3 Arm's-Reach Positioning (Meters)")]
        [Tooltip("Horizontal distance in front of the user's headset (0.9 - 1.2 m recommended for Quest 3).")]
        public float distance = 1.0f;

        [Tooltip("Vertical offset relative to eye level in meters (-0.1 m places the model slightly below eye level).")]
        public float verticalOffset = -0.1f;

        [Header("Model Scale")]
        [Tooltip("Uniform scale applied once to the root pivot. v0.2 FBX exports use real meters (1.0 = life size).")]
        [Range(0.05f, 3.0f)]
        public float brainScale = 1.0f;

        [Header("XR Initialization")]
        [Tooltip("Wait one frame before initial framing so Meta Quest 3 XR tracking pose settles.")]
        public bool deferInitialFraming = true;

        private Transform pivotTransform;
        private Vector3 initialPivotScale = Vector3.one;
        private bool pivotInitialized;

        private void Start()
        {
            if (deferInitialFraming)
            {
                StartCoroutine(FrameAfterXRInit());
            }
            else
            {
                Recenter();
            }
        }

        private IEnumerator FrameAfterXRInit()
        {
            // Allow one frame for XR HMD tracking pose to initialize on Meta Quest 3.
            yield return null;
            Recenter();
        }

        /// <summary>
        /// Repositions and scales the anatomical model cleanly in front of the active camera on the horizontal XZ plane.
        /// Safe to call repeatedly from a controller button or UI "Recenter" action.
        /// </summary>
        public void Recenter()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                Debug.LogError("BrainDisplayController: No Camera found! Assign targetCamera in the Inspector.");
                return;
            }

            if (!EnsurePivotInitialized())
            {
                return;
            }

            // Reset scale before measuring active bounds so repeated Recenter() calls never compound scale.
            pivotTransform.localScale = initialPivotScale;

            if (!TryComputeActiveBounds(pivotTransform, out Bounds modelBounds))
            {
                Debug.LogError("BrainDisplayController: No active, enabled MeshRenderers found under " + name + ".");
                return;
            }

            // Flatten camera forward onto the horizontal XZ plane so looking down/up never spawns into floor/ceiling.
            Vector3 flatForward = Vector3.ProjectOnPlane(targetCamera.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-4f)
            {
                flatForward = Vector3.ProjectOnPlane(targetCamera.transform.up, Vector3.up);
            }
            if (flatForward.sqrMagnitude < 1e-4f)
            {
                flatForward = Vector3.forward;
            }
            flatForward.Normalize();

            Vector3 targetPosition =
                targetCamera.transform.position
                + flatForward * distance
                + Vector3.up * verticalOffset;

            // Translate and scale ONLY the single root pivot transform once (prevents nested double-offset & brainScale^2).
            Vector3 centerOffset = targetPosition - modelBounds.center;
            pivotTransform.position += centerOffset;
            pivotTransform.localScale = initialPivotScale * brainScale;

            // Re-align bounds center after scaling around pivotTransform.position.
            if (TryComputeActiveBounds(pivotTransform, out Bounds scaledBounds))
            {
                pivotTransform.position += (targetPosition - scaledBounds.center);
            }

            Debug.Log(
                "BrainDisplayController: Positioned single root pivot cleanly at "
                + targetPosition
                + " (scale=" + brainScale + ")."
            );
        }

        private bool EnsurePivotInitialized()
        {
            if (pivotInitialized && pivotTransform != null)
            {
                return true;
            }

            if (!TryComputeActiveBounds(transform, out Bounds initialBounds))
            {
                Debug.LogError("BrainDisplayController: No active, enabled MeshRenderers found under " + name + ".");
                return false;
            }

            GameObject pivotObj = new GameObject("AnatomyRootPivot");
            pivotTransform = pivotObj.transform;
            pivotTransform.SetParent(transform, false);
            pivotTransform.position = initialBounds.center;
            pivotTransform.rotation = Quaternion.identity;
            pivotTransform.localScale = Vector3.one;

            // Reparent only direct children of this root (preserving nested hierarchies & world poses).
            int childCount = transform.childCount;
            Transform[] directChildren = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                directChildren[i] = transform.GetChild(i);
            }

            foreach (Transform child in directChildren)
            {
                if (child != pivotTransform)
                {
                    child.SetParent(pivotTransform, true);
                }
            }

            initialPivotScale = pivotTransform.localScale;
            pivotInitialized = true;
            return true;
        }

        private static bool TryComputeActiveBounds(Transform root, out Bounds combinedBounds)
        {
            // Exclude inactive GameObjects and disabled MeshRenderers so zeroed bounds at (0,0,0) never skew framing.
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(false);
            combinedBounds = new Bounds();
            bool hasAny = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasAny)
                {
                    combinedBounds = r.bounds;
                    hasAny = true;
                }
                else
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
            }

            return hasAny;
        }
    }
}
