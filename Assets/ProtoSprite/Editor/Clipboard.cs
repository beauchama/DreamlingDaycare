using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

namespace ProtoSprite.Editor
{
    public class Clipboard : MonoBehaviour
    {
        [DllImport("ProtoSprite_Clipboard_Windows")] // Replace "YourDLLName" with the name of your DLL
        private static extern IntPtr GetHelloMessage();

        [DllImport("ProtoSprite_Clipboard_Windows")]
        private static extern IntPtr GetClipboardImageData(out int width, out int height, out int size);

        [DllImport("ProtoSprite_Clipboard_Windows")]
        private static extern void FreeClipboardImageData(IntPtr imageData);

        public static Texture2D GetClipboardImage()
        {
            IntPtr imageDataPtr = GetClipboardImageData(out int width, out int height, out int size);

            if (imageDataPtr == IntPtr.Zero)
            {
                Debug.LogError("Failed to retrieve clipboard image data.");
                return null;
            }

            byte[] imageData = new byte[size];
            Marshal.Copy(imageDataPtr, imageData, 0, size);

            // Create a new Texture2D
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Load the PNG data into the texture
            if (!texture.LoadImage(imageData))
            {
                Debug.LogError("Failed to load image data into texture.");
                FreeClipboardImageData(imageDataPtr);
                return null;
            }

            FreeClipboardImageData(imageDataPtr);

            return texture;
        }
    }
}