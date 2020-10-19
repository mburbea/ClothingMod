namespace ClothingMod
{
    public class SimTypeAsset
    {
        internal static readonly byte[] Bundle2Bytes = new byte[23]{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x45, 0xAB, 0x1A, 0x00, 0x00, 0x00, 0x00 };

        public uint Id { get; }
        public string Name { get; }
        public byte[] Bytes { get; }
        public byte[] BundleBytes { get; }
        public uint Bundle2Id => 0x20_00_00_00 | Id;
        // used by assetinfos
        public uint BundleId => 0xF0_00_00_00 | Id;

        public SimTypeAsset(string name, byte[] bytes, byte[] bundleBytes) => (Id, Name, Bytes, BundleBytes) = ((uint)Hasher.GetHash(name), name, bytes, bundleBytes);


    }
}
