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
using Gibbed.IO;
using Gibbed.Volition.FileFormats;
using NDesk.Options;
using Package = Gibbed.Volition.FileFormats.Package;

namespace Gibbed.Volition.Packing.VPP_PUN
{
    public class Packer<TPackage, TEntry> : PackerBase<TPackage, TEntry>
        where TPackage : IPackageFile<TEntry>, new()
        where TEntry : IPackageEntry, new()
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().CodeBase);
        }

        public override int Main(string[] args)
        {
            var showHelp = false;
            var verbose = false;
            var isCompressed = false;
            var endian = Endian.Little;
            var levelOrder = false;
            var extraPad = true;

            var package = new TPackage();
            var options = new OptionSet();

            var extraFlags = 0u;

            if ((package.SupportedFlags & Package.HeaderFlags.Compressed) != 0)
            {
                options.Add(
                    "c|compress",
                    "compress data",
                    v => isCompressed = v != null
                );
            }

            options.Add<int>(
                "f|flag=",
                "extra flag (as bit)",
                v => extraFlags |= (v == null ? 0 : (1u << v))
            );

            options.Add(
                "l|little-endian",
                "pack data in little-endian mode (default)",
                v => endian = v != null ? Endian.Little : endian
            );

            options.Add(
                "b|big-endian",
                "pack data in big-endian mode",
                v => endian = v != null ? Endian.Big : endian
            );

            options.Add(
                "v|verbose",
                "enable verbose logging",
                v => verbose = v != null
            );

            options.Add(
                "p|padding",
                "add extra padding after files",
                v => extraPad = v != null
            );

            options.Add(
                "h|help",
                "show this message and exit",
                v => showHelp = v != null
            );

            options.Add(
                "s|sequence",
                "entries order",
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

            if (extras.Count < 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_directory", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ output_vpp input_directory+", GetExecutableName());
                Console.WriteLine("Pack directores into specified Volition package file.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return 2;
            }
            else
            {
                Console.WriteLine("Options: levelOrder is {0}", levelOrder.ToString());
                Console.WriteLine("Options: compress is {0}", isCompressed.ToString());
            }

            string outputPath;
            var paths = new Dictionary<string, string>();

            if (extras.Count == 1)
            {

                outputPath = Path.ChangeExtension(extras[0] + "_PACKED", ".vpp");

                // store files order
                if (levelOrder)
                {
                    var orderFiles = new List<string>();
                    var dirFiles = Directory.GetFiles(extras[0], "*");

                    if (!File.Exists(Path.ChangeExtension(extras[0], ".vpp.txt")))
                    {
                        Console.WriteLine("Level order file is not found! Check the naming (Ex: folder_name_vpp.txt)", isCompressed.ToString());
                        return 0;
                    }

                    foreach (var line in File.ReadLines(Path.ChangeExtension(extras[0], ".vpp.txt")))
                    {
                        if (line == "")
                        {
                            break;
                        }

                        orderFiles.Add(line);

                        var fullPath = Path.GetFullPath(extras[0] + "\\" + line);
                        var name = Path.GetFileName(fullPath);

                        if (paths.ContainsKey(name) == true)
                        {
                            continue;
                        }

                        paths[name] = fullPath;
                    }
                }
                else
                {
                    foreach (var path in Directory.GetFiles(extras[0], "*"))
                    {
                        var fullPath = Path.GetFullPath(path);
                        var name = Path.GetFileName(fullPath);

                        if (paths.ContainsKey(name) == true)
                        {
                            continue;
                        }

                        paths[name] = fullPath;
                    }
                }
            }
            else
            {
                outputPath = extras[0];

                // store files order
                if (levelOrder)
                {
                    var orderFiles = new List<string>();
                    var dirFiles = Directory.GetFiles(extras[0], "*");

                    foreach (var line in File.ReadLines(Path.ChangeExtension(extras[0], ".vpp.txt")))
                    {
                        if (line == "")
                        {
                            break;
                        }

                        orderFiles.Add(line);

                        var fullPath = Path.GetFullPath(extras[0] + "\\" + line);
                        var name = Path.GetFileName(fullPath);

                        if (paths.ContainsKey(name) == true)
                        {
                            continue;
                        }

                        paths[name] = fullPath;
                    }

                    foreach (var file in orderFiles)
                    {
                        var fullPath = Path.GetFullPath(extras[0] + "\\" + file);
                        var name = Path.GetFileName(fullPath);

                        if (paths.ContainsKey(name) == true)
                        {
                            continue;
                        }

                        paths[name] = fullPath;
                    }
                }
                else
                {
                    for (int i = 1; i < extras.Count; i++)
                    {
                        var directory = extras[i];

                        foreach (var path in Directory.GetFiles(directory, "*"))
                        {
                            var fullPath = Path.GetFullPath(path);
                            var name = Path.GetFileName(fullPath);

                            if (paths.ContainsKey(name) == true)
                            {
                                continue;
                            }

                            paths[name] = fullPath;
                        }
                    }
                }
            }

            package.Endian = endian;

            var flags = Package.HeaderFlags.None;

            if (isCompressed == true)
            {
                flags |= Package.HeaderFlags.Compressed;
            }

            package.Flags = flags;
            package.ExtraFlags = extraFlags;

            this.Build(package, paths, outputPath, false, extraPad);
            return 0;
        }
    }
}
