using System;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// Equirectangular projection マッピングの Look-up テクスチャを生成する静的クラス。
    /// </summary>
    internal static class EquirectangularLookUpTextureMaker
    {
        /// <summary>
        /// テクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">生成するテクスチャの横幅。</param>
        /// <param name="height">生成するテクスチャの横幅。</param>
        /// <returns>生成されたテクスチャ。</returns>
        /// <remarks>
        /// 横方向を単位視線ベクトルの Z 値、縦方向を単位視線ベクトルの Y 値とする。
        /// いずれも [-1, 1] の値範囲をテクスチャの縦横幅に線形対応させる。
        /// </remarks>
        public static Texture2D Make(
            Device device,
            int width,
            int height)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }
            Util.ValidateTextureSize(width, 1, int.MaxValue, "width");
            Util.ValidateTextureSize(height, 1, int.MaxValue, "height");

            // 浮動小数RGピクセル配列作成
            var ch = 2;
            var pixels = new float[width * ch * height];
            for (int y = 0; y < height; ++y)
            {
                var eyeY = (y * 2.0 + 1) / height - 1;
                for (int z = 0; z < width; ++z)
                {
                    var eyeZ = (z * 2.0 + 1) / width - 1;
                    var uv = Util.EquirectangularMapUV(eyeY, eyeZ);

                    var pos = (y * width + z) * ch;
                    pixels[pos + 0] = (float)Math.Min(Math.Max(0, uv.X), 1); // R
                    pixels[pos + 1] = (float)Math.Min(Math.Max(0, uv.Y), 1); // G
                }
            }

            // テクスチャ情報作成
            var desc =
                new Texture2DDescription
                {
                    ArraySize = 1,
                    Format = DXGI.Format.R32G32_Float,
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    SampleDescription = new DXGI.SampleDescription(1, 0),
                };

            // テクスチャ生成
            using (var s = new DataStream(pixels, true, false))
            {
                var data = new DataRectangle(sizeof(float) * ch * width, s);
                return new Texture2D(device, desc, data);
            }
        }
    }
}
