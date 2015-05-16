using System;
using SlimDX.Direct3D10;

namespace UE4LikeIBLTextureGen
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
            int cubeFaceSize = 256;
            int envLutSize = 256;
            int brdfLutSize = 256;
            int hammersleySampleCount = 1024;

            // Direct3D10デバイス作成
            using (var device = new Device(DeviceCreationFlags.None))
            {
                // キューブマップから Equirectangular projection マッピングへ
                // 変換するためのテクスチャ保存
                Console.WriteLine("Making the texture for transforming cube map ...");
                using (
                    var tex =
                        CubeTransformTextureMaker.Make(
                            device,
                            cubeFaceSize * 4,
                            cubeFaceSize * 2))
                {
                    Texture2D.ToFile(tex, ImageFileFormat.Dds, "cube_trans.dds");
                }

                // Equirectangular projection マッピング Look-up テクスチャ保存
                Console.WriteLine(
                    "Making the look-up texture of " +
                    "the Equirectangular projection mapping ...");
                using (
                    var tex =
                        EquirectangularLookUpTextureMaker.Make(
                            device,
                            envLutSize,
                            envLutSize))
                {
                    Texture2D.ToFile(tex, ImageFileFormat.Dds, "lookup_envmap.dds");
                }

                // IBL Look-up テクスチャ保存
                Console.WriteLine("Making the look-up texture of IBL ...");
                using (
                    var tex =
                        BrdfLookUpTextureMaker.Make(
                            device,
                            brdfLutSize,
                            hammersleySampleCount))
                {
                    Texture2D.ToFile(tex, ImageFileFormat.Dds, "lookup_brdf.dds");
                }

                // Hammersley Y座標値テクスチャ保存
                Console.WriteLine(
                    "Making the texture of Y-value of the Hammersley points ...");
                using (
                    var tex = HammersleyYTextureMaker.Make(device, hammersleySampleCount))
                {
                    Texture2D.ToFile(tex, ImageFileFormat.Dds, "hammersley_y.dds");
                }
            }

            return 0;
        }
    }
}
