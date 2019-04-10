using System;
using System.IO;

using SkiaSharp;

namespace MediaBrowser.Plugins.DVBViewer.Helpers
{
    public static class ImageHelper
    {
        public static void CreateLogoImage(string input, string output)
        {
            try
            {
                var newWidth = 540;
                var newHeight = 540;

                using (var bitmap = SKBitmap.Decode(input))
                {
                    if (bitmap != null)
                    {
                        if (bitmap.Width < bitmap.Height)
                            newWidth = (int)((double)bitmap.Width / (double)bitmap.Height * 540);

                        if (bitmap.Height < bitmap.Width)
                            newHeight = (int)((double)bitmap.Height / (double)bitmap.Width * 540);

                        using (var scaledBitmap = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKBitmapResizeMethod.Lanczos3))
                        {
                            if (scaledBitmap != null)
                            {
                                var toBitmap = new SKBitmap(540, 540);
                                var canvas = new SKCanvas(toBitmap);
                                var paint = new SKPaint()
                                {
                                    Style = SKPaintStyle.Stroke,
                                    Color = SKColors.DimGray,
                                    StrokeWidth = 1,
                                };

                                var x = (540 - scaledBitmap.Width) / 2;
                                var y = (540 - scaledBitmap.Height) / 2;

                                canvas.DrawBitmap(scaledBitmap, x, y);
                                canvas.DrawRect(SKRect.Create(2, 2, 535, 535), paint);
                                canvas.Flush();

                                using (var image = SKImage.FromBitmap(toBitmap))
                                {
                                    using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
                                    {
                                        using (var filestream = File.OpenWrite(output))
                                        {
                                            png.SaveTo(filestream);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Plugin.Logger.Info("IMAGEHELPER > Could not create Logo Image: {0}", output);
            }
        }

        public static void CreatePosterImage(string input, string output)
        {
            try
            {
                using (var bitmap = SKBitmap.Decode(input))
                {
                    if (bitmap != null)
                    {
                        float newWidth = (float)530 / (float)bitmap.Width;
                        float newHeight = (float)800 / (float)bitmap.Height;
                        float scale = Math.Min(newHeight, newWidth);

                        using (var scaledBitmap = bitmap.Resize(new SKImageInfo((int)(bitmap.Width * scale), (int)(bitmap.Height * scale)), SKBitmapResizeMethod.Lanczos3))
                        {
                            if (scaledBitmap != null)
                            {
                                var toBitmap = new SKBitmap(540, 810);
                                var canvas = new SKCanvas(toBitmap);

                                var x = (540 - scaledBitmap.Width) / 2;
                                var y = (810 - scaledBitmap.Height) / 2;

                                canvas.DrawColor(SKColors.White);
                                canvas.DrawBitmap(scaledBitmap, x, y);
                                canvas.Flush();

                                using (var image = SKImage.FromBitmap(toBitmap))
                                {
                                    using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
                                    {
                                        using (var filestream = File.OpenWrite(output))
                                        {
                                            png.SaveTo(filestream);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Plugin.Logger.Info("IMAGEHELPER > Could not create Poster Image: {0}", output);
            }
        }

        public static void CreateLandscapeImage(string input, string output)
        {
            try
            {
                using (var bitmap = SKBitmap.Decode(input))
                {
                    if (bitmap != null)
                    {
                        float newWidth = (float)666 / (float)bitmap.Width;
                        float newHeight = (float)370 / (float)bitmap.Height;
                        float scale = Math.Min(newHeight, newWidth);

                        using (var scaledBitmap = bitmap.Resize(new SKImageInfo((int)(bitmap.Width * scale), (int)(bitmap.Height * scale)), SKBitmapResizeMethod.Lanczos3))
                        {
                            if (scaledBitmap != null)
                            {
                                var toBitmap = new SKBitmap(676, 380);
                                var canvas = new SKCanvas(toBitmap);

                                var x = (676 - scaledBitmap.Width) / 2;
                                var y = (380 - scaledBitmap.Height) / 2;

                                canvas.DrawColor(SKColors.White);
                                canvas.DrawBitmap(scaledBitmap, x, y);
                                canvas.Flush();

                                using (var image = SKImage.FromBitmap(toBitmap))
                                {
                                    using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
                                    {
                                        using (var filestream = File.OpenWrite(output))
                                        {
                                            png.SaveTo(filestream);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Plugin.Logger.Info("IMAGEHELPER > Could not create Landscape Image: {0}", output);
            }
        }
    }
}
