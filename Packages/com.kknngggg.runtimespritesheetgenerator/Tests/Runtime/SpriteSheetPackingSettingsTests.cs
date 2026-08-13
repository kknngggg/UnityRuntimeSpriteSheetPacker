using System;
using NUnit.Framework;

namespace kknngggg.Unity.Sprites.Tests
{
    public class SpriteSheetPackingSettingsTests
    {
        [Test]
        public void Default_HasDocumentedValues()
        {
            SpriteSheet.PackingSettings settings = SpriteSheet.PackingSettings.Default;

            Assert.AreEqual(1, settings.Padding);
            Assert.AreEqual(2048, settings.MaxSize);
            Assert.IsTrue(settings.ForcePowerOfTwo);
        }

        [Test]
        public void Validate_AcceptsZeroPaddingAndPositiveMaxSize()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = 0,
                MaxSize = 1,
                ForcePowerOfTwo = false,
            };

            Assert.DoesNotThrow(settings.Validate);
        }

        [Test]
        public void Validate_NegativePadding_Throws()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = -1,
                MaxSize = 64,
                ForcePowerOfTwo = true,
            };

            ArgumentOutOfRangeException exception =
                Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
            Assert.AreEqual("Padding", exception.ParamName);
        }

        [Test]
        public void Validate_MaxSizeBelowOne_Throws()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = 0,
                MaxSize = 0,
                ForcePowerOfTwo = true,
            };

            ArgumentOutOfRangeException exception =
                Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
            Assert.AreEqual("MaxSize", exception.ParamName);
        }
    }
}
