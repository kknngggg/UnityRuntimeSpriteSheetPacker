namespace kknngggg.Unity.Sprites.Errors
{
    public static class PackingErrorCodes
    {
        // SUCCESS
        public const int NONE = 0;

        // PackingSettings Errors
        public const int INVALID_PADDING = -1;
        public const int INVALID_MAX_SIZE = -2;

        // SpritePackingSheet General Errors
        public const int NULL_ENTRIES = -3;
        public const int EMPTY_ENTRIES = -4;
        public const int PACK_FAILED = -5;

        // SpritePackEntry Errors
        public const int NULL_TEXTURE = -6;
        public const int EMPTY_NAME = -7;
        public const int DUPLICATE_NAME = -8;
        public const int TEXTURE_NOT_READABLE = -9;
        public const int INVALID_PIXELS_PER_UNIT = -10;
        public const int TEXTURE_EXCEEDS_MAX_SIZE = -11;
    }
}
