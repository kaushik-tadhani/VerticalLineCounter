using System;
using System.Drawing;
using System.IO;

namespace VerticalLineCounter
{
    internal class Program
    {
        // Any pixel with brightness below 128 is treated as black.
        // Brightness goes from 0 (pure black) to 255 (pure white), so 128 is the halfway point.
        // We need this tolerance because JPEG images aren't perfectly black — edges tend to go slightly gray.
        private const double DarknessThreshold = 128.0;

        // At least 5% of a column's pixels must be dark for it to count as part of a line.
        // This prevents JPEG noise from triggering a false line, while still catching
        // shorter lines that don't stretch the full height of the image.
        private const double MinDarkFraction = 0.05;

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: VerticalLineCounter.exe \"<absolute-path-to-image.jpg>\"");
                Console.WriteLine("Example: VerticalLineCounter.exe \"C:\\TMMC_interview_assignment\\img_1.jpg\"");
                Console.WriteLine("Note: quote the path if it contains spaces.");
                return;
            }

            string imagePath = args[0];

            if (!File.Exists(imagePath))
            {
                Console.WriteLine("Error: File not found — " + imagePath);
                Console.WriteLine("Make sure the path is correct and the file exists.");
                return;
            }

            try
            {
                Console.WriteLine("Vertical lines found: " + CountVerticalLines(imagePath));
            }
            catch (Exception ex)
            {
                // Catches anything unexpected — e.g. corrupt image, unsupported format.
                Console.WriteLine("Error reading image: " + ex.Message);
            }
        }

        // The idea here is simple: a vertical line shows up as a column of dark pixels.
        // So we scan every column, check if it's dark enough, then count how many
        // separate groups of dark columns there are — each group is one line.
        private static int CountVerticalLines(string imagePath)
        {
            using (Bitmap bmp = new Bitmap(imagePath))
            {
                int width  = bmp.Width;
                int height = bmp.Height;
                int minDarkPixels = (int)(height * MinDarkFraction); // minimum dark pixels needed per column

                // Go through every column and mark it as dark or not.
                bool[] darkColumn = new bool[width];
                for (int x = 0; x < width; x++)
                {
                    int darkCount = 0;
                    for (int y = 0; y < height; y++)
                    {
                        Color pixel = bmp.GetPixel(x, y);

                        // Perceived Luminance (Option 1) — converts R, G, B into a single brightness value
                        // by weighting each channel by how sensitive the human eye is to it.
                        // Green gets the most weight (0.587), blue the least (0.114).
                        // Source: https://stackoverflow.com/questions/596216/formula-to-determine-perceived-brightness-of-rgb-color
                        double brightness = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;
                        if (brightness < DarknessThreshold)
                            darkCount++;
                    }
                    darkColumn[x] = darkCount >= minDarkPixels;
                }

                // Walk across the image left to right.
                // Every time we move from a light column into a dark one, that's a new line.
                // We don't count again until we've seen white — so wide lines still count as one.
                int lineCount = 0;
                bool inLine   = false;
                for (int x = 0; x < width; x++)
                {
                    if (darkColumn[x] && !inLine)
                    {
                        lineCount++;
                        inLine = true;
                    }
                    else if (!darkColumn[x])
                    {
                        inLine = false;
                    }
                }

                return lineCount;
            }
        }
    }
}
