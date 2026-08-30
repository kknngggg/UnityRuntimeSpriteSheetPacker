using kknngggg.Unity.Sprites.Errors;
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

            Assert.That(settings.Validate() == PackingError.None);
        }

        [Test]
        public void Validate_NegativePadding_ReturnsInvalidPadding()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = -1,
                MaxSize = 64,
                ForcePowerOfTwo = true,
            };

            PackingError error = settings.Validate();
            Assert.IsNotNull(error);
            Assert.AreEqual(PackingErrorCodes.INVALID_PADDING, error.Code);
        }

        [Test]
        public void Validate_MaxSizeBelowOne_ReturnsInvalidMaxSize()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = 0,
                MaxSize = 0,
                ForcePowerOfTwo = true,
            };

            PackingError error = settings.Validate();
            Assert.IsNotNull(error);
            Assert.AreEqual(PackingErrorCodes.INVALID_MAX_SIZE, error.Code);
        }

        [Test]
        public void EffectiveMaxSize_WithoutForcePowerOfTwo_IsMaxSize()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = 0,
                MaxSize = 5,
                ForcePowerOfTwo = false,
            };

            Assert.AreEqual(5, settings.EffectiveMaxSize);
        }

        [Test]
        public void EffectiveMaxSize_WithForcePowerOfTwo_IsLargestPowerOfTwoAtMostMaxSize()
        {
            SpriteSheet.PackingSettings settings = new SpriteSheet.PackingSettings {
                Padding = 0,
                MaxSize = 5,
                ForcePowerOfTwo = true,
            };

            Assert.AreEqual(4, settings.EffectiveMaxSize);
            Assert.AreEqual(1, SpriteSheet.PackingSettings.LargestPowerOfTwoAtMost(1));
            Assert.AreEqual(8, SpriteSheet.PackingSettings.LargestPowerOfTwoAtMost(8));
            Assert.AreEqual(8, SpriteSheet.PackingSettings.LargestPowerOfTwoAtMost(15));
            Assert.AreEqual(2048, SpriteSheet.PackingSettings.LargestPowerOfTwoAtMost(2048));
        }
    }
}
