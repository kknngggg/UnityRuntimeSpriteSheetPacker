using System;
using kknngggg.Unity.Sprites.Errors;
using UnityEngine;

namespace kknngggg.Unity.Sprites
{
    public sealed partial class SpriteSheet
    {
        [Serializable]
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

            public readonly int EffectiveMaxSize =>
                this.ForcePowerOfTwo ? LargestPowerOfTwoAtMost(this.MaxSize) : this.MaxSize;

            internal readonly PackingError Validate()
            {
                if (this.Padding < 0)
                {
                    return new PackingError(PackingErrorCodes.INVALID_PADDING,
                                            "padding must be >= 0");
                }

                if (this.MaxSize < 1)
                {
                    return new PackingError(PackingErrorCodes.INVALID_MAX_SIZE,
                                            "maxSize must be >= 1");
                }

                return PackingError.None;
            }

            internal static int LargestPowerOfTwoAtMost(int value)
            {
                int next = Mathf.NextPowerOfTwo(Mathf.Max(1, value));
                return next > value ? next >> 1 : next;
            }
        }
    }
}
