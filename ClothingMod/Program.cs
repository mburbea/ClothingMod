using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ClothingMod
{
    internal class Program : IDisposable
    {
        private static readonly string ScratchDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private static readonly string SimTypeInit = Path.Combine(ScratchDir, "simtype_init.bin");
        private static readonly string KsmtBatch = Path.Combine(ScratchDir, "134225858_ksmt.batch");
        private static readonly string AssetInfosBin = Path.Combine(ScratchDir, "assetinfos.bin");
        private static readonly string LocalizationBin = Path.Combine(ScratchDir, "locatexts_en_us.koalocatext_base");

        private static readonly byte[][] FabData = typeof(Fab).GetEnumNames()
            .Select(name => Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(Program).Namespace}.{name.ToLowerInvariant()}.simtype_bxml").ReadAllBytes())
            .ToArray();

        private static readonly Dictionary<string, uint> FabDictByName = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(Program).Namespace}.fab.csv")
            .ReadAllLines()
            .Skip(1)
            .Select(line => line.Split(','))
            .ToDictionary(res => res[1], res => uint.Parse(res[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);

        private static List<(ulong, string)> Localization = new List<(ulong, string)>();



        public static void InitLocalization()
        {
            ReadOnlySpan<byte> data = File.ReadAllBytes(LocalizationBin);
            var count = data.Read<uint>(0);

            var sizes = new List<int>();
            var begin = data.Read<int>(4);
            var end = 0;

            int offset = 8;

            for (uint i = 0; i < count - 1; i++)
            {
                end = data.Read<int>(offset);
                offset += 4;
                sizes.Add(end - begin - 1);
                begin = end;
            }

            sizes.Add(data.Length - begin - 1);

            for (uint i = 0; i < count; i++)
            {
                Localization.Add((data.Read<ulong>(offset), ""));
                offset += 8;
            }

            for (int i = 0; i < count; i++)
            {
                offset++;
                Localization[i] = (Localization[i].Item1, Encoding.UTF8.GetString(data.Slice(offset, sizes[i] - 1).ToArray()));
                offset += sizes[i];
            }
            return;
        }

        public static void WriteLocalization()
        {
            File.Delete(LocalizationBin);
            var stream = new FileStream(LocalizationBin, FileMode.Create);
            var writer = new BinaryWriter(stream);

            writer.Write(Localization.Count);

            int begin = 4 + (Localization.Count * (4 + 8));
            writer.Write(begin);
            
            for (int i = 1; i < Localization.Count; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(Localization[i - 1].Item2);
                begin += (1 + bytes.Length + 1);
                writer.Write(begin);
            }

            for (int i = 0; i < Localization.Count; i++)
            {
                writer.Write(Localization[i].Item1);
            }

            for (int i = 0; i < Localization.Count; i++)
            {
                writer.Write((byte)17);
                writer.Write(Encoding.UTF8.GetBytes(Localization[i].Item2));
                writer.Write((byte)0);
            }

            writer.Close();
            stream.Close();
            return;
        }

        public static ulong AddLocalizationText(string text)
        {
            ulong key = 0;
            if (Localization.Count > 0)
            {
                key = Localization.Last().Item1 + 1;
            }
            Localization.Add((key, text));
            return key;
        }

        public Program()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            if (Debugger.IsAttached)
            {
                Directory.SetCurrentDirectory(Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\common\Kingdoms of Amalur Re-Reckoning\modding\")));
            }
            Directory.CreateDirectory(ScratchDir);
        }

        private readonly SimTypeAsset[] _assets = new []
        {
            BuildTable("splinter_02_melee_Set_Unique_", "splinter_02_melee_Set_Unique_f_","glowing_warrior_", "Glowing Warrior "),
            BuildTable("splinter_03_Rogue_Set_Unique_", "splinter_03_Rogue_Set_Unique_f_", "glowing_rogue_", "Glowing Rogue "),

            BuildTable("01_generic_male_peasant_", "01_generic_peasant_female_", "Clothing_peasant03_", "Generic Peasant "),
            BuildTable("01_Dokkalfar_noble_male_", "01_Dokkalfar_noble_female_", "Clothing_peasant04_", "Dokkalfar Noble "),
            BuildTable("02_Dokkalfar_noble_male_", "02_Dokkalfar_noble_female_", "Clothing_peasant05_", "Dokkalfar Alt Noble "),
            BuildTable("01_Dokkalfar_peasant_male_", "01_Dokkalfar_peasant_female_", "Clothing_peasant06_", "Dokkalfar Peasant "),
            BuildTable("02_Dokkalfar_peasant_male_", "02_Dokkalfar_peasant_female_", "Clothing_peasant07_", "Dokkalfar Alt Peasant "),
            BuildTable("01_Dokkalfar_merchant_male_", "01_Dokkalfar_merchant_female_", "Clothing_peasant08_", "Dokkalfar Merchant "),
            BuildTable("02_Dokkalfar_merchant_male_", "02_Dokkalfar_merchant_female_", "Clothing_peasant09_", "Dokkalfar Alt Merchant "),

            BuildTable("01_Ljosalfar_noble_male_", "01_Ljosalfar_noble_female_", "Clothing_peasant10_", "Ljosalfar Noble "),
            BuildTable("02_Ljosalfar_noble_male_", "02_Ljosalfar_noble_female_", "Clothing_peasant11_", "Ljosalfar Alt Noble "),
            BuildTable("01_Ljosalfar_peasant_male_", "01_Ljosalfar_peasant_female_", "Clothing_peasant12_", "Ljosalfar Peasant "),
            BuildTable("02_Ljosalfar_peasant_male_", "02_Ljosalfar_peasant_female_", "Clothing_peasant13_", "Ljosalfar Alt Peasant "),
            BuildTable("01_Ljosalfar_merchant_male_", "01_Ljosalfar_Merchant_f_", "Clothing_peasant14_", "Ljosalfar Merchant "),
            BuildTable("02_Ljosalfar_merchant_male_", "02_Ljosalfar_Merchant_f_", "Clothing_peasant15_", "Ljosalfar Alt Merchant "),

            BuildTable("01_almain_merchant_male_", "01_almain_Merchant_female_", "Clothing_peasant16_", "Almain Merchant "),
            BuildTable("02_almain_merchant_male_", "02_almain_Merchant_female_", "Clothing_peasant17_", "Almain Alt Merchant "),
            BuildTable("01_varani_merchant_male_", "01_varani_Merchant_female_", "Clothing_peasant18_", "Verani Merchant "),
            BuildTable("02_varani_merchant_male_", "02_varani_Merchant_female_", "Clothing_peasant19_", "Verani Alt Merchant "),
            BuildTable("01_almain_noble_male_", "01_almain_noble_female_", "Clothing_peasant20_", "Almain Noble "),
            BuildTable("02_almain_noble_male_", "02_almain_noble_female_", "Clothing_peasant21_", "Almain Alt Noble "),
            BuildTable("01_varani_noble_male_", "01_varani_noble_female_", "Clothing_peasant22_", "Verani Noble "),
            BuildTable("02_varani_noble_male_", "02_varani_noble_female_", "Clothing_peasant23_", "Verani Alt Noble "),
        }.SelectMany(x => x).ToArray();

        private static SimTypeAsset CreateModifiedSimType(Fab fab, string maleName, string femaleName, string baseSimName, string displayName)
        {
            var bytes = FabData[(int)fab].ToArray();
            var ix = bytes.AsSpan().IndexOf(new byte[8] { 0x06, 0, 0, 0, 0x07, 0, 0, 0 });
            var modified = false;
            if (FabDictByName.TryGetValue($"{maleName}{fab}", out uint maleId))
            {
                modified = true;
                bytes.Write(ix + 28, maleId);
            }
            if (FabDictByName.TryGetValue($"{femaleName}{fab}", out uint femaleId))
            {
                modified = true;
                bytes.Write(ix + 32, femaleId);
            }
            if (!modified)
            {
                return null;
            }
            maleId = bytes.Read<uint>(ix + 28);
            femaleId = bytes.Read<uint>(ix + 32);
            var name = $"{baseSimName}{fab}";
            var bundleBytes = new byte[30];
            bundleBytes.Write(8, 2);
            bundleBytes.Write(16, maleId);
            bundleBytes.Write(20, femaleId);
            bundleBytes.Write(24, 0x00_00_10_10);
            return new SimTypeAsset(name, $"{displayName}{(FabDisplayName)fab}", bytes, bundleBytes);
        }

        private void PostProcessAssets()
        {
            ReadOnlySpan<byte> bytes = File.ReadAllBytes(KsmtBatch);
            int entryCount = bytes.Read<int>(0);
            uint lastID = bytes.Read<uint>(4 + ((entryCount - 1) * 8));
            foreach (var asset in _assets)
            {
                lastID++;
                asset.Id = lastID;
                asset.Bytes.Write<ulong>(8, AddLocalizationText(asset.DisplayName));
            }
        }

        private void BuildSimtypeInit()
        {
            ReadOnlySpan<byte> data = File.ReadAllBytes(SimTypeInit);
            var buffer = new byte[data.Length + _assets.Length * 8];
            var currentLen = data.Read<int>(4);
            var listSize = currentLen * 4;
            var eol1 = listSize + 8;
            var eol2 = eol1 + _assets.Length * 4 + listSize;
            data[..eol1].CopyTo(buffer);
            data[eol1..].CopyTo(buffer.AsSpan(eol1 + _assets.Length * 4));
            buffer.Write(4, currentLen + _assets.Length);

            foreach (var asset in _assets)
            {
                buffer.Write(eol1, asset.Id);
                buffer.Write(eol2, asset.Hash);
                eol1 += 4;
                eol2 += 4;
            }
            File.WriteAllBytes(SimTypeInit, buffer);
        }

        private void BuildKsmtBatch()
        {
            ReadOnlySpan<byte> bytes = File.ReadAllBytes(KsmtBatch);
            int entryCount = bytes.Read<int>(0);
            var buffer = new byte[bytes.Length + _assets.Length * 8 + _assets.Sum(x => x.Bytes.Length)];
            var entriesOffset = 4 + 8 * entryCount;
            var fileOffset = entriesOffset + _assets.Length * 8;
            bytes[..entriesOffset].CopyTo(buffer);
            buffer.Write(0, entryCount + _assets.Length);
            var existingSimtypes = bytes[entriesOffset..];
            existingSimtypes.CopyTo(buffer.AsSpan(fileOffset));
            fileOffset += existingSimtypes.Length;
            foreach (var asset in _assets)
            {
                buffer.Write(entriesOffset, asset.Id);
                buffer.Write(entriesOffset + 4, asset.Bytes.Length);
                asset.Bytes.CopyTo(buffer, fileOffset);
                entriesOffset += 8;
                fileOffset += asset.Bytes.Length;
            }
            File.WriteAllBytes(KsmtBatch, buffer);
        }

        private void BuildAssetInfosBin()
        {
            ReadOnlySpan<byte> bytes = File.ReadAllBytes(AssetInfosBin);
            var fileIds = bytes.Read<int>(0);
            var buffer = new byte[bytes.Length + _assets.Sum(x => $"{x.Id}.simtype_bxml".Length + 6 + $"{x.Id}.bundle".Length + 6 + $"{x.Bundle2Id}.bundle".Length + 6)];
            var assetInfos = new (uint fileId, byte flag, byte len, byte[] name)[fileIds + _assets.Length * 3];
            var offset = 8;
            int i = 0;
            for (; i < fileIds; i++)
            {
                var fileId = bytes.Read<uint>(offset);
                var flag = bytes[offset + 4];
                var len = bytes[offset + 5];
                var name = bytes.Slice(offset + 6, len).ToArray();//.ToArray();
                assetInfos[i] = (fileId, flag, len, name);
                offset += 6 + len;
            }
            foreach (var (fileId, name) in _assets.SelectMany(a => new[] { (a.Id, $"{a.Id}.simtype_bxml"), (a.BundleId, $"{a.Id}.bundle"), (a.Bundle2Id, $"{a.Bundle2Id}.bundle") }))
            {
                var flag = (byte)(name.EndsWith("simtype_bxml") ? 148 : 156);
                var len = (byte)name.Length;

                assetInfos[i++] = (fileId, flag, len, Encoding.UTF8.GetBytes(name));
            }
            Array.Sort(assetInfos, (x, y) => x.fileId.CompareTo(y.fileId));
            var hashes = bytes[offset..];
            buffer.Write(0, fileIds + _assets.Length * 3);
            buffer.Write(4, bytes.Read<uint>(4));
            offset = 8;
            foreach (var (fileId, flag, len, name) in assetInfos)
            {
                buffer.Write(offset, fileId);
                buffer[offset + 4] = flag;
                buffer[offset + 5] = len;
                name.CopyTo(buffer, offset + 6);
                offset += 6 + name.Length;
            }
            hashes.CopyTo(buffer.AsSpan(offset));
            File.WriteAllBytes(AssetInfosBin, buffer);
        }

        private static void BackupOrRestore()
        {
            if (!File.Exists(@"..\data\patch_0.zip"))
            {
                using var zip = new ZipArchive(File.OpenWrite(@"..\data\patch_0.zip"), ZipArchiveMode.Create);
                zip.CreateEntryFromFile(@"..\data\patch_0.pak", "patch_0.pak");
            }
            else
            {
                File.Delete(@"..\data\patch_0.pak");
                ZipFile.ExtractToDirectory(@"..\data\patch_0.zip", @"..\data\");
            }
        }

        private static void Unpack()
        {
            Process.Start("pakfileunpacker.exe", @$"..\data\patch_0.pak unpack {ScratchDir}").WaitForExit();
            if (!File.Exists(SimTypeInit))
            {
                Process.Start($@"pakfileunpacker.exe", @$"..\data\initial_0.pak unpack {ScratchDir} simtype_init.bin").WaitForExit();
            }
        }

        private static void Pack()
        {
            var listPath = Path.GetTempFileName();
            File.WriteAllLines(listPath, Directory.EnumerateFiles(ScratchDir));
            Process.Start("pakfilebuilder.exe", @$"-c {listPath} ..\data\patch_0.pak").WaitForExit();
            File.Delete(listPath);
        }

        private static IEnumerable<SimTypeAsset> BuildTable(string maleName, string femaleName, string baseSimType, string displayName)
        => ((Fab[])typeof(Fab).GetEnumValues())
                .Select(x =>CreateModifiedSimType(x, maleName, femaleName, baseSimType, displayName))
                .Where(x => x != null);


        private void WriteBundles()
        {
            foreach (var item in _assets)
            {
                File.WriteAllBytes(Path.Combine(ScratchDir, $"{item.Id}.bundle"), item.BundleBytes);
                File.WriteAllBytes(Path.Combine(ScratchDir, $"{item.Bundle2Id}.bundle"), SimTypeAsset.Bundle2Bytes);
            }
        }

        private static void Main()
        {
            FabDictByName.Add("01_generic_peasant_female_feet", FabDictByName["01_generic_female_peasant_feet"]);
            using var program = new Program();
            if (!File.Exists("pakfileunpacker.exe"))
            {
                Console.WriteLine("pakfileunpacker.exe not found. Please make sure this is located in your Kingdoms of Amalur Re-Reckoning\\modding directory. Press any key to exit");
                Console.ReadKey();
                Environment.Exit(1);
            }
            program.Run();
        }

        private void Run()
        {
            BackupOrRestore();
            Unpack();
            InitLocalization();
            PostProcessAssets();
            BuildSimtypeInit();
            BuildKsmtBatch();
            BuildAssetInfosBin();
            WriteBundles();
            WriteLocalization();
            Pack();
        }

        public void Dispose() => Directory.Delete(ScratchDir, true);
    }
}
