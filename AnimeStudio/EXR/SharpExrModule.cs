using System;
using System.Buffers.Binary;
using System.IO;
using EXR;

namespace AnimeStudio
{
    public static class EXRModule
    {
        public static bool TryImportRgbaHalf(byte[] exrData, out ushort[] rgbaHalfData, out int width, out int height, out string error)
        {
            rgbaHalfData = Array.Empty<ushort>();
            width = 0;
            height = 0;
            error = string.Empty;

            if (exrData == null || exrData.Length == 0)
            {
                error = "EXR data is empty.";
                return false;
            }

            try
            {
                using var headerStream = new MemoryStream(exrData, writable: false);
                var exr = EXRFile.FromStream(headerStream);
                if (exr.Parts == null || exr.Parts.Count == 0)
                {
                    error = "EXR has no readable parts.";
                    return false;
                }

                var part = exr.Parts[0];
                using var dataStream = new MemoryStream(exrData, writable: false);
                part.Open(dataStream);
                try
                {
                    var halfData = part.GetHalfs(ChannelConfiguration.RGB, false, GammaEncoding.Linear, true);
                    rgbaHalfData = new ushort[halfData.Length];
                    for (var i = 0; i < halfData.Length; i++)
                    {
                        rgbaHalfData[i] = halfData[i].value;
                    }

                    width = part.DataWindow.Width;
                    height = part.DataWindow.Height;
                    return true;
                }
                finally
                {
                    part.Close();
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryExportRgbaHalf(string outputPath, byte[] rgbaHalfData, int dataLength, int width, int height, bool flipVertically, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                error = "EXR output path is empty.";
                return false;
            }

            if (rgbaHalfData == null || dataLength <= 0)
            {
                error = "EXR source data is empty.";
                return false;
            }

            if (width <= 0 || height <= 0)
            {
                error = "EXR dimensions are invalid.";
                return false;
            }

            int expectedLength;
            try
            {
                expectedLength = checked(width * height * 8);
            }
            catch (OverflowException)
            {
                error = "EXR dimensions are too large.";
                return false;
            }

            if (dataLength < expectedLength || rgbaHalfData.Length < expectedLength)
            {
                error = "EXR source data is shorter than expected RGBA half buffer size.";
                return false;
            }

            try
            {
                byte[] input = PrepareInputData(rgbaHalfData, expectedLength, width, height, flipVertically);
                WriteUncompressedScanlineHalfExr(outputPath, input, width, height);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static byte[] PrepareInputData(byte[] source, int expectedLength, int width, int height, bool flipVertically)
        {
            if (!flipVertically && source.Length == expectedLength)
            {
                return source;
            }

            var output = new byte[expectedLength];
            if (!flipVertically)
            {
                Buffer.BlockCopy(source, 0, output, 0, expectedLength);
                return output;
            }

            int rowByteSize = width * 8;
            for (int y = 0; y < height; y++)
            {
                int srcOffset = y * rowByteSize;
                int dstOffset = (height - y - 1) * rowByteSize;
                Buffer.BlockCopy(source, srcOffset, output, dstOffset, rowByteSize);
            }
            return output;
        }

        private static void WriteUncompressedScanlineHalfExr(string outputPath, byte[] rgbaHalfData, int width, int height)
        {
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new BinaryWriter(fs);

            // OpenEXR magic + version 2 (single-part scanline)
            writer.Write(20000630);
            writer.Write(2);

            WriteHeader(writer, width, height);
            writer.Write((byte)0); // end of header

            int dataSize = checked(width * 8);
            int chunkSize = checked(8 + dataSize);

            long offsetTableStart = writer.BaseStream.Position;
            long chunkDataStart = offsetTableStart + (height * 8L);

            for (int y = 0; y < height; y++)
            {
                long chunkOffset = chunkDataStart + (y * (long)chunkSize);
                writer.Write((uint)chunkOffset);
                writer.Write(0u);
            }

            for (int y = 0; y < height; y++)
            {
                writer.Write(y); // scanline index
                writer.Write(dataSize);

                int rowOffset = y * width * 8;
                // Scanline payload is channel-major in channel list order: A... B... G... R...
                WriteChannelRow(writer, rgbaHalfData, rowOffset, width, channelByteOffset: 6); // A
                WriteChannelRow(writer, rgbaHalfData, rowOffset, width, channelByteOffset: 4); // B
                WriteChannelRow(writer, rgbaHalfData, rowOffset, width, channelByteOffset: 2); // G
                WriteChannelRow(writer, rgbaHalfData, rowOffset, width, channelByteOffset: 0); // R
            }
        }

        private static void WriteChannelRow(BinaryWriter writer, byte[] rgbaHalfData, int rowOffset, int width, int channelByteOffset)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelOffset = rowOffset + x * 8;
                ushort value = BinaryPrimitives.ReadUInt16LittleEndian(rgbaHalfData.AsSpan(pixelOffset + channelByteOffset, 2));
                writer.Write(value);
            }
        }

        private static void WriteHeader(BinaryWriter writer, int width, int height)
        {
            int xMax = width - 1;
            int yMax = height - 1;

            WriteAttribute(writer, "channels", "chlist", valueWriter =>
            {
                WriteHalfChannel(valueWriter, "A");
                WriteHalfChannel(valueWriter, "B");
                WriteHalfChannel(valueWriter, "G");
                WriteHalfChannel(valueWriter, "R");
                valueWriter.Write((byte)0);
            });

            WriteAttribute(writer, "compression", "compression", valueWriter => valueWriter.Write((byte)EXRCompression.None));
            WriteAttribute(writer, "dataWindow", "box2i", valueWriter => WriteBox2I(valueWriter, 0, 0, xMax, yMax));
            WriteAttribute(writer, "displayWindow", "box2i", valueWriter => WriteBox2I(valueWriter, 0, 0, xMax, yMax));
            WriteAttribute(writer, "lineOrder", "lineOrder", valueWriter => valueWriter.Write((byte)LineOrder.IncreasingY));
            WriteAttribute(writer, "pixelAspectRatio", "float", valueWriter => valueWriter.Write(1.0f));
            WriteAttribute(writer, "screenWindowCenter", "v2f", valueWriter =>
            {
                valueWriter.Write(0.0f);
                valueWriter.Write(0.0f);
            });
            WriteAttribute(writer, "screenWindowWidth", "float", valueWriter => valueWriter.Write(1.0f));
        }

        private static void WriteHalfChannel(BinaryWriter writer, string name)
        {
            WriteNullTerminatedString(writer, name);
            writer.Write((int)PixelType.Half);
            writer.Write((byte)0); // pLinear
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(1); // xSampling
            writer.Write(1); // ySampling
        }

        private static void WriteBox2I(BinaryWriter writer, int xMin, int yMin, int xMax, int yMax)
        {
            writer.Write(xMin);
            writer.Write(yMin);
            writer.Write(xMax);
            writer.Write(yMax);
        }

        private static void WriteAttribute(BinaryWriter writer, string name, string type, Action<BinaryWriter> writeValue)
        {
            using var valueStream = new MemoryStream();
            using (var valueWriter = new BinaryWriter(valueStream, System.Text.Encoding.ASCII, true))
            {
                writeValue(valueWriter);
            }

            WriteNullTerminatedString(writer, name);
            WriteNullTerminatedString(writer, type);
            writer.Write((int)valueStream.Length);
            writer.Write(valueStream.GetBuffer(), 0, (int)valueStream.Length);
        }

        private static void WriteNullTerminatedString(BinaryWriter writer, string value)
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes(value));
            writer.Write((byte)0);
        }
    }
}
