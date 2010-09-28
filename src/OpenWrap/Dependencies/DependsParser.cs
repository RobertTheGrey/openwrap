﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenWrap.Dependencies
{
    public class DependsParser : AbstractDescriptorParser
    {
        public DependsParser() : base("depends"){}

        protected override void ParseContent(string content, WrapDescriptor descriptor)
        {
            var dependency = ParseDependency(content);
            if (dependency != null)
                descriptor.Dependencies.Add(dependency);
        }
        protected override IEnumerable<string> WriteContent(WrapDescriptor descriptor)
        {
            foreach (var dependency in descriptor.Dependencies)
                yield return dependency.ToString();
        }

        public static WrapDescriptor ParseDependsInstruction(string line)
        {
            var descriptor = new WrapDescriptor();
            new DependsParser().Parse(line, descriptor);
            return descriptor;
        }
        static PackageDependency ParseDependency(string line)
        {
            var bits = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (bits.Length < 1)
                return null;

            var versions = ParseVersions(bits.Skip(1).ToArray()).ToList();
            var tags = bits.Skip((versions.Count * 2) + versions.Count).ToArray();

            return new PackageDependency
            {
                    Name = bits[0],
                    VersionVertices = versions.Count > 0 ? versions : new List<VersionVertex>() { new AnyVersionVertex() },
                    Tags = tags,
            };
        }

        public static IEnumerable<VersionVertex> ParseVersions(string[] args)
        {
            int consumedArgs = 0;
            // Versions are always in the format
            // comparator version ('and' comparator version)*
            if (args.Length >= 2)
            {
                var version = GetVersionVertice(args, 0);
                if (version == null)
                    yield break;
                yield return version;
                for (int i = 0; i < (args.Length - 2) / 3; i++)
                    if (args[2 + (i * 3)].Equals("and", StringComparison.OrdinalIgnoreCase))
                        if ((version = GetVersionVertice(args, 3 + (i * 3))) != null)
                            yield return version;
                        else
                            yield break;
            }
        }
        private static VersionVertex GetVersionVertice(string[] strings, int offset)
        {
            var comparator = strings[offset];
            var version = new Version(strings[offset + 1]);
            switch (comparator)
            {
                case ">":
                    return new GreaterThenVersionVertex(version);
                case ">=": return new GreaterThenOrEqualVersionVertex(version);
                case "=": return new ExactVersionVertex(version);
                case "<": return new LessThanVersionVertex(version);
                default: return null;
            }
        }
    }
    
}
