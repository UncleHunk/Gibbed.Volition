﻿/* Copyright (c) 2017 Rick (rick 'at' gibbed 'dot' us)
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
using Gibbed.IO;
using Gibbed.Volition.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;
using Package = Gibbed.Volition.FileFormats.Package;

namespace Gibbed.Volition.Packing.VPP_PUN
{
    public class Unpacker<TPackage>
        where TPackage : IPackageFile, new()
    {
        public bool SupportPS3Chunking = false;

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public int Main(string[] args)
        {
            var showHelp = false;
            var overwriteFiles = false;
            var verbose = false;
            var levelOrder = false;

            var options = new OptionSet();

            options.Add(
                "o|overwrite",
                "overwrite files if they already exist",
                v => overwriteFiles = v != null
            );

            options.Add(
                "v|verbose",
                "enable verbose logging",
                v => verbose = v != null
            );

            options.Add(
                "h|help",
                "show this message and exit",
                v => showHelp = v != null
            );

            options.Add(
                "s|sequence",
                "save entries order",
                v => levelOrder = v != null
            );

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return 1;
            }

            if (extras.Count < 1 || extras.Count > 3 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_vpp [output_dir]", GetExecutableName());
                Console.WriteLine("Unpack specified Volition package file.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return 2;
            }
            else
            {
                Console.WriteLine("Options: levelOrder is {0}", levelOrder.ToString());
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ?
                extras[1] :
                Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(extras[0]));

            var previousNames = new Dictionary<string, long>();

            Console.WriteLine("START");

            using (var input = File.OpenRead(inputPath))
            {
                var package = new TPackage();
                Console.WriteLine("begin Deserialize");
                package.Deserialize(input);

                long current = 0;
                long total = package.Directory.Count();
                var totalCompressed = 0;

                Console.WriteLine("total: {0}", total.ToString());
                if (total > 0)
                {
                    Stream data = input;
                    var flags = package.Flags;

                    var dataOffset = package.DataOffset;
                    var isCompressed = (package.Flags & Package.HeaderFlags.Compressed) != 0;

                    Console.WriteLine("isCompressed: {0}", isCompressed.ToString());

                    var padding = total.ToString().Length;

                    foreach (var entry in package.Directory.OrderBy(e => e.Offset))
                    {
                        current++;

                        string outputName;

                        // save files order
                        if (levelOrder)
                        {
                            using (var sw = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), inputPath + ".txt"), true))
                            {
                                sw.WriteLine(entry.Name);
                                sw.Flush();
                            }
                        }

                        if (previousNames.ContainsKey(entry.Name) == true)
                        {
                            outputName = string.Format("{0} [DUPLICATE_{1}]{2}",
                                Path.ChangeExtension(entry.Name, null),
                                previousNames[entry.Name],
                                Path.GetExtension(entry.Name) ?? "");
                            previousNames[entry.Name]++;
                        }
                        else
                        {
                            outputName = entry.Name;
                            previousNames.Add(entry.Name, 1);
                        }

                        var entryPath = Path.Combine(outputPath, outputName);
                        Console.WriteLine("Write File: {0} to dir {1}", outputName, outputPath);

                        if (overwriteFiles == true || File.Exists(entryPath) == false)
                        {
                            if (verbose == true)
                            {
                                Console.WriteLine("[{0}/{1}] {2}",
                                    current.ToString().PadLeft(padding),
                                    total,
                                    entry.Name);
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(entryPath));

                            var dataStart = dataOffset;

                            data.Seek(dataOffset, SeekOrigin.Begin);
                            using (var output = File.Create(entryPath))
                            {
                                // some files can be compressed
                                if (entry.UncompressedSize != entry.CompressedSize)
                                {
                                    isCompressed = true;
                                    Console.WriteLine("file {0} compressed", entry.Name.ToString());
                                    totalCompressed++;
                                }
                                else isCompressed = false;

                                if (isCompressed == false)
                                {
                                    output.WriteFromStream(data, entry.UncompressedSize);
                                }
                                else
                                {
                                    using (var temp = data.ReadToMemoryStream(entry.CompressedSize))
                                    {
                                        var zlib = new InflaterInputStream(temp);
                                        output.WriteFromStream(zlib, entry.UncompressedSize);
                                    }
                                }
                            }
                        }

                        var dataSize = isCompressed == false ? entry.UncompressedSize : entry.CompressedSize;

                        dataSize = dataSize.Align(2048);
                        dataOffset += dataSize;
                    }

                    if (data != input)
                    {
                        data.Close();
                    }
                }
                Console.WriteLine("Total compressed files {0}", totalCompressed.ToString());
                return 0;
            }
        }
    }
}