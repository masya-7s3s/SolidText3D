using System;

namespace MasaChuang.SolidText3D
{
    internal readonly struct DisplayResultSignature : IEquatable<DisplayResultSignature>
    {
        public DisplayResultSignature(string text, int hashCode, int layoutHash, int fontSourceId, ObjectMode objectMode)
        {
            Text = text ?? string.Empty;
            HashCode = hashCode;
            LayoutHash = layoutHash;
            FontSourceId = fontSourceId;
            ObjectMode = objectMode;
        }

        public DisplayResultSignature(int hashCode, int fontSourceId, ObjectMode objectMode)
            : this(string.Empty, hashCode, hashCode, fontSourceId, objectMode)
        {
        }

        public string Text { get; }

        public int HashCode { get; }

        public int LayoutHash { get; }

        public int FontSourceId { get; }

        public ObjectMode ObjectMode { get; }

        public bool Equals(DisplayResultSignature other)
        {
            return string.Equals(Text, other.Text, StringComparison.Ordinal)
                && HashCode == other.HashCode
                && LayoutHash == other.LayoutHash
                && FontSourceId == other.FontSourceId
                && ObjectMode == other.ObjectMode;
        }

        public override bool Equals(object obj)
        {
            return obj is DisplayResultSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Text != null ? Text.GetHashCode() : 0;
                hash = (hash * 397) ^ HashCode;
                hash = (hash * 397) ^ LayoutHash;
                hash = (hash * 397) ^ FontSourceId;
                hash = (hash * 397) ^ (int)ObjectMode;
                return hash;
            }
        }

        public bool CanReusePerCharacterLayoutWith(DisplayResultSignature other)
        {
            return ObjectMode == ObjectMode.PerCharacter
                && other.ObjectMode == ObjectMode.PerCharacter
                && FontSourceId == other.FontSourceId
                && LayoutHash == other.LayoutHash
                && Text.Length == other.Text.Length;
        }

        public static bool operator ==(DisplayResultSignature left, DisplayResultSignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DisplayResultSignature left, DisplayResultSignature right)
        {
            return !left.Equals(right);
        }
    }
}