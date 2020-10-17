namespace ClothingMod
{
    public class SimTypeAsset
    {
        internal static readonly byte[] Bundle2Bytes = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x45, 0xAB, 0x1A, 0x00, 0x00, 0x00, 0x00 };

        public int Id { get; }
        public string Name { get; }
        public byte[] Bytes { get; }
        public byte[] BundleBytes { get; }
        public int Bundle2Id => 0x20_00_00_00 | Id;
        // used by assetinfos
        public int BundleId => unchecked((int)0xF0_00_00_00) | Id;

        public SimTypeAsset(string name, byte[] bytes, byte[] bundleBytes) => (Id, Name, Bytes, BundleBytes) = (Hasher.GetHash(name), name, bytes, bundleBytes);


    }
}
