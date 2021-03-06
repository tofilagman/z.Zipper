﻿using System;
using System.Text;
using System.IO;
using LZ4;
using SevenZip;
using System.IO.Compression;
using System.Reflection;

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
            using (var fs = new FileStream(fpath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[1024];
                int bytesread = 0;
                while ((bytesread = fs.Read(buffer, 0, buffer.Length)) > 0)
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
            }
            catch (Exception ex)
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
            using (GZipStream str = new GZipStream(outFile, System.IO.Compression.CompressionMode.Compress, false))
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
            using (GZipStream str = new GZipStream(outFile, System.IO.Compression.CompressionMode.Compress, false))
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
            using (GZipStream zipStream = new GZipStream(inFile, System.IO.Compression.CompressionMode.Decompress, true))
                while (DecompressFile(sDir, zipStream, progress)) ;
        }

        //Seven Zip 
        /*
         *   Extract 
         *    var extractor = new SevenZipExtractor(sCompressFile);
         *   extractor.Extracting += new EventHandler<ProgressEventArgs>(extr_Extracting);
         *   extractor.FileExtractionStarted += new EventHandler<FileInfoEventArgs>(extr_FileExtractionStarted);
         *   extractor.FileExists += new EventHandler<FileOverwriteEventArgs>(extr_FileExists);
         *   extractor.ExtractionFinished += new EventHandler<EventArgs>(extr_ExtractionFinished);
         *   extractor.BeginExtractArchive(sDir);
         */

        /*
         * Compress  
         *  SevenZipCompressor cmp = new SevenZipCompressor();
         * cmp.Compressing += new EventHandler<ProgressEventArgs>(cmp_Compressing);
         * cmp.FileCompressionStarted += new EventHandler<FileNameEventArgs>(cmp_FileCompressionStarted);
         * cmp.CompressionFinished += new EventHandler<EventArgs>(cmp_CompressionFinished);
         * cmp.ArchiveFormat = (OutArchiveFormat)Enum.Parse(typeof(OutArchiveFormat), cb_Format.Text);
         * cmp.CompressionLevel = (CompressionLevel)slider_Level.Value;
         * string directory = tb_CompressFolder.Text;
         * string archFileName = tb_CompressArchive.Text;
         * cmp.BeginCompressDirectory(directory, archFileName);  
         */


    }
}
