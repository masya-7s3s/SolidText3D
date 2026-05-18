using System.Collections.Generic;

namespace MasaChuang.SolidText3D
{
    internal sealed class PreparedDisplayResult
    {
        public long Version { get; set; }

        public DisplayResultSignature Signature { get; set; }

        public GlyphMeshData BodyMeshData { get; set; }

        public GlyphMeshData OutlineMeshData { get; set; }

        public List<GlyphMeshData> PerCharacterMeshData { get; set; }

        public List<UnityEngine.Vector3> PerCharacterOffsets { get; set; }

        public bool ClearsDisplay { get; set; }
    }
}