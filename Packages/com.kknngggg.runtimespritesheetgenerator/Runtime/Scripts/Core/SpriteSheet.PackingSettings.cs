using System;
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

            internal readonly int EffectiveMaxSize =>
                this.ForcePowerOfTwo ? LargestPowerOfTwoAtMost(this.MaxSize) : this.MaxSize;

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

            internal static int LargestPowerOfTwoAtMost(int value)
            {
                int next = Mathf.NextPowerOfTwo(Mathf.Max(1, value));
                return next > value ? next >> 1 : next;
            }
        }
    }
}
