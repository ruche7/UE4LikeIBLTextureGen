using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace UE4IBLLookUpTextureGen
{
    /// <summary>
    /// 便利処理をまとめた静的クラス。
    /// </summary>
    internal static class Util
    {
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
        /// Hammersley 座標値を求める。
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
    }
}
