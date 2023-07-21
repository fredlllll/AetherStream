namespace AetherStream
{
    public enum AetherDataType : byte
    {
        NULL = 0,

        INT8 = 1,
        INT16 = 2,
        INT32 = 3,
        INT64 = 4,
        UINT8 = 5,
        UINT16 = 6,
        UINT32 = 7,
        UINT64 = 8,

        FLOAT32 = 9,
        FLOAT64 = 10,

        TRUE = 11,
        FALSE = 12,

        STRING = 13,
        BYTES = 14,

        DICTIONARY = 15,
        LIST = 16,
    }
}
