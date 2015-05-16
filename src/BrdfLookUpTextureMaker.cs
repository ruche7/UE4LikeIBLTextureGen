using System;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// IBL に用いるBRDF項の Look-up テクスチャを生成する静的クラス。
    /// </summary>
    /// <remarks>
    /// 参考文献: SIGGRAPH 2013 Course: Physically Based Shading in Theory and Practice
    /// http://blog.selfshadow.com/publications/s2013-shading-course/
    /// </remarks>
    internal static class BrdfLookUpTextureMaker
    {
        /// <summary>
        /// テクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="nvDotWidth">
        /// 法線ベクトルと視点ベクトルとの内積値をアサインするイメージ横幅。
        /// 1 以上かつ 2 の累乗値。
        /// </param>
        /// <param name="roughnessHeight">
        /// ラフネス値をアサインするイメージ縦幅。 1 以上かつ 2 の累乗値。
        /// </param>
        /// <param name="hammersleySampleCount">
        /// Hammersley 座標の総サンプリング数。 1 以上。
        /// </param>
        /// <returns>生成されたテクスチャ。</returns>
        public static Texture2D Make(
            Device device,
            int nvDotWidth,
            int roughnessHeight,
            int hammersleySampleCount)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }
            Util.ValidateTextureSize(nvDotWidth, 1, int.MaxValue, "nvDotWidth");
            Util.ValidateTextureSize(roughnessHeight, 1, int.MaxValue, "roughnessHeight");
            Util.ValidateRange(
                hammersleySampleCount, 1, int.MaxValue, "hammersleySampleCount");

            // 浮動小数RGピクセル配列作成
            var ch = 2;
            var pixels = new Half[nvDotWidth * ch * roughnessHeight];
            for (int y = 0; y < roughnessHeight; ++y)
            {
                var roughness = (y + 0.5) / roughnessHeight;
                for (int x = 0; x < nvDotWidth; ++x)
                {
                    var nvDot = (x + 0.5) / nvDotWidth;
                    var lut = Util.IntegrateBRDF(roughness, nvDot, hammersleySampleCount);

                    var pos = (y * nvDotWidth + x) * ch;
                    pixels[pos + 0] = (Half)(float)Math.Min(Math.Max(0, lut.X), 1); // R
                    pixels[pos + 1] = (Half)(float)Math.Min(Math.Max(0, lut.Y), 1); // G
                }
            }

            // テクスチャ情報作成
            var desc =
                new Texture2DDescription
                {
                    ArraySize = 1,
                    Format = DXGI.Format.R16G16_Float,
                    Width = nvDotWidth,
                    Height = roughnessHeight,
                    MipLevels = 1,
                    SampleDescription = new DXGI.SampleDescription(1, 0),
                };

            // テクスチャ生成
            using (var s = new DataStream(pixels, true, false))
            {
                var data = new DataRectangle(Util.SizeOfHalf * ch * nvDotWidth, s);
                return new Texture2D(device, desc, data);
            }
        }

        /// <summary>
        /// テクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="widthAndHeight">
        /// イメージの縦横幅。 1 以上かつ 2 の累乗値。
        /// </param>
        /// <param name="hammersleySampleCount">
        /// Hammersley 座標の総サンプリング数。 1 以上。
        /// </param>
        /// <returns>生成されたテクスチャ。</returns>
        public static Texture2D Make(
            Device device,
            int widthAndHeight,
            int hammersleySampleCount)
        {
            Util.ValidateTextureSize(widthAndHeight, 1, int.MaxValue, "widthAndHeight");

            return Make(device, widthAndHeight, widthAndHeight, hammersleySampleCount);
        }
    }
}
