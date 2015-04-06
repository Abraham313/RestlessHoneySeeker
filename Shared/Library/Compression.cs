﻿using Ionic.Zip;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Library
{
    public class Compression
    {
        public static bool Zip(string file, string outputfile, string password = "")
        {
            bool result = false;
            using (ZipFile zip = new ZipFile())
            {
                if (password.Length > 0)
                    zip.Password = password;
                zip.AddFile(file, Path.GetFileName(file));
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zip.CompressionMethod = CompressionMethod.Deflate;
                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip.Save(outputfile);
                zip.ZipError += (o, e) =>
                {
                    result = false;
                };
                result = true;
            }
            return result;
        }

        public static bool Zip(string[] files, string outputfile, string password = "")
        {
            bool result = false;
            using (ZipFile zip = new ZipFile())
            {
                if (password.Length > 0)
                    zip.Password = password;
                zip.AddFiles(files);
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zip.CompressionMethod = CompressionMethod.Deflate;
                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip.Save(outputfile);
                zip.ZipError += (o, e) =>
                {
                    result = false;
                };
                result = true;
            }
            return result;
        }

        //public static byte[] Compress(
        //    string file,
        //    CompressionLevel compressionLevel = Ionic.Zlib.CompressionLevel.BestCompression,
        //    CompressionMethod compressionMethod = CompressionMethod.Deflate,
        //    EncryptionAlgorithm encryption = EncryptionAlgorithm.WinZipAes256,
        //    string password = "")
        //{
        //    return Compress(new string[] { file }, compressionLevel, compressionMethod, encryption, password);
        //}

        public static byte[] Compress(
            string[] files,
            //CompressionLevel compressionLevel = Ionic.Zlib.CompressionLevel.BestCompression,
            //CompressionMethod compressionMethod = CompressionMethod.Deflate,
            //EncryptionAlgorithm encryption = EncryptionAlgorithm.WinZipAes256,
            string password = "")
        {
            var memoryStream = new MemoryStream();
            using (var zip = new ZipFile())
            {
                CompressionLevel compressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                CompressionMethod compressionMethod = CompressionMethod.Deflate;
                EncryptionAlgorithm encryption = EncryptionAlgorithm.WinZipAes256;

                bool hasError = false;
                if (!String.IsNullOrEmpty(password))
                {
                    zip.Password = password;
                }
                zip.AddFiles(files);
                zip.CompressionLevel = compressionLevel;
                zip.CompressionMethod = compressionMethod;
                zip.Encryption = encryption;
                zip.FlattenFoldersOnExtract = true;
                zip.ZipError += (o, e) =>
                {
                    hasError = true;
                };
                if (hasError)
                    return null;
                zip.Save(memoryStream);
                //byte[] bytes = memoryStream.ToArray();
                //File.WriteAllBytes(@"C:\Users\benjamin\AeroFS\Visual Studio 2012\Projects\restless-honey-seeker\serverDotNet\Server\DataFromClient\" + DateTime.Now.Ticks + ".zip", bytes);
                //return bytes;
            }
            return memoryStream.ToArray();
        }
    }
}