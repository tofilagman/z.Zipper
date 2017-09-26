using System;
using System.Text;
using System.IO;
using LZ4;
using SevenZip;
using System.IO.Compression;

namespace z.Zipper
{
    /// <summary>
    /// LJ 20160218
    /// Implements LZ4 Compressor
    /// </summary>
    public static class Zip
    { 
        /// <summary>
        /// Message Handler
        /// </summary>
        /// <param name="sMessage"></param>
        public delegate void ProgressDelegate(string sMessage);

        /// <summary>
        /// Compress File 
        /// </summary>
        /// <param name="sDir"></param>
        /// <param name="sRelativePath"></param>
        /// <param name="zipStream"></param>
        public static void CompressFile(string sDir, string sRelativePath, Stream zipStream)
        {
            //Compress file name
            char[] chars = sRelativePath.ToCharArray();
            zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
            foreach (char c in chars)
                zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));

            //Compress file content
            //byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
            //zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
            //zipStream.Write(bytes, 0, bytes.Length);
            WriteBytes(Path.Combine(sDir, sRelativePath), zipStream);
        }

        /// <summary>
        /// Write Bytes
        /// </summary>
        /// <param name="fpath"></param>
        /// <param name="zipStream"></param>
        public static void WriteBytes(string fpath, Stream zipStream)
        {
            using(var fs = new FileStream(fpath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[1024];
                int bytesread = 0;
                while((bytesread = fs.Read(buffer, 0 , buffer.Length)) > 0)
                {
                    zipStream.Write(BitConverter.GetBytes(bytesread), 0, sizeof(int));
                    zipStream.Write(buffer, 0, bytesread);
                }
            }
        }

        /// <summary>
        /// Decompress File
        /// </summary>
        /// <param name="sDir"></param>
        /// <param name="zipStream"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static bool DecompressFile(string sDir, Stream zipStream, ProgressDelegate progress)
        {
            try
            {
                //Decompress file name
                byte[] bytes = new byte[sizeof(int)];
                int Readed = zipStream.Read(bytes, 0, sizeof(int));
                if (Readed < sizeof(int))
                    return false;

                int iNameLen = BitConverter.ToInt32(bytes, 0);
                bytes = new byte[sizeof(char)];
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < iNameLen; i++)
                {
                    zipStream.Read(bytes, 0, sizeof(char));
                    char c = BitConverter.ToChar(bytes, 0);
                    sb.Append(c);
                }
                string sFileName = sb.ToString();
                if (progress != null)
                    progress(sFileName);

                //Decompress file content
                bytes = new byte[sizeof(int)];
                zipStream.Read(bytes, 0, sizeof(int));
                int iFileLen = BitConverter.ToInt32(bytes, 0);

                bytes = new byte[iFileLen];
                zipStream.Read(bytes, 0, bytes.Length);

                string sFilePath = Path.Combine(sDir, sFileName);
                string sFinalDir = Path.GetDirectoryName(sFilePath);
                if (!Directory.Exists(sFinalDir))
                    Directory.CreateDirectory(sFinalDir);

                using (FileStream outFile = new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    outFile.Write(bytes, 0, iFileLen);

                return true;
            }catch(Exception ex)
            {
                throw ex;
            }
            finally
            {

            }
        }

        /// <summary>
        /// Compress Directory
        /// </summary>
        /// <param name="sInDir"></param>
        /// <param name="sOutFile"></param>
        /// <param name="progress"></param>
        public static void CompressDirLZ4(string sInDir, string sOutFile, ProgressDelegate progress = null)
        {
            string[] sFiles = Directory.GetFiles(sInDir, "*.*", SearchOption.AllDirectories);
            int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar ? sInDir.Length : sInDir.Length + 1;

            using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (LZ4Stream str = new LZ4Stream(outFile, LZ4StreamMode.Compress, false))
                foreach (string sFilePath in sFiles)
                {
                    string sRelativePath = sFilePath.Substring(iDirLen);
                    if (progress != null)
                        progress(sRelativePath);
                    CompressFile(sInDir, sRelativePath, str);
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RootDir"></param>
        /// <param name="sFiles"></param>
        /// <param name="sOutFile"></param>
        /// <param name="progress"></param>
        public static void CompressDirLZ4(string RootDir, string[] sFiles, string sOutFile, ProgressDelegate progress = null)
        {
            using (FileStream outFile = new FileStream(sOutFile, FileMode.Append, FileAccess.Write, FileShare.None))
            using (LZ4Stream str = new LZ4Stream(outFile, LZ4StreamMode.Compress, false))
            {
                 
                foreach (string sFilePath in sFiles)
                {
                    progress?.Invoke(sFilePath);
                    CompressFile(RootDir, sFilePath, str);
                }
            }
        }

        /// <summary>
        /// Compress Using GZIP
        /// </summary>
        /// <param name="sInDir"></param>
        /// <param name="sOutFile"></param>
        /// <param name="progress"></param>
        public static void CompressDirGZip(string sInDir, string sOutFile, ProgressDelegate progress = null)
        {
            string[] sFiles = Directory.GetFiles(sInDir, "*.*", SearchOption.AllDirectories);
            int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar ? sInDir.Length : sInDir.Length + 1;

            using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress, false))
                foreach (string sFilePath in sFiles)
                {
                    string sRelativePath = sFilePath.Substring(iDirLen);
                    if (progress != null)
                        progress(sRelativePath);
                    CompressFile(sInDir, sRelativePath, str);
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RootDir"></param>
        /// <param name="sFiles"></param>
        /// <param name="sOutFile"></param>
        /// <param name="progress"></param>
        public static void CompressDirGZip(string RootDir, string[] sFiles, string sOutFile, ProgressDelegate progress = null)
        {
            using (FileStream outFile = new FileStream(sOutFile, FileMode.Append, FileAccess.Write, FileShare.None))
            using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress, false))
                foreach (string sFilePath in sFiles)
                {
                    progress?.Invoke(sFilePath);
                    CompressFile(RootDir, sFilePath, str);
                }
        }

        /// <summary>
        /// Decompress Directory
        /// </summary>
        /// <param name="sCompressedFile"></param>
        /// <param name="sDir"></param>
        /// <param name="progress"></param>
        public static void DecompressToDirLZ4(string sCompressedFile, string sDir, ProgressDelegate progress = null)
        {
            using (FileStream inFile = new FileStream(sCompressedFile, FileMode.Open, FileAccess.Read, FileShare.None))
            using (LZ4Stream zipStream = new LZ4Stream(inFile, LZ4StreamMode.Decompress, true))
                while (DecompressFile(sDir, zipStream, progress)) ;
        }

        /// <summary>
        /// Extract Using GZip
        /// </summary>
        /// <param name="sCompressedFile"></param>
        /// <param name="sDir"></param>
        /// <param name="progress"></param>
        public static void DecompressToDirGZip(string sCompressedFile, string sDir, ProgressDelegate progress = null)
        {
            using (FileStream inFile = new FileStream(sCompressedFile, FileMode.Open, FileAccess.Read, FileShare.None))
            using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
                while (DecompressFile(sDir, zipStream, progress)) ;
        }

        /// <summary>
        /// Compress using LZMA
        /// </summary>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        public static void CompressFileLZMA(string inFile, string outFile)
        {
            Int32 dictionary = 1 << 23;
            Int32 posStateBits = 2;
            Int32 litContextBits = 3; // for normal files
            // UInt32 litContextBits = 0; // for 32-bit data
            Int32 litPosBits = 0;
            // UInt32 litPosBits = 2; // for 32-bit data
            Int32 algorithm = 2;
            Int32 numFastBytes = 128;

            string mf = "bt4";
            bool eos = true;
            bool stdInMode = false;


            CoderPropID[] propIDs =  {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder,
                CoderPropID.EndMarker
            };

            object[] properties = {
                (Int32)(dictionary),
                (Int32)(posStateBits),
                (Int32)(litContextBits),
                (Int32)(litPosBits),
                (Int32)(algorithm),
                (Int32)(numFastBytes),
                mf,
                eos
            };

            using (FileStream inStream = new FileStream(inFile, FileMode.Open))
            {
                using (FileStream outStream = new FileStream(outFile, FileMode.Create))
                {
                    SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
                    encoder.SetCoderProperties(propIDs, properties);
                    encoder.WriteCoderProperties(outStream);
                    Int64 fileSize;
                    if (eos || stdInMode)
                        fileSize = -1;
                    else
                        fileSize = inStream.Length;
                    for (int i = 0; i < 8; i++)
                        outStream.WriteByte((Byte)(fileSize >> (8 * i)));
                    encoder.Code(inStream, outStream, -1, -1, null);
                }
            }

        }

        /// <summary>
        /// Decompress Using LZMA
        /// </summary>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        public static void DecompressFileLZMA(string inFile, string outFile)
        {
            using (FileStream input = new FileStream(inFile, FileMode.Open))
            {
                using (FileStream output = new FileStream(outFile, FileMode.Create))
                {
                    SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

                    byte[] properties = new byte[5];
                    if (input.Read(properties, 0, 5) != 5)
                        throw (new Exception("input .lzma is too short"));
                    decoder.SetDecoderProperties(properties);

                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = input.ReadByte();
                        if (v < 0)
                            throw (new Exception("Can't Read 1"));
                        outSize |= ((long)(byte)v) << (8 * i);
                    }
                    long compressedSize = input.Length - input.Position;

                    decoder.Code(input, output, compressedSize, outSize, null);
                }
            }
        }
    }
}
