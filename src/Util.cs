using System;
using System.Windows;
using System.Windows.Media.Media3D;
using SlimDX;
using SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;

namespace UE4LikeIBLTextureGen
{
    /// <summary>
    /// 便利処理をまとめた静的クラス。
    /// </summary>
    internal static class Util
    {
        /// <summary>
        /// Half 型のサイズ。
        /// </summary>
        public const int SizeOfHalf = sizeof(ushort);

        /// <summary>
        /// 値が2の累乗値であるか否かを取得する。
        /// </summary>
        /// <param name="value">調べる値。</param>
        /// <returns>2の累乗値ならば true 。そうでなければ false 。</returns>
        public static bool IsPowerOf2(int value)
        {
            return (value > 0 && (value & (value - 1)) == 0);
        }

        /// <summary>
        /// 値が範囲内に収まっているか検証する。
        /// </summary>
        /// <typeparam name="T">値の型。</typeparam>
        /// <param name="value">調べる値。</param>
        /// <param name="minLimit">最小許容値。</param>
        /// <param name="maxLimit">最大許容値。</param>
        /// <param name="paramName">例外生成に用いる引数名。</param>
        public static void ValidateRange<T>(
            T value,
            T minLimit,
            T maxLimit,
            string paramName)
        {
            if ((dynamic)value < minLimit)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    "The value of `" + paramName + "` is less than " + minLimit + ".");
            }
            if ((dynamic)value > maxLimit)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    "The value of `" + paramName + "` is greater than " + maxLimit + ".");
            }
        }

        /// <summary>
        /// 値がテクスチャの縦横幅として適切か検証する。
        /// </summary>
        /// <param name="value">調べる値。</param>
        /// <param name="minLimit">最小許容値。</param>
        /// <param name="maxLimit">最大許容値。</param>
        /// <param name="paramName">例外生成に用いる引数名。</param>
        public static void ValidateTextureSize(
            int value,
            int minLimit,
            int maxLimit,
            string paramName)
        {
            ValidateRange(value, minLimit, maxLimit, paramName);

            // 2の累乗数でなければならない
            if (!Util.IsPowerOf2(value))
            {
                throw new ArgumentException(
                    "The value of `" + paramName + "` is not a power of 2.",
                    paramName);
            }
        }

        /// <summary>
        /// ピクセル配列からテクスチャを生成する。
        /// </summary>
        /// <param name="device">Direct3D10デバイス。</param>
        /// <param name="width">横幅。</param>
        /// <param name="height">横幅。</param>
        /// <param name="format">フォーマット。</param>
        /// <param name="pixels">ピクセル配列。</param>
        /// <param name="bytesPerPixel">1ピクセルあたりのバイトサイズ。</param>
        /// <returns>生成されたテクスチャ。</returns>
        public static Texture2D MakeTexture(
            Device device,
            int width,
            int height,
            DXGI.Format format,
            Array pixels,
            int bytesPerPixel)
        {
            // テクスチャ情報作成
            var desc =
                new Texture2DDescription
                {
                    ArraySize = 1,
                    Format = format,
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    SampleDescription = new DXGI.SampleDescription(1, 0),
                };

            // テクスチャ生成
            using (var s = new DataStream(pixels, true, false))
            {
                var data = new DataRectangle(bytesPerPixel * width, s);
                return new Texture2D(device, desc, data);
            }
        }

        /// <summary>
        /// インポータンスサンプリング計算を行う。
        /// </summary>
        /// <param name="xi">座標値。</param>
        /// <param name="roughness">ラフネス値。 0.0 以上 1.0 以下。</param>
        /// <param name="normal">法線ベクトル値。</param>
        /// <returns>計算結果。</returns>
        /// <remarks>
        /// 参考文献: SIGGRAPH 2013 Course: Physically Based Shading in Theory and Practice
        /// http://blog.selfshadow.com/publications/s2013-shading-course/
        /// </remarks>
        public static Vector3D ImportanceSampleGGX(
            Vector xi,
            double roughness,
            Vector3D normal)
        {
            ValidateRange(roughness, 0.0, 1.0, "roughness");

            var a = roughness * roughness;

            var phi = 2 * Math.PI * xi.X;
            var cos = Math.Sqrt((1 - xi.Y) / (1 + (a * a - 1) * xi.Y));
            var sin = Math.Sqrt(1 - cos * cos);

            var h = new Vector3D(sin * Math.Cos(phi), sin * Math.Sin(phi), cos);

            var upVec =
                (Math.Abs(normal.Z) < 0.999) ?
                    new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            var tanX = Vector3D.CrossProduct(upVec, normal);
            tanX.Normalize();
            var tanY = Vector3D.CrossProduct(normal, tanX);

            return (tanX * h.X + tanY * h.Y + normal * h.Z);
        }

        /// <summary>
        /// UE4のスペキュラシェーディングに用いる環境BRDF値を算出する。
        /// </summary>
        /// <param name="roughness">ラフネス値。 0.0 以上 1.0 以下。</param>
        /// <param name="nvDot">
        /// 法線ベクトルと視点ベクトルとの内積値。 0.0 以上 1.0 以下。
        /// </param>
        /// <param name="hammersleySampleCount">
        /// Hammersley 座標の総サンプリング数。 1 以上。
        /// </param>
        /// <returns>環境BRDF値。</returns>
        /// <remarks>
        /// 参考文献: SIGGRAPH 2013 Course: Physically Based Shading in Theory and Practice
        /// http://blog.selfshadow.com/publications/s2013-shading-course/
        /// </remarks>
        public static Vector IntegrateBRDF(
            double roughness,
            double nvDot,
            int hammersleySampleCount)
        {
            ValidateRange(roughness, 0.0, 1.0, "roughness");
            ValidateRange(nvDot, 0.0, 1.0, "nvDot");
            ValidateRange(hammersleySampleCount, 1, int.MaxValue, "hammersleySampleCount");

            var result = new Vector();

            if (nvDot <= 0)
            {
                return result;
            }

            var v = new Vector3D(Math.Sqrt(1 - nvDot * nvDot), 0, nvDot);
            var normal = new Vector3D(0, 0, 1);

            for (int hi = 0; hi < hammersleySampleCount; ++hi)
            {
                var xi = Hammersley(hi, hammersleySampleCount);
                var h = ImportanceSampleGGX(xi, roughness, normal);

                var vhDot = Vector3D.DotProduct(v, h);
                var l = 2 * vhDot * h - v;

                if (l.Z > 0)
                {
                    var nlDot = Math.Min(Math.Max(0.0, l.Z), 1.0);
                    var nhDot = Math.Min(Math.Max(0.0, h.Z), 1.0);
                    vhDot = Math.Min(Math.Max(0.0, vhDot), 1.0);

                    var g = GSmith(roughness, nvDot, nlDot);
                    var gVis = g * vhDot / (nhDot * nvDot);
                    var fc = Math.Pow(1 - vhDot, 5);

                    result.X += (1 - fc) * gVis;
                    result.Y += fc * gVis;
                }
            }

            return (result / hammersleySampleCount);
        }

        /// <summary>
        /// Smith Method の計算を行う。
        /// </summary>
        /// <param name="a">計算パラメータ。</param>
        /// <param name="nvDot">計算パラメータ。</param>
        /// <param name="nlDot">計算パラメータ。</param>
        /// <returns>計算結果。</returns>
        /// <remarks>
        /// 参考文献: Specular BRDF Reference
        /// http://graphicrants.blogspot.com.au/2013/08/specular-brdf-reference.html
        /// </remarks>
        private static double GSmith(double a, double nvDot, double nlDot)
        {
            return (SchlickGGX(nlDot, a * a) * SchlickGGX(nvDot, a * a));
        }

        /// <summary>
        /// Schlick-GGX の計算を行う。
        /// </summary>
        /// <param name="nvDot">計算パラメータ。</param>
        /// <param name="a">計算パラメータ。</param>
        /// <returns>計算結果。</returns>
        /// <remarks>
        /// 参考文献: Specular BRDF Reference
        /// http://graphicrants.blogspot.com.au/2013/08/specular-brdf-reference.html
        /// </remarks>
        private static double SchlickGGX(double nvDot, double a)
        {
            var k = a / 2;
            return (nvDot / (nvDot * (1 - k) + k));
        }

        /// <summary>
        /// Hammersley 座標値を算出する。
        /// </summary>
        /// <param name="index">
        /// サンプリングインデックス。 0 以上 sampleCount 未満。
        /// </param>
        /// <param name="sampleCount">総サンプリング数。 1 以上。</param>
        /// <returns>Hammersley 座標値。</returns>
        /// <remarks>
        /// 参考文献: Hammersley Points on the Hemisphere
        /// http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
        /// </remarks>
        public static Vector Hammersley(int index, int sampleCount)
        {
            ValidateRange(sampleCount, 1, int.MaxValue, "sampleCount");
            ValidateRange(index, 0, sampleCount - 1, "index");

            var bits = (uint)index;
            bits = (bits << 16) | (bits >> 16);
            bits = ((bits & 0x55555555) << 1) | ((bits & 0xAAAAAAAA) >> 1);
            bits = ((bits & 0x33333333) << 2) | ((bits & 0xCCCCCCCC) >> 2);
            bits = ((bits & 0x0F0F0F0F) << 4) | ((bits & 0xF0F0F0F0) >> 4);
            bits = ((bits & 0x00FF00FF) << 8) | ((bits & 0xFF00FF00) >> 8);

            return
                new Vector(
                    (double)index / sampleCount,
                    bits * 2.3283064365386963e-10);
        }

        /// <summary>
        /// キューブマップ展開図のUV値を算出する。
        /// </summary>
        /// <param name="eyeX">
        /// 正規化された視線ベクトルの X 値。 -1.0 以上 1.0 以下。
        /// </param>
        /// <param name="eyeZ">
        /// 正規化された視線ベクトルの Z 値。 -1.0 以上 1.0 以下。
        /// </param>
        /// <returns>UV値。</returns>
        /// <remarks>
        /// キューブマップ展開図は次のような形であることを想定している。
        /// 
        /// <pre>
        /// |  |+Y|  |  |
        /// |-X|+Z|+X|-Z|
        /// |  |-Y|  |  |
        /// </pre>
        /// 
        /// 視線ベクトルの Y 値は X 値と Z 値から求められる。
        /// Y 値は常に 0 以上の値として扱う。
        /// 
        /// X 値と Z 値のみの視線ベクトルの長さが 1 を超える場合、
        /// Y 値を 0 として正規化したベクトルを入力値とする。
        /// </remarks>
        public static Vector EyeToCubeUV(double eyeX, double eyeZ)
        {
            ValidateRange(eyeX, -1, +1, "eyeX");
            ValidateRange(eyeZ, -1, +1, "eyeZ");

            // 視線ベクトル決定
            Vector3D eye;
            var len2XZ = eyeX * eyeX + eyeZ * eyeZ;
            if (len2XZ > 1)
            {
                var lenXZ = Math.Sqrt(len2XZ);
                eye = new Vector3D(eyeX / lenXZ, 0, eyeZ / lenXZ);
            }
            else
            {
                eye = new Vector3D(eyeX, Math.Sqrt(1 - len2XZ), eyeZ);
            }

            // 各面上における [(-1, -1), (+1, +1)] 座標と
            // キューブマップ展開図上の各面への平行移動量を決定
            var pos = new Vector();
            var trans = new Vector();
            var absEye = new Vector3D(Math.Abs(eye.X), Math.Abs(eye.Y), Math.Abs(eye.Z));
            if (absEye.Z > absEye.X && absEye.Z > absEye.Y)
            {
                if (eye.Z < 0)
                {
                    // -Z
                    pos.X = -eye.X / absEye.Z;
                    pos.Y = eye.Y / absEye.Z;
                    trans.X = +0.75;
                    trans.Y = 0;
                }
                else
                {
                    // +Z
                    pos.X = eye.X / absEye.Z;
                    pos.Y = eye.Y / absEye.Z;
                    trans.X = -0.25;
                    trans.Y = 0;
                }
            }
            else if (absEye.Y > absEye.X)
            {
                if (eye.Y < 0)
                {
                    // -Y (無いはずだが念のため)
                    pos.X = eye.X / absEye.Y;
                    pos.Y = eye.Z / absEye.Y;
                    trans.X = -0.25;
                    trans.Y = -2.0 / 3;
                }
                else
                {
                    // +Y
                    pos.X = eye.X / absEye.Y;
                    pos.Y = -eye.Z / absEye.Y;
                    trans.X = -0.25;
                    trans.Y = +2.0 / 3;
                }
            }
            else
            {
                if (eye.X < 0)
                {
                    // -X
                    pos.X = eye.Z / absEye.X;
                    pos.Y = eye.Y / absEye.X;
                    trans.X = -0.75;
                    trans.Y = 0;
                }
                else
                {
                    // +X
                    pos.X = -eye.Z / absEye.X;
                    pos.Y = eye.Y / absEye.X;
                    trans.X = +0.25;
                    trans.Y = 0;
                }
            }

            // キューブの各面位置へ移動
            pos.X *= 0.25;
            pos.Y *= 1.0 / 3;
            pos += trans;

            // UV座標系に変換
            pos.X = pos.X * 0.5 + 0.5;
            pos.Y = -pos.Y * 0.5 + 0.5;

            return pos;
        }

        /// <summary>
        /// Equirectangular projection マッピングのUV値を算出する。
        /// </summary>
        /// <param name="eyeY">
        /// 正規化された視線ベクトルの Y 値。 -1.0 以上 1.0 以下。
        /// </param>
        /// <param name="eyeZ">
        /// 正規化された視線ベクトルの Z 値。 -1.0 以上 1.0 以下。
        /// </param>
        /// <returns>UV値。</returns>
        /// <remarks>
        /// マップの中心座標は Z 軸の正方向とする。
        /// 
        /// 視線ベクトルの X 値は Y 値と Z 値から求められる。
        /// X 値は常に 0 以上の値として扱う。
        /// 
        /// Y 値と Z 値のみの視線ベクトルの長さが 1 を超える場合、
        /// X 値を 0 として正規化したベクトルを入力値とする。
        /// </remarks>
        public static Vector EyeToEquirectangularUV(double eyeY, double eyeZ)
        {
            ValidateRange(eyeY, -1, +1, "eyeY");
            ValidateRange(eyeZ, -1, +1, "eyeZ");

            // 視線ベクトル決定
            Vector3D eye;
            var len2YZ = eyeY * eyeY + eyeZ * eyeZ;
            if (len2YZ > 1)
            {
                var lenYZ = Math.Sqrt(len2YZ);
                eye = new Vector3D(0, eyeY / lenYZ, eyeZ / lenYZ);
            }
            else
            {
                eye = new Vector3D(Math.Sqrt(1 - len2YZ), eyeY, eyeZ);
            }

            // XZ成分の長さを算出
            var lenXZ = Math.Sqrt(eye.X * eye.X + eye.Z * eye.Z);

            return
                new Vector(
                    Math.Atan2(eye.X, eye.Z) / Math.PI * 0.5 + 0.5,
                    -Math.Atan2(eye.Y, lenXZ) / Math.PI + 0.5);
        }

        /// <summary>
        /// Equirectangular projection マッピングのUV座標を視線ベクトル値に変換する。
        /// </summary>
        /// <param name="u">
        /// Equirectangular projection マッピングのU座標。 0.0 以上 1.0 以下。
        /// </param>
        /// <param name="v">
        /// Equirectangular projection マッピングのV座標。 0.0 以上 1.0 以下。
        /// </param>
        /// <returns>
        /// 視線ベクトル値。各要素の絶対値のうち最大の値が 1.0 となるように補正される。
        /// </returns>
        /// <remarks>
        /// Equirectangular projection マッピングの中心座標は Z 軸の正方向とする。
        /// </remarks>
        public static Vector3D EquirectangularUVToEye(double u, double v)
        {
            ValidateRange(u, 0, 1, "u");
            ValidateRange(v, 0, 1, "v");

            // V 成分は視線ベクトルとXZ平面との角度に線形対応
            // V==0.0 => -π/2
            // V==0.5 => 0
            // V==1.0 => +π/2
            var rv = (v - 0.5) * Math.PI;

            // U 成分はXZ平面上における視線ベクトルと Z 軸との角度に線形対応
            // U==0.0 => -π
            // U==0.5 => 0
            // U==1.0 => +π
            var ru = (u - 0.5) * Math.PI * 2;

            // 視線ベクトルを作成
            var cosRV = Math.Cos(rv);
            var eye =
                new Vector3D(cosRV * Math.Sin(ru), -Math.Sin(rv), cosRV * Math.Cos(ru));

            // 絶対値のうち最大の値が 1.0 となるように補正
            var maxAbs =
                Math.Max(Math.Max(Math.Abs(eye.X), Math.Abs(eye.Y)), Math.Abs(eye.Z));
            if (maxAbs > 0)
            {
                eye /= maxAbs;
            }

            return eye;
        }

        /// <summary>
        /// Equirectangular projection マッピングのUV座標を
        /// キューブマップ展開図のUV座標に変換する。
        /// </summary>
        /// <param name="u">
        /// Equirectangular projection マッピングのU座標。 0.0 以上 1.0 以下。
        /// </param>
        /// <param name="v">
        /// Equirectangular projection マッピングのV座標。 0.0 以上 1.0 以下。
        /// </param>
        /// <returns>キューブマップ展開図のUV座標。</returns>
        /// <remarks>
        /// キューブマップ展開図は次のような形であることを想定している。
        /// 
        /// <pre>
        /// |  |+Y|  |  |
        /// |-X|+Z|+X|-Z|
        /// |  |-Y|  |  |
        /// </pre>
        /// 
        /// Equirectangular projection マッピングの中心座標は Z 軸の正方向とする。
        /// </remarks>
        public static Vector EquirectangularUVToCubeUV(double u, double v)
        {
            ValidateRange(u, 0, 1, "u");
            ValidateRange(v, 0, 1, "v");

            // 視線ベクトルを作成
            var eye = EquirectangularUVToEye(u, v);
            eye.Normalize();

            // 視線ベクトルからキューブ展開図のUV値を求める
            var uv = EyeToCubeUV(eye.X, eye.Z);

            // 視線ベクトルの Y 値が負数ならば V 値を反転させる
            if (eye.Y < 0)
            {
                uv.Y = 1 - uv.Y;
            }

            return uv;
        }
    }
}
