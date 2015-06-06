using System;
using System.Windows.Media.Media3D;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// 環境マップの Look-up テクスチャを生成する静的クラス。
    /// </summary>
    internal static class EnvMapLookUpTextureMaker
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
        public static Texture2D MakeEyeToCubeUV(Device device, int width, int height)
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

            // テクスチャ生成
            return
                Util.MakeTexture(
                    device,
                    width,
                    height,
                    DXGI.Format.R16G16_Float,
                    pixels,
                    Util.SizeOfHalf * ch);
        }

        /// <summary>
        /// Equirectangular projection マッピングのUV値からキューブマップ展開図のUV値を
        /// 取得するためのテクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">生成するテクスチャの横幅。</param>
        /// <param name="height">生成するテクスチャの横幅。</param>
        /// <returns>生成されたテクスチャ。</returns>
        public static Texture2D MakeEquirectangularUVToCubeUV(
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
            var pixels = new Half[width * ch * height];
            for (int y = 0; y < height; ++y)
            {
                var v = (y + 0.5) / height;
                for (int x = 0; x < width; ++x)
                {
                    var u = (x + 0.5) / width;
                    var uv = Util.EquirectangularUVToCubeUV(u, v);

                    var pos = (y * width + x) * ch;
                    pixels[pos + 0] = (Half)(float)Math.Min(Math.Max(0, uv.X), 1); // R
                    pixels[pos + 1] = (Half)(float)Math.Min(Math.Max(0, uv.Y), 1); // G
                }
            }

            // テクスチャ生成
            return
                Util.MakeTexture(
                    device,
                    width,
                    height,
                    DXGI.Format.R16G16_Float,
                    pixels,
                    Util.SizeOfHalf * ch);
        }

        /// <summary>
        /// 単位視線ベクトルから Equirectangular projection マッピングのUV値を
        /// 取得するためのテクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">生成するテクスチャの横幅。</param>
        /// <param name="height">生成するテクスチャの横幅。</param>
        /// <returns>生成されたテクスチャ。</returns>
        /// <remarks>
        /// 横方向を単位視線ベクトルの Z 値、縦方向を単位視線ベクトルの Y 値とする。
        /// いずれも [-1, 1] の値範囲をテクスチャの縦横幅に線形対応させる。
        /// </remarks>
        public static Texture2D MakeEyeToEquirectangularUV(
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
            var pixels = new Half[width * ch * height];
            for (int y = 0; y < height; ++y)
            {
                var eyeY = (y * 2.0 + 1) / height - 1;
                for (int z = 0; z < width; ++z)
                {
                    var eyeZ = (z * 2.0 + 1) / width - 1;
                    var uv = Util.EyeToEquirectangularUV(eyeY, eyeZ);

                    var pos = (y * width + z) * ch;
                    pixels[pos + 0] = (Half)(float)Math.Min(Math.Max(0, uv.X), 1); // R
                    pixels[pos + 1] = (Half)(float)Math.Min(Math.Max(0, uv.Y), 1); // G
                }
            }

            // テクスチャ生成
            return
                Util.MakeTexture(
                    device,
                    width,
                    height,
                    DXGI.Format.R16G16_Float,
                    pixels,
                    Util.SizeOfHalf * ch);
        }

        /// <summary>
        /// Equirectangular projection マッピングのUV値から視線ベクトルを
        /// 取得するためのテクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">生成するテクスチャの横幅。</param>
        /// <param name="height">生成するテクスチャの横幅。</param>
        /// <returns>生成されたテクスチャ。</returns>
        /// <remarks>
        /// [(-1, -1, -1), (1, 1, 1)] の範囲の視線ベクトル要素値を
        /// [(0, 0, 0), (1, 1, 1)] の範囲に線形補正して書き出す。
        /// </remarks>
        public static Texture2D MakeEquirectangularUVToEye(
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

            // 浮動小数RGBAピクセル配列作成
            var ch = 4;
            var pixels = new Half[width * ch * height];
            for (int y = 0; y < height; ++y)
            {
                var v = (y + 0.5) / height;
                for (int x = 0; x < width; ++x)
                {
                    var u = (x + 0.5) / width;
                    var eye = Util.EquirectangularUVToEye(u, v);

                    // 線形補正
                    eye += new Vector3D(1, 1, 1);
                    eye *= 0.5;

                    var pos = (y * width + x) * ch;
                    pixels[pos + 0] = (Half)(float)Math.Min(Math.Max(0, eye.X), 1); // R
                    pixels[pos + 1] = (Half)(float)Math.Min(Math.Max(0, eye.Y), 1); // G
                    pixels[pos + 2] = (Half)(float)Math.Min(Math.Max(0, eye.Z), 1); // B
                    pixels[pos + 3] = new Half(1);                                  // A
                }
            }

            // テクスチャ生成
            return
                Util.MakeTexture(
                    device,
                    width,
                    height,
                    DXGI.Format.R16G16B16A16_Float,
                    pixels,
                    Util.SizeOfHalf * ch);
        }
    }
}
