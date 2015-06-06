using System;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// Hammersley 座標のY座標値を格納したテクスチャを生成する静的クラス。
    /// </summary>
    internal static class HammersleyYTextureMaker
    {
        /// <summary>
        /// テクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="sampleCount">
        /// Hammersley 座標の総サンプリング数。 1 以上かつ 2 の累乗値。
        /// </param>
        /// <returns>生成されたテクスチャ。</returns>
        public static Texture2D Make(Device device, int sampleCount)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }
            Util.ValidateTextureSize(sampleCount, 1, int.MaxValue, "sampleCount");

            // 浮動小数ピクセル配列作成
            var pixels = new float[sampleCount];
            for (int hi = 0; hi < sampleCount; ++hi)
            {
                pixels[hi] = (float)Util.Hammersley(hi, sampleCount).Y;
            }

            // テクスチャ生成
            return
                Util.MakeTexture(
                    device,
                    sampleCount,
                    1,
                    DXGI.Format.R32_Float,
                    pixels,
                    sizeof(float));
        }
    }
}
