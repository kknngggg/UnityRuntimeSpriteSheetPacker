using System;

namespace kknngggg.Unity.Sprites
{
    public partial class SpriteSheet
    {
        public struct PackingSettings
        {
            public int Padding;
            public int MaxSize;
            public bool ForcePowerOfTwo;

            public static PackingSettings Default => new() {
                Padding = 1,
                MaxSize = 2048,
                ForcePowerOfTwo = true,
            };

            internal readonly void Validate()
            {
                if (this.Padding < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.Padding), "padding must be >= 0");
                }

                if (this.MaxSize < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.MaxSize), "maxSize must be >= 1");
                }
            }
        }
    }
}
