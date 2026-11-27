using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MedVerse.VR
{
    /// <summary>
    /// Searches and highlights matching anatomical meshes (including bilateral L/R pairs and multi-part organs)
    /// using a single cached MaterialPropertyBlock without wiping unrelated shader overrides.
    /// </summary>
    public class AnatomyPartSelector : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        private struct HighlightRecord
        {
            public MeshRenderer Renderer;
            public bool HadBaseColor;
            public Color OriginalBaseColor;
            public bool HadLegacyColor;
            public Color OriginalLegacyColor;
        }

        [Header("References")]
        public TMP_InputField commandInput;
        public Transform brainRoot;

        [Header("Highlight")]
        public Color highlightColor = Color.red;

        private readonly List<HighlightRecord> selectedRenderers = new List<HighlightRecord>();
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Reads the search query from <see cref="commandInput"/> and highlights all matching anatomical meshes.
        /// Empty or whitespace input clears any previous selection cleanly.
        /// </summary>
        public void SelectPart()
        {
            if (commandInput == null)
            {
                Debug.LogError("AnatomyPartSelector: commandInput is not assigned in the Inspector.");
                return;
            }

            SelectPart(commandInput.text);
        }

        /// <summary>
        /// Programmatic search entry point (usable by UI input, voice commands, or manifest/FMA lookups).
        /// </summary>
        public void SelectPart(string rawQuery)
        {
            // Always clear previous highlights first (Issue #3 acceptance criterion: empty input clears selection).
            ClearSelection();

            string searchName = string.IsNullOrWhiteSpace(rawQuery) ? string.Empty : rawQuery.Trim();
            if (string.IsNullOrEmpty(searchName))
            {
                Debug.Log("AnatomyPartSelector: Empty search input — previous highlights cleared.");
                return;
            }

            if (brainRoot == null)
            {
                Debug.LogError("AnatomyPartSelector: brainRoot is not assigned in the Inspector.");
                return;
            }

            EnsurePropertyBlock();

            MeshRenderer[] parts = brainRoot.GetComponentsInChildren<MeshRenderer>(true);
            int matchCount = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                MeshRenderer partRenderer = parts[i];
                if (partRenderer == null)
                {
                    continue;
                }

                string objectName = partRenderer.gameObject.name;
                if (objectName.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    HighlightRenderer(partRenderer);
                    matchCount++;
                }
            }

            if (matchCount > 0)
            {
                Debug.Log("AnatomyPartSelector: Highlighted " + matchCount + " matching mesh(es) for '" + searchName + "'.");
            }
            else
            {
                Debug.Log("AnatomyPartSelector: No anatomical structure matched '" + searchName + "'.");
            }
        }

        private void HighlightRenderer(MeshRenderer targetRenderer)
        {
            EnsurePropertyBlock();
            propertyBlock.Clear();
            targetRenderer.GetPropertyBlock(propertyBlock);

            Material sharedMat = targetRenderer.sharedMaterial;
            HighlightRecord record = new HighlightRecord
            {
                Renderer = targetRenderer,
                HadBaseColor = sharedMat != null && sharedMat.HasProperty(BaseColorId),
                OriginalBaseColor = Color.white,
                HadLegacyColor = sharedMat != null && sharedMat.HasProperty(LegacyColorId),
                OriginalLegacyColor = Color.white,
            };

            // Capture existing color (preferring current property block override if set, else sharedMaterial).
            if (record.HadBaseColor)
            {
                Color blockColor = propertyBlock.GetColor(BaseColorId);
                record.OriginalBaseColor = blockColor.a > 0f ? blockColor : sharedMat.GetColor(BaseColorId);
                propertyBlock.SetColor(BaseColorId, highlightColor);
            }
            if (record.HadLegacyColor)
            {
                Color blockColor = propertyBlock.GetColor(LegacyColorId);
                record.OriginalLegacyColor = blockColor.a > 0f ? blockColor : sharedMat.GetColor(LegacyColorId);
                propertyBlock.SetColor(LegacyColorId, highlightColor);
            }

            // Fallback when material uses an unknown shader without standard color property declarations.
            if (!record.HadBaseColor && !record.HadLegacyColor)
            {
                propertyBlock.SetColor(BaseColorId, highlightColor);
                propertyBlock.SetColor(LegacyColorId, highlightColor);
            }

            targetRenderer.SetPropertyBlock(propertyBlock);
            selectedRenderers.Add(record);
        }

        /// <summary>
        /// Restores original colors on all highlighted renderers without calling destructive SetPropertyBlock(null),
        /// preserving unrelated shader overrides (transparency, clipping planes, fade states).
        /// </summary>
        public void ClearSelection()
        {
            if (selectedRenderers.Count == 0)
            {
                return;
            }

            EnsurePropertyBlock();

            for (int i = 0; i < selectedRenderers.Count; i++)
            {
                HighlightRecord record = selectedRenderers[i];
                if (record.Renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                record.Renderer.GetPropertyBlock(propertyBlock);

                if (record.HadBaseColor)
                {
                    propertyBlock.SetColor(BaseColorId, record.OriginalBaseColor);
                }
                if (record.HadLegacyColor)
                {
                    propertyBlock.SetColor(LegacyColorId, record.OriginalLegacyColor);
                }

                record.Renderer.SetPropertyBlock(propertyBlock);
            }

            selectedRenderers.Clear();
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
}
