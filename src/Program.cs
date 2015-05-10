using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace UE4IBLLookUpTextureGen
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
            int lutSize = 256;
            int hammersleySampleCount = 1024;

            // LUT イメージ保存
            Console.WriteLine("Making LUT ...");
            SaveTexture(
                UE4LookUpTextureMaker.Make(lutSize, hammersleySampleCount),
                (args.Length < 1) ? "lut.png" : args[0]);

            // Hammersley Y座標イメージ保存
            Console.WriteLine("Making Y of Hammersley points ...");
            SaveTexture(
                HammersleyYTextureMaker.Make(hammersleySampleCount),
                (args.Length < 2) ? "hammersley_y.png" : args[1]);

            return 0;
        }

        /// <summary>
        /// テクスチャイメージを画像ファイルに保存する。
        /// </summary>
        /// <param name="bmp">テクスチャイメージ。</param>
        /// <param name="filePath">画像ファイルパス。拡張子で形式が決まる。</param>
        private static void SaveTexture(BitmapSource bmp, string filePath)
        {
            // ファイルパスに応じてエンコーダ選択
            BitmapEncoder enc = null;
            switch (Path.GetExtension(filePath).ToLower())
            {
            case ".bmp":
            case ".dib":
                enc = new BmpBitmapEncoder();
                break;

            case ".png":
                enc = new PngBitmapEncoder();
                break;

            case ".jpg":
            case ".jpeg":
                enc = new JpegBitmapEncoder();
                break;

            case ".tif":
            case ".tiff":
                enc = new TiffBitmapEncoder();
                break;

            case ".gif":
                enc = new GifBitmapEncoder();
                break;

            default:
                filePath += ".png";
                enc = new PngBitmapEncoder();
                break;
            }

            // イメージ追加
            enc.Frames.Add(BitmapFrame.Create(bmp));

            // 保存
            using (var s = File.OpenWrite(filePath))
            {
                enc.Save(s);
            }
        }
    }
}
