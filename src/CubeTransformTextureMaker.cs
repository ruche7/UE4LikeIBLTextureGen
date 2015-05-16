using System;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// キューブマップ展開図から Equirectangular projection マッピングへ変換するための
    /// テクスチャを生成する静的クラス。
    /// </summary>
    internal static class CubeTransformTextureMaker
    {
        /// <summary>
        /// テクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">生成するテクスチャの横幅。</param>
        /// <param name="height">生成するテクスチャの横幅。</param>
        /// <returns>生成されたテクスチャ。</returns>
        public static Texture2D Make(Device device, int width, int height)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }
            Util.ValidateTextureSize(width, 1, int.MaxValue, "width");
            Util.ValidateTextureSize(height, 1, int.MaxValue, "height");

            // 浮動小数RGピクセル配列作成
            var ch = 2;
            var pixels = new Half[width * ch * height];
            for (int y = 0; y < height; ++y)
            {
                var v = (y + 0.5) / height;
                for (int x = 0; x < width; ++x)
                {
                    var u = (x + 0.5) / width;
                    var uv = Util.EquirectangularUVToCube(u, v);

                    var pos = (y * width + x) * ch;
                    pixels[pos + 0] = (Half)(float)Math.Min(Math.Max(0, uv.X), 1); // R
                    pixels[pos + 1] = (Half)(float)Math.Min(Math.Max(0, uv.Y), 1); // G
                }
            }

            // テクスチャ情報作成
            var desc =
                new Texture2DDescription
                {
                    ArraySize = 1,
                    Format = DXGI.Format.R16G16_Float,
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    SampleDescription = new DXGI.SampleDescription(1, 0),
                };

            // テクスチャ生成
            using (var s = new DataStream(pixels, true, false))
            {
                var data = new DataRectangle(Util.SizeOfHalf * ch * width, s);
                return new Texture2D(device, desc, data);
            }
        }
    }
}
