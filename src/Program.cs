using System;
using SlimDX.Direct3D10;

namespace UE4IBLLookUpTextureGen
{
    /// <summary>
    /// プログラムのメインクラス。
    /// </summary>
    static class Program
    {
        /// <summary>
        /// メインエントリポイント。
        /// </summary>
        /// <param name="args">プログラム引数。</param>
        static int Main(string[] args)
        {
            int lutSize = 256;
            int hammersleySampleCount = 1024;

            // Direct3D10デバイス作成
            using (var device = new Device(DeviceCreationFlags.None))
            {
                // LUT イメージ保存
                Console.WriteLine("Making LUT ...");
                using (
                    var tex =
                        UE4LookUpTextureMaker.Make(device, lutSize, hammersleySampleCount))
                {
                    var filePath = (args.Length < 1) ? "lut.dds" : args[0];
                    var res = Texture2D.ToFile(tex, ImageFileFormat.Dds, filePath);
                }

                // Hammersley Y座標イメージ保存
                Console.WriteLine("Making Y of Hammersley points ...");
                using (
                    var tex = HammersleyYTextureMaker.Make(device, hammersleySampleCount))
                {
                    var filePath = (args.Length < 2) ? "hammersley_y.dds" : args[1];
                    var res = Texture2D.ToFile(tex, ImageFileFormat.Dds, filePath);
                }
            }

            return 0;
        }
    }
}
