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
            int[] cubeLutWidthes = { 512, 1024 };
            int equirectLutSize = 256;
            int brdfLutSize = 256;
            int hammersleySampleCount = 1024;

            // Direct3D10デバイス作成
            using (var device = new Device(DeviceCreationFlags.None))
            {
                // Equirectangular projection マッピングのUV値からキューブマップ展開図の
                // UV値へ変換するためのテクスチャ保存
                Console.WriteLine(
                    "Making the texture for converting " +
                    "from UV value of Equirectangular projection mapping " +
                    "to UV value of cube mapping ...");
                foreach (var width in cubeLutWidthes)
                {
                    using (
                        var tex =
                            EnvMapLookUpTextureMaker.MakeEquirectangularUVToCubeUV(
                                device,
                                width,
                                width / 2))
                    {
                        Texture2D.ToFile(
                            tex,
                            ImageFileFormat.Dds,
                            "equirect_to_cube_" + width + ".dds");
                    }
                }

                // 単位視線ベクトルから Equirectangular projection マッピングのUV値へ
                // 変換するためのテクスチャ保存
                Console.WriteLine(
                    "Making the texture for converting from the unit vector of eye " +
                    "to UV value of Equirectangular projection mapping ...");
                using (
                    var tex =
                        EnvMapLookUpTextureMaker.MakeEyeToEquirectangularUV(
                            device,
                            equirectLutSize,
                            equirectLutSize))
                {
                    Texture2D.ToFile(tex, ImageFileFormat.Dds, "eye_to_equirect.dds");
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
