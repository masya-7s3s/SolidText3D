using UnityEngine;

namespace MasaChuang.SolidText3D
{
    internal readonly struct TextStateRequest
    {
        public TextStateRequest(
            long version,
            DisplayResultSignature signature,
            string text,
            byte[] fontData,
            MeshGenerationParams generationParams,
            bool outlineEnabled,
            float outlineOffset,
            float outlineThickness,
            Material outlineMaterial,
            OutlineDisplayMode outlineDisplayMode,
            ObjectMode objectMode)
        {
            Version = version;
            Signature = signature;
            Text = text ?? string.Empty;
            FontData = fontData;
            GenerationParams = generationParams;
            OutlineEnabled = outlineEnabled;
            OutlineOffset = outlineOffset;
            OutlineThickness = outlineThickness;
            OutlineMaterial = outlineMaterial;
            OutlineDisplayMode = outlineDisplayMode;
            ObjectMode = objectMode;
        }

        public long Version { get; }

        public DisplayResultSignature Signature { get; }

        public string Text { get; }

        public byte[] FontData { get; }

        public MeshGenerationParams GenerationParams { get; }

        public bool OutlineEnabled { get; }

        public float OutlineOffset { get; }

        public float OutlineThickness { get; }

        public Material OutlineMaterial { get; }

        public OutlineDisplayMode OutlineDisplayMode { get; }

        public ObjectMode ObjectMode { get; }

        public OutlineSettings CreateOutlineSettings()
        {
            return new OutlineSettings
            {
                Enabled = OutlineEnabled,
                OffsetAmount = OutlineOffset,
                Thickness = OutlineThickness,
                Material = OutlineMaterial,
                DisplayMode = OutlineDisplayMode
            };
        }
    }
}