namespace SupertonicTTSTrayApp.Core
{
    public class TTSConfig
    {
        public AEConfig AE { get; set; } = new();
        public TTLConfig TTL { get; set; } = new();

        public class AEConfig
        {
            public int SampleRate { get; set; }
            public int BaseChunkSize { get; set; }
        }

        public class TTLConfig
        {
            public int ChunkCompressFactor { get; set; }
            public int LatentDim { get; set; }
        }
    }

    public class Style
    {
        public float[] Ttl { get; set; }
        public long[] TtlShape { get; set; }
        public float[] Dp { get; set; }
        public long[] DpShape { get; set; }

        public Style(float[] ttl, long[] ttlShape, float[] dp, long[] dpShape)
        {
            Ttl = ttl;
            TtlShape = ttlShape;
            Dp = dp;
            DpShape = dpShape;
        }
    }
}
