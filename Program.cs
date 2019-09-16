using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.Cache;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;
using WolvenKit.Bundles;
using WolvenKit.Cache;
using System.IO.MemoryMappedFiles;
using WolvenKit.Common;
using WolvenKit.CR2W;
using Force.Crc32;

namespace W3_Zip
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Main
            //InterpretCL(args);
            #endregion



            GenerateHashes();
            //TestIO();
            //BalanceMapsDump();






        }

        private static void GenerateHashes()
        {
            string filesdlc = @"E:\moddingdir_tw3\DATA\Hashes\tw3_all_files_dlc.csv";
            string filesep1 = @"E:\moddingdir_tw3\DATA\Hashes\tw3_all_files_ep1.csv";
            string filesbob = @"E:\moddingdir_tw3\DATA\Hashes\tw3_all_files_bob.csv";
            string filesc0 = @"E:\moddingdir_tw3\DATA\Hashes\tw3_all_files_content0.csv";

            GenerateHashesForFile(filesdlc);
            GenerateHashesForFile(filesep1);
            GenerateHashesForFile(filesbob);
            GenerateHashesForFile(filesc0);
        }

        private static void GenerateHashesForFile(string filepath)
        {
            string newfile = filepath.Remove(filepath.Length - 4, 4); //trim the extension cos I'm lazy
            newfile = $"{newfile}.hash.csv";

            var alllines = File.ReadAllLines(filepath).ToList();
            alllines.RemoveAt(0);

            using (StreamWriter sw = new StreamWriter(newfile))
            {
                sw.WriteLine("RelativePath,FNV1a64Hash"); //header

                foreach (string line in alllines)
                {
                    var result = new List<string>();
                    var splits = line.Split(',').ToList();
                    if (splits.Count != 5)
                    {
                        throw new NotImplementedException();
                    }

                    //path to hash
                    string relPath = splits[1];
                    string hash = FNV1a.HashFNV1a64(relPath).ToString();

                    //result.Add(splits.First().Split('\\').Last());  //bundlename
                    result.Add(relPath);                            //relativepath
                    result.Add(hash);                               //hash

                    string res = string.Join(",", result);

                    sw.WriteLine(res);
                }
            }

        }

        private static void BalanceMapsDump()
        {
            DirectoryInfo indir = new DirectoryInfo(@"E:\_test\Balancemaps");

            var files = indir.GetFiles("*.xbm", SearchOption.AllDirectories).ToList();
            foreach (var f in files)
            {
                ConvertBalancemap(f);
            }
        }

        private static void TestIO()
        {
            DirectoryInfo di = new DirectoryInfo(@"E:\_test\texturecaches");
            string indir = Path.Combine(di.FullName, "_in");
            string extracted = Path.Combine(di.FullName, "_extracted");

            string reread = Path.Combine(di.FullName, "reread");
            string recreated = Path.Combine(di.FullName, "recreated");
            string reextracted = Path.Combine(di.FullName, "reextracted");



            // test reading
            string filename = Path.Combine(indir, "blob0.bundle");
            Bundle b = new Bundle(filename);
            b.Write(reread);

            // test creating from bundleitems
            Bundle brc = new Bundle(b.Items.ToArray());
            brc.Write(recreated);

            // test creating from physical files
            List<FileInfo> bundlefiles = new List<FileInfo>();
            DirectoryInfo extractedDI = new DirectoryInfo(extracted);
            foreach (var f in extractedDI.GetFiles("*", SearchOption.AllDirectories))
            {
                bundlefiles.Add(f);

                var name = f.FullName;
                /*using (var file = MemoryMappedFile.CreateFromFile(f.FullName, FileMode.Open))
                using (var vs = file.CreateViewStream(0, f.Length))
                using (var br = new BinaryReader(vs))
                {
                    var w2rc = new CR2WFile(br);
                }*/
            }
            Bundle bex = new Bundle(bundlefiles.ToArray(), extractedDI);
            bex.Write(reextracted);
        }

        private static void InterpretCL(string[] args)
        {
            if (args == null || !(args.Length > 0))
            {
                return;
            }

            if (args[0] == "Pack")
            {
                List<FileInfo> bundlefiles = new List<FileInfo>();
                List<FileInfo> bufferfiles = new List<FileInfo>();
                DirectoryInfo indir = new DirectoryInfo(args[1]);
                foreach (var f in indir.GetFiles("*", SearchOption.AllDirectories))
                {
                    if (f.Extension == ".buffer")
                        bufferfiles.Add(f);
                    else
                        bundlefiles.Add(f);
                }


                var blob = new Bundle(bundlefiles.ToArray(), indir);
                var buffer = new Bundle(bufferfiles.ToArray(), indir);
                blob.Write(indir.Parent.FullName);
                buffer.Write(indir.Parent.FullName);
            }
            if (args[0] == "Cook")
            {

            }
            if (args[0] == "Buildcache")
            {

            }
            else if (args[0] == "Unbundle")
            {
                if (File.Exists(args[1]))
                {
                    FileInfo infile = new FileInfo(args[1]);
                    DirectoryInfo indir = infile.Directory;
                    var b = new Bundle(infile.FullName);
                    foreach (var item in b.Items)
                    {
                        string outfile = Path.Combine(indir.FullName, item.DepotPath);
                        DirectoryInfo outdir = new FileInfo(outfile).Directory;
                        Directory.CreateDirectory(outdir.FullName);
                        item.Extract(outfile);
                    }
                }
            }

            else if (args[0] == "ExtractCache")
            {
                if (File.Exists(args[1]))
                {
                    FileInfo infile = new FileInfo(args[1]);
                    DirectoryInfo indir = infile.Directory;
                    var c = new TextureCache(infile.FullName);
                    foreach (var item in c.Items)
                    {
                        string outfile = Path.Combine(indir.FullName, item.DepotPath);
                        outfile.TrimEnd('m', 'b', 'x', '.');
                        outfile += ".dds";

                        DirectoryInfo outdir = new FileInfo(outfile).Directory;
                        Directory.CreateDirectory(outdir.FullName);
                        item.Extract(outfile);
                    }
                }
            }
            else if (args[0] == "Metadatastore")
            {

                DirectoryInfo indir = new DirectoryInfo(args[1]);
                var ms = new Metadata_Store(indir.FullName);
                ms.Write(indir.FullName);
            }
            else
            {

            }
        }

        private static void ConvertBalancemap(FileInfo fi)
        {
            //var tc = new TextureCache(cachePath);
            List<byte> buffer = new List<byte>();
            UInt16 width;
            UInt16 height;
            string filename = fi.Name;
            string filenameNoExt = filename.Trim(fi.Extension.ToCharArray());
            string outdir = @"E:\DumpedBalanceMaps";

            using (var ms = new MemoryStream(File.ReadAllBytes(fi.FullName)))
            using (var br = new BinaryReader(ms))
            {
                var cr2w = new CR2WFile(br);
                CR2WChunk imagechunk = cr2w.chunks[0];
                CByteArray image = ((CBitmapTexture)(imagechunk.data)).Image;

                buffer = image.Bytes.ToList();
                //conversion
                width = UInt16.Parse(imagechunk.GetVariableByName("width").ToString());
                height = UInt16.Parse(imagechunk.GetVariableByName("height").ToString());

                //remove some sort of header (28 bytes?)
                buffer.RemoveRange(0, 28);
            }

            //dbg
            if (buffer == null)
            {
                return;
            }
            byte[] magic = new byte[4];
            magic = buffer.Take(4).ToArray();
            if (magic[3] != 0xFF )
            {
                throw new NotImplementedException();
            }


            //writing
            string convertedpath = Path.Combine(outdir, $"{filenameNoExt}.tga");
            using (var fs = new FileStream(convertedpath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                //header
                bw.Write(new byte[4] { 0x00, 0x00, 0x02, 0x00 });
                bw.Write(new byte[8]);
                bw.Write((UInt16)width);
                bw.Write((UInt16)height);
                bw.Write(new byte[2] { 0x20, 0x28 });

                //data
                bw.Write(buffer.ToArray());
            }
        }
    }
}
