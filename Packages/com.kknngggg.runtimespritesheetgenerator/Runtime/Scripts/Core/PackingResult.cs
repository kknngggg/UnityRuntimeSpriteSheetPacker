using kknngggg.Unity.Sprites.Errors;

namespace kknngggg.Unity.Sprites
{
    public readonly struct PackingResult
    {
        public SpriteSheet SpriteSheet { get; }
        public PackingError Error { get; }

        public bool IsSuccess => this.Error == PackingError.None;

        private PackingResult(SpriteSheet spriteSheet, PackingError error)
        {
            this.SpriteSheet = spriteSheet;
            this.Error = error;
        }

        public static PackingResult Success(SpriteSheet spriteSheet)
        {
            return new PackingResult(spriteSheet, PackingError.None);
        }

        public static PackingResult Failed(PackingError error)
        {
            return new PackingResult(null, error);
        }
    }
}
