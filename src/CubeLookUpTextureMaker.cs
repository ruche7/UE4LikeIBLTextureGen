using System;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// キューブマップ展開図の Look-up テクスチャを生成する静的クラス。
    /// </summary>
    internal static class CubeLookUpTextureMaker
    {
        /// <summary>
        /// 単位視線ベクトルからキューブマップ展開図のUV値を取得するためのテクスチャを
        /// 生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">生成するテクスチャの横幅。</param>
        /// <param name="height">生成するテクスチャの横幅。</param>
        /// <returns>生成されたテクスチャ。</returns>
        /// <remarks>
        /// 横方向を単位視線ベクトルの X 値、縦方向を単位視線ベクトルの Z 値とする。
        /// いずれも [-1, 1] の値範囲をテクスチャの縦横幅に線形対応させる。
        /// </remarks>
        public static Texture2D MakeEyeToMapUV(Device device, int width, int height)
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
            for (int z = 0; z < height; ++z)
            {
                var eyeZ = (z * 2.0 + 1) / height - 1;
                for (int x = 0; x < width; ++x)
                {
                    var eyeX = (x * 2.0 + 1) / width - 1;
                    var uv = Util.EyeToCubeUV(eyeX, eyeZ);

                    var pos = (z * width + x) * ch;
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
