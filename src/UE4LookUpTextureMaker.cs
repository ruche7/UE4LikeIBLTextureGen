using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UE4IBLLookUpTextureGen
{
    /// <summary>
    /// UE4 の IBL に用いる Look-up テクスチャを生成する静的クラス。
    /// </summary>
    /// <remarks>
    /// 参考文献: SIGGRAPH 2013 Course: Physically Based Shading in Theory and Practice
    /// http://blog.selfshadow.com/publications/s2013-shading-course/
    /// </remarks>
    internal static class UE4LookUpTextureMaker
    {
        /// <summary>
        /// Look-up テクスチャイメージを生成する。
        /// </summary>
        /// <param name="nvDotWidth">
        /// 法線ベクトルと視点ベクトルとの内積値をアサインするイメージ横幅。
        /// 1 以上かつ 2 の累乗値。
        /// </param>
        /// <param name="roughnessHeight">
        /// ラフネス値をアサインするイメージ縦幅。 1 以上かつ 2 の累乗値。
        /// </param>
        /// <param name="hammerslaySampleCount">
        /// Hammerslay 座標の総サンプリング数。 1 以上。
        /// </param>
        /// <returns>生成されたテクスチャイメージ。</returns>
        public static BitmapSource Make(
            int nvDotWidth,
            int roughnessHeight,
            int hammerslaySampleCount)
        {
            ValidateWidthOrHeight(nvDotWidth, "nvDotWidth");
            ValidateWidthOrHeight(roughnessHeight, "roughnessHeight");
            Util.ValidateRange(
                hammerslaySampleCount, 1, int.MaxValue, "hammerslaySampleCount");

            // BGRA ピクセル配列作成
            int bpp = 4;
            var pixels = new byte[nvDotWidth * bpp * roughnessHeight];
            for (int y = 0; y < roughnessHeight; ++y)
            {
                var roughness = (y + 0.5) / roughnessHeight;
                for (int x = 0; x < nvDotWidth; ++x)
                {
                    var nvDot = (x + 0.5) / nvDotWidth;
                    var lut = Util.IntegrateBRDF(roughness, nvDot, hammerslaySampleCount);

                    var pos = (y * nvDotWidth + x) * bpp;
                    pixels[pos + 0] = 0; // B
                    pixels[pos + 1] = (byte)Math.Min(Math.Max(0, lut.Y * 255), 255); // G
                    pixels[pos + 2] = (byte)Math.Min(Math.Max(0, lut.X * 255), 255); // R
                    pixels[pos + 3] = 255; // A
                }
            }

            // イメージ作成
            var bmp =
                new WriteableBitmap(
                    nvDotWidth,
                    roughnessHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null);
            bmp.WritePixels(
                new Int32Rect(0, 0, nvDotWidth, roughnessHeight),
                pixels,
                nvDotWidth * bpp,
                0);

            return bmp;
        }

        /// <summary>
        /// Look-up テクスチャイメージを生成する。
        /// </summary>
        /// <param name="widthAndHeight">
        /// イメージの縦横幅。 1 以上かつ 2 の累乗値。
        /// </param>
        /// <param name="hammerslaySampleCount">
        /// Hammerslay 座標の総サンプリング数。 1 以上。
        /// </param>
        /// <returns>生成されたテクスチャイメージ。</returns>
        public static BitmapSource Make(int widthAndHeight, int hammerslaySampleCount)
        {
            ValidateWidthOrHeight(widthAndHeight, "widthAndHeight");

            return Make(widthAndHeight, widthAndHeight, hammerslaySampleCount);
        }

        /// <summary>
        /// イメージの縦横幅値を検証する。
        /// </summary>
        /// <param name="value">縦横幅値。</param>
        /// <param name="paramName">例外生成に用いる引数名。</param>
        private static void ValidateWidthOrHeight(int value, string paramName)
        {
            Util.ValidateRange(value, 1, int.MaxValue, paramName);

            if (!Util.IsPowerOf2(value))
            {
                throw new ArgumentException(
                    "The value of `" + paramName + "` is not a power of 2.",
                    paramName);
            }
        }
    }
}
