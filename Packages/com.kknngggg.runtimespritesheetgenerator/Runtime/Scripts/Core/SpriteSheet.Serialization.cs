using System;

namespace kknngggg.Unity.Sprites
{
    public sealed partial class SpriteSheet
    {
        public byte[] ToBytes()
        {
            ThrowIfDisposed();
            return SpriteSheetBinary.Serialize(this);
        }

        public static SpriteSheet Load(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return SpriteSheetBinary.Deserialize(data);
        }
    }
}
