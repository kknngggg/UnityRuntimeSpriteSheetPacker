using System;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public readonly struct SelectedFileInfo : IEquatable<SelectedFileInfo>
    {
        public SelectedFileInfo(string fullPath)
        {
            this.FullPath = fullPath ?? string.Empty;
        }

        public string FullPath { get; }

#region NullObject

        public static SelectedFileInfo Null => new(string.Empty);

#endregion

#region IEquatable<SelectedFileInfo>

        public bool Equals(SelectedFileInfo other)
        {
            return this.FullPath == other.FullPath;
        }

        public override bool Equals(object obj)
        {
            return obj is SelectedFileInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return this.FullPath.GetHashCode();
        }

        public static bool operator ==(SelectedFileInfo left, SelectedFileInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SelectedFileInfo left, SelectedFileInfo right)
        {
            return left.Equals(right) == false;
        }

#endregion // IEquatable<SelectedFileInfo>

    }
}
