using System;

namespace kknngggg.Unity.Sprites.Errors
{
    public readonly struct PackingError : IEquatable<PackingError>
    {
        public static PackingError None => new(PackingErrorCodes.NONE, null);

        public int Code { get; }
        public string Message { get; }

        public PackingError(int code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        public bool Equals(PackingError other)
        {
            return this.Code == other.Code;
        }

        public override bool Equals(object obj)
        {
            return obj is PackingError other && Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Code;
        }

        public static bool operator ==(PackingError left, PackingError right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PackingError left, PackingError right)
        {
            return left.Equals(right) == false;
        }
    }
}
