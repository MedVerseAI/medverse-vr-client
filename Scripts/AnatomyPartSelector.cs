using UnityEngine;
using TMPro;

public class AnatomyPartSelector : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField commandInput;
    public Transform brainRoot;

    [Header("Highlight")]
    public Color highlightColor = Color.red;

    private MeshRenderer selectedRenderer;
    private MaterialPropertyBlock propertyBlock;

    public void SelectPart()
    {
        string searchName = commandInput.text.Trim();

        if (string.IsNullOrEmpty(searchName))
        {
            Debug.Log("Please enter a brain part name.");
            return;
        }

        Debug.Log("Searching for: " + searchName);

        ClearSelection();

        MeshRenderer[] parts =
            brainRoot.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in parts)
        {
            string objectName = renderer.gameObject.name;

            if (objectName.ToLower().Contains(searchName.ToLower()))
            {
                SelectRenderer(renderer);

                Debug.Log("FOUND: " + objectName);
                Debug.Log("SELECTED: " + objectName);

                return;
            }
        }

        Debug.Log("Brain part NOT FOUND: " + searchName);
    }

    private void SelectRenderer(MeshRenderer renderer)
    {
        selectedRenderer = renderer;

        propertyBlock = new MaterialPropertyBlock();

        renderer.GetPropertyBlock(propertyBlock);

        // Try common shader color properties
        propertyBlock.SetColor("_BaseColor", highlightColor);
        propertyBlock.SetColor("_Color", highlightColor);

        renderer.SetPropertyBlock(propertyBlock);

        Debug.Log(
            "HIGHLIGHT APPLIED TO: " +
            renderer.gameObject.name
        );
    }

    public void ClearSelection()
    {
        if (selectedRenderer != null)
        {
            selectedRenderer.SetPropertyBlock(null);
        }

        selectedRenderer = null;
        propertyBlock = null;
    }
}