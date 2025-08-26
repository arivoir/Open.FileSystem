using System;

namespace Open.FileSystem
{
    public class ImageException : Exception
    {
        public ImageException(byte[] imageData) :
            base("Exception containing an image")
        {
            ImageData = imageData;
        }

        public byte[] ImageData { get; private set; }
    }
}
