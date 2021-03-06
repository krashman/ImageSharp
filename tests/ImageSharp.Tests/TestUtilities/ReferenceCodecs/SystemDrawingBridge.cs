﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Drawing.Imaging;

using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Tests.TestUtilities.ReferenceCodecs
{
    public static class SystemDrawingBridge
    {
        // TODO: It would be nice to have this method in PixelOperations<T>
        private static void ToArgb32<TPixel>(Span<TPixel> source, Span<Argb32> dest)
            where TPixel : struct, IPixel<TPixel>
        {
            int length = source.Length;
            Guard.MustBeSizedAtLeast(dest, length, nameof(dest));

            using (IBuffer<Rgba32> rgbaBuffer = Configuration.Default.MemoryManager.Allocate<Rgba32>(length))
            {
                Span<Rgba32> rgbaSpan = rgbaBuffer.Span;
                PixelOperations<TPixel>.Instance.ToRgba32(source, rgbaSpan, length);

                for (int i = 0; i < length; i++)
                {
                    ref Rgba32 s = ref rgbaSpan[i];
                    ref Argb32 d = ref dest[i];

                    d.PackFromRgba32(s);
                }
            }
        }

        private static void FromArgb32<TPixel>(Span<Argb32> source, Span<TPixel> dest)
            where TPixel : struct, IPixel<TPixel>
        {
            int length = source.Length;
            Guard.MustBeSizedAtLeast(dest, length, nameof(dest));

            using (IBuffer<Rgba32> rgbaBuffer = Configuration.Default.MemoryManager.Allocate<Rgba32>(length))
            {
                Span<Rgba32> rgbaSpan = rgbaBuffer.Span;
                PixelOperations<Argb32>.Instance.ToRgba32(source, rgbaSpan, length);

                for (int i = 0; i < length; i++)
                {
                    ref Rgba32 s = ref rgbaSpan[i];
                    ref TPixel d = ref dest[i];

                    d.PackFromRgba32(s);
                }
            }
        }

        private static void FromRgb24<TPixel>(Span<Rgb24> source, Span<TPixel> dest)
            where TPixel : struct, IPixel<TPixel>
        {
            int length = source.Length;
            Guard.MustBeSizedAtLeast(dest, length, nameof(dest));

            using (IBuffer<Rgb24> rgbBuffer = Configuration.Default.MemoryManager.Allocate<Rgb24>(length))
            {
                Span<Rgb24> rgbSpan = rgbBuffer.Span;
                PixelOperations<Rgb24>.Instance.ToRgb24(source, rgbSpan, length);

                for (int i = 0; i < length; i++)
                {
                    ref Rgb24 s = ref rgbSpan[i];
                    ref TPixel d = ref dest[i];
                    var rgba = default(Rgba32);
                    s.ToRgba32(ref rgba);

                    d.PackFromRgba32(rgba);
                }
            }
        }

        internal static unsafe Image<TPixel> FromFromArgb32SystemDrawingBitmap<TPixel>(System.Drawing.Bitmap bmp)
            where TPixel : struct, IPixel<TPixel>
        {
            int w = bmp.Width;
            int h = bmp.Height;

            var fullRect = new System.Drawing.Rectangle(0, 0, w, h);

            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
            {
                throw new ArgumentException($"FromFromArgb32SystemDrawingBitmap(): pixel format should be Argb32!", nameof(bmp));
            }

            BitmapData data = bmp.LockBits(fullRect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte* sourcePtrBase = (byte*)data.Scan0;

            long sourceRowByteCount = data.Stride;
            long destRowByteCount = w * sizeof(Argb32);

            var image = new Image<TPixel>(w, h);

            using (IBuffer<Argb32> workBuffer = Configuration.Default.MemoryManager.Allocate<Argb32>(w))
            {
                fixed (Argb32* destPtr = &workBuffer.DangerousGetPinnableReference())
                {
                    for (int y = 0; y < h; y++)
                    {
                        Span<TPixel> row = image.Frames.RootFrame.GetPixelRowSpan(y);

                        byte* sourcePtr = sourcePtrBase + data.Stride * y;

                        Buffer.MemoryCopy(sourcePtr, destPtr, destRowByteCount, sourceRowByteCount);

                        FromArgb32(workBuffer.Span, row);
                    }
                }
            }

            return image;
        }

        /// <summary>
        /// TODO: Doesn not work yet!
        /// </summary>
        internal static unsafe Image<TPixel> FromFromRgb24SystemDrawingBitmap<TPixel>(System.Drawing.Bitmap bmp)
            where TPixel : struct, IPixel<TPixel>
        {
            int w = bmp.Width;
            int h = bmp.Height;

            var fullRect = new System.Drawing.Rectangle(0, 0, w, h);

            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new ArgumentException($"FromFromArgb32SystemDrawingBitmap(): pixel format should be Rgb24!", nameof(bmp));
            }

            BitmapData data = bmp.LockBits(fullRect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte* sourcePtrBase = (byte*)data.Scan0;

            long sourceRowByteCount = data.Stride;
            long destRowByteCount = w * sizeof(Rgb24);

            var image = new Image<TPixel>(w, h);

            using (IBuffer<Rgb24> workBuffer = Configuration.Default.MemoryManager.Allocate<Rgb24>(w))
            {
                fixed (Rgb24* destPtr = &workBuffer.DangerousGetPinnableReference())
                {
                    for (int y = 0; y < h; y++)
                    {
                        Span<TPixel> row = image.Frames.RootFrame.GetPixelRowSpan(y);

                        byte* sourcePtr = sourcePtrBase + data.Stride * y;

                        Buffer.MemoryCopy(sourcePtr, destPtr, destRowByteCount, sourceRowByteCount);

                        FromRgb24(workBuffer.Span, row);
                    }
                }
            }

            return image;
        }

        internal static unsafe System.Drawing.Bitmap ToSystemDrawingBitmap<TPixel>(Image<TPixel> image)
            where TPixel : struct, IPixel<TPixel>
        {
            int w = image.Width;
            int h = image.Height;

            var resultBitmap = new System.Drawing.Bitmap(w, h, PixelFormat.Format32bppArgb);
            var fullRect = new System.Drawing.Rectangle(0, 0, w, h);
            BitmapData data = resultBitmap.LockBits(fullRect, ImageLockMode.ReadWrite, resultBitmap.PixelFormat);
            byte* destPtrBase = (byte*)data.Scan0;

            long destRowByteCount = data.Stride;
            long sourceRowByteCount = w * sizeof(Argb32);

            using (IBuffer<Argb32> workBuffer = image.GetConfiguration().MemoryManager.Allocate<Argb32>(w))
            {
                fixed (Argb32* sourcePtr = &workBuffer.DangerousGetPinnableReference())
                {

                    for (int y = 0; y < h; y++)
                    {
                        Span<TPixel> row = image.Frames.RootFrame.GetPixelRowSpan(y);
                        ToArgb32(row, workBuffer.Span);
                        byte* destPtr = destPtrBase + data.Stride * y;

                        Buffer.MemoryCopy(sourcePtr, destPtr, destRowByteCount, sourceRowByteCount);
                    }
                }
            }

            resultBitmap.UnlockBits(data);

            return resultBitmap;
        }
    }
}