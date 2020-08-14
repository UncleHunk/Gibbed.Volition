/* Copyright (c) 2017 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;

/* VPP version 3
 * 
 * Used by:
 *   The Punisher
 */

namespace Gibbed.Volition.FileFormats
{
    public class PackageFileV3Pun : IPackageFile<Package.Entry>
    {
        public Endian Endian { get; set; }
        public Package.HeaderFlags Flags { get; set; }
        public uint ExtraFlags { get; set; }

        public Package.HeaderFlags SupportedFlags
        {
            get
            {
                return Package.HeaderFlags.Compressed |
                       Package.HeaderFlags.Condensed;
            }
        }

        public uint TotalSize { get; set; }
        public uint UncompressedSize { get; set; }
        public uint CompressedSize { get; set; }
        public List<Package.Entry> Entries { get; private set; }

        public IEnumerable<IPackageEntry> Directory
        {
            get { return this.Entries; }
        }

        public long DataOffset { get; set; }

        public PackageFileV3Pun()
        {
            this.Flags = Package.HeaderFlags.None;
            this.Entries = new List<Package.Entry>();
        }

        public int EstimateHeaderSize()
        {
            int totalSize = 2048 + (this.Entries.Count * 32) + (2048 - (this.Entries.Count * 32) % 2048);

            return totalSize;
        }

        protected static Package.HeaderFlags ConvertFlags(Package.HeaderFlagsV3 flags)
        {
            var newFlags = Package.HeaderFlags.None;

            if ((flags & Package.HeaderFlagsV3.Compressed) != 0)
            {
                newFlags |= Package.HeaderFlags.Compressed;
            }

            return newFlags;
        }

        protected static Package.HeaderFlagsV3 ConvertFlags(Package.HeaderFlags flags)
        {
            var newFlags = Package.HeaderFlagsV3.None;

            if ((flags & Package.HeaderFlags.Compressed) != 0)
            {
                newFlags |= Package.HeaderFlagsV3.Compressed;
            }

            return newFlags;
        }

        public void Serialize(Stream output)
        {
            var endian = this.Endian;
            var names = new MemoryStream();
            var directory = new MemoryStream();
            var header = new Package.HeaderV3Pun()
            {
                DirectoryCount = (uint)this.Entries.Count,
                PackageSize = this.TotalSize,
            };

            output.WriteValueU32(0x51890ACE, endian);
            output.WriteValueU32(3, endian);
            header.Serialize(output, endian);
            output.Seek(2048, SeekOrigin.Begin);

            // write entries names and size
            foreach (var entry in this.Entries)
            {
                var curOutputPos = output.Position;
                output.WriteStringZ(entry.Name, Encoding.ASCII);

                if (entry.Name.Length < 24)
                {
                    output.Position = curOutputPos;
                    output.Seek(24, SeekOrigin.Current);
                }

                output.WriteValueU32(entry.UncompressedSize, endian);
                output.WriteValueU32(entry.CompressedSize, endian);
            }
        }

        public void Deserialize(Stream input)
        {
            Endian endian;
            Package.HeaderV3Pun header;

            using (var data = input.ReadToMemoryStream(2048))
            {
                var magic = data.ReadValueU32(Endian.Little);
                if (magic != 0x51890ACE &&
                    magic.Swap() != 0x51890ACE)
                {
                    throw new FormatException("not a package file");
                }
                endian = magic == 0x51890ACE ? Endian.Little : Endian.Big;

                var version = data.ReadValueU32(endian);
                if (version != 3)
                {
                    throw new FormatException("unexpected package version (expected 3)");
                }

                header = new Package.HeaderV3Pun();
                header.Deserialize(data, endian);
            }

            this.Entries.Clear();

            input.Seek(2048, SeekOrigin.Begin);

            for (int i = 0; i < header.DirectoryCount; i++)
            {
                var test = input.ReadBytes(24);
                Byte[] names = new Byte[24];

                for (var j = 0; j < test.Length; j++)
                {
                    if (test[j] != 0)
                    {
                        names[j] = test[j];
                    }
                    else break;
                }
                var name = Encoding.ASCII.GetString(names).TrimEnd((Char)0); ;
                var uncompressedSize = input.ReadValueU32(endian);  // uncompressed size
                var compressedSize = input.ReadValueU32(endian);    // compressed size

                this.Entries.Add(new Package.Entry()
                {
                    Name = name,
                    UncompressedSize = uncompressedSize,
                    CompressedSize = compressedSize,
                });
            }

            this.Endian = endian;
            this.DataOffset = input.Position + (2048 - (input.Position % 2048));
        }
    }
}
