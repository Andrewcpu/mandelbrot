using System;
using System.Windows.Forms;

namespace mandelbrot;

static class Program
{

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MandelbrotViewer(800, 600));
    }
}