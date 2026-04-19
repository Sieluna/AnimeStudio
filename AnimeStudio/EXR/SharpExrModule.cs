using System;
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
    }
}
