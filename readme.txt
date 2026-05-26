VerticalLineCounter
===================

A Windows console application (.NET Framework 4.7.2) that counts the number
of vertical black lines in a black-and-white JPEG image created with MS Paint.

Usage
-----
  VerticalLineCounter.exe <absolute-path-to-image.jpg>

  Prints a single integer (the line count) to standard output.
  Prints an error message (no crash) if the file is missing or unreadable.
  Prints a usage message if called with the wrong number of arguments.

Example
-------
  VerticalLineCounter.exe "C:\TMMC_interview_assignment\img_1.jpg"

  Always quote the path — paths containing spaces will otherwise be split
  into multiple arguments and the app will print a usage message instead.
  In PowerShell, prefix the exe with .\ to run from the current directory:
    .\VerticalLineCounter.exe "C:\Users\Kaushik Tadhani\Downloads\img_1.jpg"

Build
-----
  Open VerticalLineCounter.slnx in Visual Studio 2017+ and build (Ctrl+Shift+B).
  Output: VerticalLineCounter\bin\Debug\VerticalLineCounter.exe

  Or via MSBuild from the solution root:
    msbuild VerticalLineCounter\VerticalLineCounter.csproj

Test images (expected output)
------------------------------
  img_1.jpg  ->  1
  img_2.jpg  ->  3
  img_3.jpg  ->  1
  img_4.jpg  ->  7

How it works
------------
  1. Loads the JPEG as a System.Drawing.Bitmap.
  2. Reads raw pixel bytes via LockBits (32bppArgb) for efficient access.
  3. For each column, computes perceptual brightness per pixel using the
     ITU-R BT.601 luma formula: brightness = 0.299*R + 0.587*G + 0.114*B
  4. Marks a column as "dark" when enough pixels fall below the brightness
     threshold (accommodates JPEG compression artifacts near hard edges).
  5. Counts contiguous runs of dark columns — each run is one vertical line.


Problems I ran into while building VerticalLineCounter
=======================================================

1. JPEG makes black pixels go gray near the edges  [led to: DarknessThreshold]
   -----------------------------------------------------------------------
   When I tested on img_1.jpg (1 line) and img_2.jpg (3 lines), my first version
   checked if a pixel was pure black (R=0, G=0, B=0). It was giving wrong counts.

   Then, I found JPEG doesn't save exact colours. Even though the lines were drawn
   pure black in MS Paint, saving as JPEG causes the pixels near the edges of each
   line to go slightly gray — values like (30, 30, 30) or (60, 60, 60) instead of (0, 0, 0).
   My pure-black check was skipping all those edge pixels, so thin lines looked
   broken or invisible to the program.

   Fix: instead of checking for exact black, I convert each pixel to a single
   brightness number (0 to 255) and treat anything below 128 as dark enough to count.
   128 is just the halfway point — black pixels land near 0, white pixels near 255,
   and the JPEG gray fringe lands somewhere in between but still well below 128.


2. img_4 has lines that don't go all the way to the top and bottom  [led to: MinDarkFraction]
   --------------------------------------------------------------------------------------------
   img_4 has 7 lines and some of them are noticeably shorter than the image height.
   My first check was: if more than 50% of a column's pixels are dark, it's a line.
   That was missing the shorter lines in img_4 because they only covered maybe 30-40%
   of the column height — nowhere near 50%.

   I also couldn't just set it to 0% any dark pixel counts because JPEG compression
   creates random stray dark pixels in otherwise white areas, which would give false positives.

   Fix: I settled on 5% as the minimum. Even the shortest line in img_4 clears 5% easily,
   and a stray JPEG artifact pixel in a white column would only be 1-2 pixels out of
   hundreds — well under 5%.



3. Why I used the brightness formula instead of just checking R, G, B directly
   -----------------------------------------------------------------------------
   My first instinct was to check each colour channel separately — something like
   "if R < 128 AND G < 128 AND B < 128, it's dark." That works fine for pure black,
   but it breaks down for coloured lines. A pure red pixel is (255, 0, 0) — its G
   and B are already below 128, so it would still get flagged as dark even though
   it's clearly not a black line.

   I needed a single number that represents how light or dark a pixel looks overall,
   regardless of its colour. That's exactly what a brightness formula does
     — it collapses R, G, B into one value between 0 and 255.

   The formula I used:
     brightness = 0.299 * R + 0.587 * G + 0.114 * B

   The weights (0.299, 0.587, 0.114) are not arbitrary. I found this formula on Stack Overflow 
   under "Perceived Luminance (Option 1)". It weights each channel by how sensitive the human eye actually is to it:
     - Green  0.587  — eyes are most sensitive to green
     - Red    0.299  — moderate sensitivity
     - Blue   0.114  — eyes are least sensitive to blue
   The three weights add up to exactly 1.0, so the result always stays in the 0-255 range. 
   The formula originally comes from the NTSC colour television standard (1953).
   Reference: https://stackoverflow.com/questions/596216/formula-to-determine-perceived-brightness-of-rgb-color

   With this formula, a pure red pixel (255, 0, 0) gives brightness = 76 — dark enough that it could trip the 
   threshold on its own. That's why the coloured lines in my custom test image (point 5) came back with 
   brightness above 128 — I had used bright colours like yellow (255, 255, 0) which gives brightness = 225, clearly light. 
   Dark or saturated colours like red and blue would score low and get caught, which is actually the 
   correct behaviour — those are visually dark and the program should treat them that way.


4. img_3 looked like a solid black square, not a line
   ----------------------------------------------------
   When I looked at img_3.jpg it was basically a big black rectangle, not a thin line.
   I wasn't sure if the algorithm would count it as 1 or more.

   Turned out it was fine — because the whole black area is one connected group of dark
   columns with no white gap in between, the program counts it as exactly 1 line.
   The algorithm doesn't care how wide a line is, just how many separate dark groups there are.


5. Running the exe from PowerShell without quoting the path
   ----------------------------------------------------------
   My file path was: C:\Users\Kaushik Tadhani\Downloads\img_1.jpg
   There's a space in "Kaushik Tadhani", so when I ran:

     .\VerticalLineCounter.exe C:\Users\Kaushik Tadhani\Downloads\img_1.jpg

   PowerShell split it into two separate arguments at the space. The program saw
   2 arguments instead of 1 and printed the usage message. Took me a minute to
   figure out why it wasn't working.

   Fix: always wrap the path in double quotes:
     .\VerticalLineCounter.exe "C:\Users\Kaushik Tadhani\Downloads\img_1.jpg"

6. Tested with a custom image — colored lines, black background strip, and an overlap
   ------------------------------------------------------------------------------------
   I created my own test image in MS Paint to push the program a bit further.
   The image had a few vertical black lines, a few vertical lines in other colours
   (like red and blue), a small section with a black background, and one coloured
   line drawn right on top of an existing black line.

   I was worried the coloured lines would throw off the count, or that the
   overlapping line would be counted twice.

   Ran the program and the count came back correct — only the black vertical lines
   were counted. The coloured lines didn't register because their brightness values
   (after running through the formula) came out above 128, so they were treated as
   light/background. The overlap didn't cause a double count either because it was
   sitting on top of an already-dark column — the program just sees one continuous
   dark group, not two separate ones.

   This confirmed the brightness threshold approach works for more than just
   the four original test images.


