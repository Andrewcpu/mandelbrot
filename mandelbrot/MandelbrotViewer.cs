namespace mandelbrot;
using System.Drawing.Imaging;

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public partial class MandelbrotViewer : Form
{
    private Bitmap image;
    private double centerReal = -0.75, centerImag = 0.0;
    private double zoom = 1.0;
    private int maxIter = 1000;

    private int lastX, lastY;
    private bool dragging = false;
    private int[] pixels;

    public MandelbrotViewer(int width, int height)
    {
        this.DoubleBuffered = true;  // Enable double buffering
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        this.UpdateStyles();
        this.Width = width;
        this.Height = height;
        InitializeBitmapAndPixels(width, height);

        this.MouseDown += OnMouseDown;
        this.MouseUp += OnMouseUp;
        this.MouseMove += OnMouseMove;
        this.MouseWheel += OnMouseWheel;
        this.Resize += OnResize;  // Handle window resize
        
        RenderMandelbrot();
    }
    private void InitializeBitmapAndPixels(int width, int height)
    {
        this.image = new Bitmap(width, height);
        this.pixels = new int[width * height];
    }
    [DllImport("mandelbrot_support.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void runMandelbrotWithColor(
        int[] image, int width, int height, int maxIter,
        double centerReal, double centerImag, double zoom);

    private void RenderMandelbrot()
    {
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            runMandelbrotWithColor(pixels, Width, Height, maxIter, centerReal, centerImag, zoom);
        }
        finally
        {
            handle.Free();
        }
        UpdateBitmap();
    }


    private void UpdateBitmap()
    {
        // Lock the bitmap's bits for faster access
        Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
        BitmapData bmpData = image.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        // Get a pointer to the first pixel's data
        IntPtr ptr = bmpData.Scan0;
        int bytes = Math.Abs(bmpData.Stride) * image.Height;
        byte[] rgbValues = new byte[bytes];

        // Copy pixel data into the byte array (BGRA format)
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int color = pixels[y * Width + x];
                int red = (color >> 16) & 0xFF;
                int green = (color >> 8) & 0xFF;
                int blue = color & 0xFF;

                int index = (y * bmpData.Stride) + (x * 4);
                rgbValues[index] = (byte)blue;      // Blue
                rgbValues[index + 1] = (byte)green; // Green
                rgbValues[index + 2] = (byte)red;   // Red
                rgbValues[index + 3] = 255;         // Alpha
            }
        }

        // Copy the byte array back to the bitmap
        System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

        // Unlock the bits
        image.UnlockBits(bmpData);

        // Invalidate the form to trigger a repaint
        this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImage(image, 0, 0);
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        lastX = e.X;
        lastY = e.Y;
        dragging = true;
    }

    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        dragging = false;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (dragging)
        {
            double deltaX = (e.X - lastX) * (4.0 / zoom) / Width;
            double deltaY = (e.Y - lastY) * (4.0 / zoom) / Height;

            centerReal -= deltaX;
            centerImag -= deltaY;

            lastX = e.X;
            lastY = e.Y;

            RenderMandelbrot();
            this.Invalidate();
            this.Update();  // Forces the UI to repaint immediately
        }
    }


    private void OnMouseWheel(object sender, MouseEventArgs e)
    {
        zoom *= e.Delta > 0 ? 1.1 : 0.9;
        RenderMandelbrot();
        this.Invalidate();
        this.Update();  // Forces the UI to repaint immediately
    }
    
    private void OnResize(object sender, EventArgs e)
    {
        if (Width > 0 && Height > 0)
        {
            InitializeBitmapAndPixels(Width, Height);  // Reinitialize on resize
            RenderMandelbrot();  // Re-render the Mandelbrot set
            this.Invalidate();
            this.Update();  // Forces the UI to repaint immediately
        }
    }
}
