using System;
using System.Drawing;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

using System.Threading;
using WindowsCredentialProviderTest.Internet;

namespace WindowsCredentialProviderTest.QRCode
{
    public sealed class QRCodeBitmap
    {
        //qrCodeURL = "IDG - FIDO://eyJzdGFuIjoiMEZDQzFENjVENkMzOTE2NzJEQzYzODdDNTE0RERDODAiLCJ0eGRhdGVUaW1lIjoiMjAyMjA2MjIxMDIwMzAiLCJpZCI6IkphY2tIdSIsImNoYW5uZWwiOiJTUCJ9";
        
        public static Bitmap GetBitmap(string qrCodeURL)
        {
            //specify desired options
            QrCodeEncodingOptions options = new QrCodeEncodingOptions()
            {
                CharacterSet = "UTF-8",
                DisableECI = true,
                Width = 250,
                Height = 250
            };

            //create new instance and set properties
            BarcodeWriter writer = new BarcodeWriter()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = options
            };

            //create QR code and return Bitmap
            return writer.Write(qrCodeURL);
        }

        //public Bitmap GetBitmap()
        //{

        //    //Decode the base64 string to bytes
        //    byte[] qrCodeBytes = Convert.FromBase64String(qrCodeURL);

        //    //Create a MemoryStream from the decoded bytes
        //    using (MemoryStream ms = new MemoryStream(qrCodeBytes))
        //    {
        //        Image image = Image.FromStream(ms);
        //        //Create a Bitmap from the MemoryStream
        //        Bitmap bitmap = new Bitmap(image);

        //        //Save the Bitmap to a file
        //        //string outputPath = "output.bmp";
        //        //bitmap.Save(outputPath);
        //        return bitmap;
        //    }
        //}
    }
}

