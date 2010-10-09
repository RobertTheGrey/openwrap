﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using OpenFileSystem.IO;

namespace OpenWrap.Dependencies
{
    public static class PackageBuilder
    {
        public static void NewFromFiles(IFile destinationPackage, IEnumerable<PackageContent> content)
        {
            using (var wrapStream = destinationPackage.OpenWrite())
            using (var zipFile = new ZipOutputStream(wrapStream))
            {
                foreach(var contentFile in content)
                {
                    zipFile.PutNextEntry(GetZipEntry(contentFile));
                    
                    using (var contentStream = contentFile.Stream())
                        contentStream.CopyTo(zipFile);
                }
                zipFile.Finish();
            }
        }

        static ZipEntry GetZipEntry(PackageContent contentFile)
        {
            if (contentFile.RelativePath == ".")
                return new ZipEntry(Path.GetFileName(contentFile.FileName));
            var target = contentFile.RelativePath;
            if (target.Last() != '/')
                target += '/';
            return new ZipEntry(Path.Combine(target, contentFile.FileName));
        }

        public static IFile NewWithDescriptor(IFile wrapFile, string name, string version, params string[] descriptorLines)
        {
            return NewWithDescriptor(wrapFile, name, version, Enumerable.Empty<PackageContent>(), descriptorLines);
        }

        public static IFile NewWithDescriptor(IFile wrapFile, string name, string version, IEnumerable<PackageContent> addedContent, params string[] descriptorLines)
        {
            var descriptorContent = (descriptorLines.Any() ? String.Join("\r\n", descriptorLines) : " ").ToUTF8Stream();
            var versionContent = version.ToUTF8Stream();
            var content = new List<PackageContent>
            {
                    new PackageContent
                    {
                            FileName = name + ".wrapdesc",
                            RelativePath = ".",
                            Stream = () => descriptorContent
                    },
                    new PackageContent
                    {
                            FileName = "version",
                            RelativePath = ".",
                            Stream = () => versionContent
                    }
            }.Concat(addedContent);
            NewFromFiles(wrapFile, content);
            return wrapFile;
        }
    }
    public class PackageContent
    {
        public string RelativePath { get; set; }
        public string FileName { get; set; }
        public Func<Stream> Stream { get;set; }
    }
}