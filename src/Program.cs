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
            // LUT イメージ作成
            var bmp = UE4LookUpTextureMaker.Make(256, 1024);

            // ファイルパス決定
            var filePath = (args.Length > 0) ? args[0] : "lut.png";

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
                filePath += ".bmp";
                enc = new BmpBitmapEncoder();
                break;
            }

            // イメージ追加
            enc.Frames.Add(BitmapFrame.Create(bmp));

            // 保存
            using (var s = File.OpenWrite(filePath))
            {
                enc.Save(s);
            }

            return 0;
        }
    }
}
