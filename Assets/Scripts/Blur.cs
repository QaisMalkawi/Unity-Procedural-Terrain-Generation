using UnityEngine;
using System.Collections;

public static class Blur
{

    static float avgR = 0;
    static float avgG = 0;
    static float avgB = 0;
    static float avgA = 0;
    static float blurPixelCount = 0;

    static int radius = 2;
    static int iterations = 2;



    public static Texture2D FastBlur(Texture2D image, int radius, int iterations)
    {
        Texture2D tex = image;

        for(var i = 0;i < iterations;i++)
        {

            tex = BlurImage(tex, tex, radius, true);
            tex = BlurImage(tex, tex, radius, false);

        }

        return tex;
    }



    static Texture2D BlurImage(Texture2D original, Texture2D image, int blurSize, bool horizontal)
    {
        Texture2D blurred = original;
        int _W = image.width;
        int _H = image.height;
        int xx, yy, x, y;

        if(horizontal)
        {
            for(yy = 0;yy < _H;yy++)
            {
                for(xx = 0;xx < _W;xx++)
                {
                    ResetPixel();

                    //Right side of pixel

                    for(x = xx;(x < xx + blurSize && x < _W);x++)
                    {
                        AddPixel(image.GetPixel(x, yy));
                    }

                    //Left side of pixel

                    for(x = xx;(x > xx - blurSize && x > 0);x--)
                    {
                        AddPixel(image.GetPixel(x, yy));

                    }


                    CalcPixel();

                    for(x = xx;x < xx + blurSize && x < _W;x++)
                    {
                        blurred.SetPixel(x, yy, new Color(avgR, avgG, avgB, 1.0f));

                    }
                }
            }
        }

        else
        {
            for(xx = 0;xx < _W - 1;xx++)
            {
                for(yy = 0;yy < _H - 1;yy++)
                {
                    ResetPixel();

                    //Over pixel

                    for(y = yy;(y < yy + blurSize && y < _H);y++)
                    {
                        AddPixel(image.GetPixel(xx, y));
                    }
                    //Under pixel

                    for(y = yy;(y > yy - blurSize && y > 0);y--)
                    {
                        AddPixel(image.GetPixel(xx, y));
                    }
                    CalcPixel();
                    for(y = yy;y < yy + blurSize && y < _H;y++)
                    {
                        blurred.SetPixel(xx, y, new Color(avgR, avgG, avgB, 1.0f));

                    }
                }
            }
        }

        blurred.Apply();
        return blurred;
    }
    static void AddPixel(Color pixel)
    {
        avgR += pixel.r;
        avgG += pixel.g;
        avgB += pixel.b;
        blurPixelCount++;
    }

    static void ResetPixel()
    {
        avgR = 0.0f;
        avgG = 0.0f;
        avgB = 0.0f;
        blurPixelCount = 0;
    }

    static void CalcPixel()
    {
        avgR = avgR / blurPixelCount;
        avgG = avgG / blurPixelCount;
        avgB = avgB / blurPixelCount;
    }
}