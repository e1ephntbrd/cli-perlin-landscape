using System;
using System.Text;
using System.Threading;
using SharpNoise.Modules;

namespace NoiseLandscape;

/// <summary>
/// Main application class that renders a real-time 3D isometric landscape
/// inside the terminal using Perlin Noise.
/// </summary>
class Program
{
    /// <summary>
    /// Application entry point containing the main simulation and rendering loop.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    static void Main(string[] args)
    {
        // Initialize the Perlin Noise generator with a random seed
        var perlin = new Perlin
        {
            Seed = new Random().Next(),
            Frequency = 0.04 // Controls the smoothness of mountains (lower = smoother)
        };

        double timeOffset = 0;
        Console.CursorVisible = false;
        Console.Clear();

        // Ensure the cursor becomes visible again when the user exits via Ctrl+C
        Console.CancelKeyPress += (sender, e) => Console.CursorVisible = true;

        // Main rendering loop
        while (true)
        {
            // Dynamically detect terminal window constraints
            int screenWidth = Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 80;
            int screenHeight = Console.WindowHeight > 0 ? Console.WindowHeight - 1 : 40;

            // Define double-buffered grids for characters and colors to prevent flickering
            char[,] screenBuffer = new char[screenHeight, screenWidth];
            int[,] colorBuffer = new int[screenHeight, screenWidth];

            // Initialize the buffers with empty space (representing the sky/void)
            for (int y = 0; y < screenHeight; y++)
            {
                for (int x = 0; x < screenWidth; x++)
                {
                    screenBuffer[y, x] = ' ';
                }
            }

            // Define the dimensions of the procedural 3D map grid
            int mapSizeX = 60;
            int mapSizeY = 60;

            // Painter's Algorithm: Iterate from back to front to handle depth sorting correctly
            for (int j = 0; j < mapSizeY; j++)
            {
                for (int i = 0; i < mapSizeX; i++)
                {
                    // Sample 3D Perlin Noise based on spatial coordinates and time offset
                    double noise = perlin.GetValue(i * 0.1, j * 0.1, timeOffset);
                    double heightVal = (noise + 1) / 2.0; // Normalize from [-1..1] to [0.0..1.0]

                    // Determine the column height in character cells (max 12 characters tall)
                    int isoHeight = (int)(heightVal * 12); 

                    // --- ISOMETRIC PROJECTION MATRIX ---
                    // Transform 3D world coordinates (i, j, isoHeight) into 2D screen coordinates (screenX, screenY)
                    // The 1.6 and 0.6 multipliers balance the typical rectangular aspect ratio of CLI fonts
                    int screenX = (int)((i - j) * 1.6 + screenWidth / 2);
                    int screenY = (int)((i + j) * 0.6 + (screenHeight / 2) - isoHeight);

                    // Draw the vertical column downward to prevent floating terrain artifacts
                    for (int h = 0; h <= isoHeight; h++)
                    {
                        int curY = screenY + h;
                        
                        // Bounds checking to ensure coordinates fall within the terminal viewport
                        if (screenX >= 0 && screenX < screenWidth && curY >= 0 && curY < screenHeight)
                        {
                            // Assign characters and colors based on elevation topology layers
                            if (h == 0) // Mountain Peak / Surface Layer
                            {
                                screenBuffer[curY, screenX] = GetIsoChar(heightVal);
                                colorBuffer[curY, screenX] = GetIsoColor(heightVal);
                            }
                            else if (h < 3) // Sub-surface / Upper Slope Layer
                            {
                                screenBuffer[curY, screenX] = '.';
                                colorBuffer[curY, screenX] = 244; // Rocky Gray
                            }
                            else // Deep Underground / Bedrock Layer
                            {
                                screenBuffer[curY, screenX] = ':';
                                colorBuffer[curY, screenX] = 236; // Dark Charcoal Gray for depth shading
                            }
                        }
                    }
                }
            }

            // --- CLI RENDER OUTPUT PHASE ---
            Console.SetCursorPosition(0, 0);
            var frame = new StringBuilder();

            for (int y = 0; y < screenHeight; y++)
            {
                int lastColor = -1;
                for (int x = 0; x < screenWidth; x++)
                {
                    int color = colorBuffer[y, x];
                    char ch = screenBuffer[y, x];

                    if (ch == ' ')
                    {
                        frame.Append(' ');
                        lastColor = -1;
                        continue;
                    }

                    // ANSI Optimization: Only emit color sequence if the color state has changed
                    if (color != lastColor)
                    {
                        frame.Append($"\x1b[38;5;{color}m");
                        lastColor = color;
                    }
                    frame.Append(ch);
                }
                if (y < screenHeight - 1) frame.Append('\n');
            }

            // Flush the entire frame buffer to the standard output in a single call
            Console.Write(frame.ToString());

            timeOffset += 0.02; // Progression speed of the map animation
            Thread.Sleep(30);   // Target frame rate ceiling (~30 FPS)
        }
    }

    /// <summary>
    /// Selects an ASCII character representation based on the landscape height value.
    /// </summary>
    /// <param name="height">Normalized elevation value between 0.0 and 1.0.</param>
    /// <returns>An ASCII character mapping to the terrain type.</returns>
    static char GetIsoChar(double height) => height switch
    {
        < 0.40 => '~', // Water Body
        < 0.46 => '.', // Sandy Coastline
        < 0.68 => '#', // Vegetation / Forest Plains
        < 0.85 => '^', // Mountain Rock Face
        _      => '█'  // Snowy Mountain Peak
    };

    /// <summary>
    /// Determines the 8-bit ANSI Xterm color code corresponding to the terrain elevation.
    /// </summary>
    /// <param name="height">Normalized elevation value between 0.0 and 1.0.</param>
    /// <returns>An integer representing a standard 256-color palette index.</returns>
    static int GetIsoColor(double height) => height switch
    {
        < 0.40 => 33,  // Deep Blue
        < 0.46 => 222, // Desert Sand Yellow
        < 0.68 => 28,  // Forest Green
        < 0.85 => 248, // Light Slate Gray
        _      => 15   // Pure White
    };
}
