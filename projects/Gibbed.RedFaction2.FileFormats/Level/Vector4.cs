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

using System.IO;
using Gibbed.IO;

namespace Gibbed.RedFaction2.FileFormats.Level
{
    public struct Vector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public static Vector4 Read(Stream input, Endian endian)
        {
            Vector4 instance;
            instance.X = input.ReadValueF32(endian);
            instance.Y = input.ReadValueF32(endian);
            instance.Z = input.ReadValueF32(endian);
            instance.W = input.ReadValueF32(endian);
            return instance;
        }

        public static void Write(Stream output, Vector4 instance, Endian endian)
        {
            output.WriteValueF32(instance.X, endian);
            output.WriteValueF32(instance.Y, endian);
            output.WriteValueF32(instance.Z, endian);
            output.WriteValueF32(instance.W, endian);
        }

        public void Write(Stream output, Endian endian)
        {
            Write(output, this, endian);
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", this.X, this.Y, this.Z, this.W);
        }
    }
}