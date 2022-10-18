using System;
using SkiaSharp;

namespace MapTest;

public class CalculateMatrix
{
    public static SKMatrix ForExtent(SKRect elementBounds, SKRect viewportBounds, SKRect mapBounds)
    {
        var paddedElementBounds = elementBounds;

        if (elementBounds != mapBounds)
        {
            paddedElementBounds = Pad(elementBounds, 20);
        }

        var zoomLevel = CalculateScale(
            viewportBounds.Width,
            viewportBounds.Height,
            paddedElementBounds.Width,
            paddedElementBounds.Height);

        var scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, 0, 0);

        // Apply the scaling matrix
        var matrix = scaleMatrix;

        var mappedDesiredCenter = matrix.MapPoint(paddedElementBounds.MidX, paddedElementBounds.MidY);
        
        var translateX = mappedDesiredCenter.X - viewportBounds.MidX;
        var translateY = mappedDesiredCenter.Y - viewportBounds.MidY;

        var translateMatrix = SKMatrix.CreateTranslation(-translateX, -translateY);

        matrix = matrix.PostConcat(translateMatrix);

        return matrix;
    }

    public static float CalculateScale(float outerWidth, float outerHeight, float innerWidth, float innerHeight)
    {
        var scale = outerWidth / innerWidth;

        // Check whether the inner bounds are taller
        // than wide. If that's the case the scale
        // needs to be calculated using height instead.
        if (scale * innerHeight > outerHeight)
        {
            scale = outerHeight / innerHeight;
        }

        return scale;
    }

    private static SKRect Pad(SKRect bounds, int padding)
    {
        return new SKRect(
            bounds.Left - padding,
            bounds.Top - padding,
            bounds.Right + padding,
            bounds.Bottom + padding);
    }

    public static SKMatrix ToFitViewport(SKRect viewportBounds, SKRect mapBounds)
    {
        var zoomLevel = CalculateScale(
            viewportBounds.Width,
            viewportBounds.Height,
            mapBounds.Width,
            mapBounds.Height);

        var scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, 0, 0);
        var newBounds = scaleMatrix.MapRect(mapBounds);

        var matrix = scaleMatrix;

        var translateX = 0f;
        var translateY = 0f;

        if (newBounds.Width < viewportBounds.Width)
        {
            // Center horizontally
            translateX = (viewportBounds.Width - newBounds.Width) / 2;
        }

        if (newBounds.Height < viewportBounds.Height)
        {
            // Center vertically
            translateY = (viewportBounds.Height - newBounds.Height) / 2;
        }

        // Handle situations where top/left isn't at the origin
        translateX += -Math.Min(newBounds.Left, 0);
        translateY += -Math.Min(newBounds.Top, 0);

        var translate = SKMatrix.CreateTranslation(translateX, translateY);

        matrix = matrix.PostConcat(translate);

        return matrix;
    }

    public static SKMatrix ForPoint(float zoomLevel, float x, float y, bool centerOnPosition, SKRect mapBounds, SKRect viewportBounds)
    {
        var scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, 0, 0);

        var newBounds = Round(scaleMatrix.MapRect(mapBounds));

        if (IsEntirelyWithin(newBounds, viewportBounds))
        {
            // Clip the lower zoom to ensure that you can't zoom out
            // further than the whole object being visible.
            //AdjustZoomLevelToMapObjectBounds();
            zoomLevel = CalculateScale(
                viewportBounds.Width,
                viewportBounds.Height,
                mapBounds.Width,
                mapBounds.Height);

            scaleMatrix = SKMatrix.CreateScale(zoomLevel, zoomLevel, x, y);

            // Ensure that when zooming out the bitmap never
            // appears away from the origin (top/left 0,0)
            // so that there won't be any gaps on screen.
            var topLeftMapped = scaleMatrix.MapPoint(mapBounds.Left, mapBounds.Top);

            if (topLeftMapped.X < 0 || topLeftMapped.Y < 0)
            {
                var scaleTranslateX = -Math.Min(0, topLeftMapped.X);
                var scaleTranslateY = -Math.Min(0, topLeftMapped.Y);

                if (scaleTranslateX != 0 || scaleTranslateY != 0)
                {
                    scaleMatrix = scaleMatrix.PostConcat(SKMatrix.CreateTranslation(scaleTranslateX, scaleTranslateY));
                }
            }

            // Update new bounds
            newBounds = scaleMatrix.MapRect(mapBounds);
        }

        if (IsEntirelyWithin(newBounds, viewportBounds) &&
            IsOutsideViewport(newBounds, viewportBounds))
        {
            var translateX = -Math.Min(newBounds.Left, 0);
            var translateY = -Math.Min(newBounds.Top, 0);

            var translateMatrix = SKMatrix.CreateTranslation(translateX, translateY);

            scaleMatrix = scaleMatrix.PostConcat(translateMatrix);

            // Update new bounds
            newBounds = scaleMatrix.MapRect(mapBounds);
        }

        if (newBounds.Width < viewportBounds.Width)
        {
            // As we've already corrected for the aspect ratio
            // of the map objects bitmap, it turns out that
            // this one is indeed taller than wide.
            // To make it look nice we should center the
            // bitmap.
            var offset = (viewportBounds.Width - newBounds.Width) / 2;

            var translateMatrix = SKMatrix.CreateTranslation(offset, 0);

            scaleMatrix = scaleMatrix.PostConcat(translateMatrix);

            newBounds = translateMatrix.MapRect(newBounds);
        }

        if (newBounds.Height < viewportBounds.Height)
        {
            // As we've already corrected for the aspect ratio
            // of the map objects bitmap, it turns out that
            // this one is indeed wider than tall.
            // To make it look nice we should center the
            // bitmap.
            var offset = (viewportBounds.Height - newBounds.Height) / 2;

            var shift = -x;

            if (newBounds.Right + shift < viewportBounds.Right)
            {
                shift = viewportBounds.Right - newBounds.Right;
            }

            var translateMatrix = SKMatrix.CreateTranslation(
                centerOnPosition
                    ? 0
                    : shift,
                offset);

            scaleMatrix = scaleMatrix.PostConcat(translateMatrix);

            newBounds = translateMatrix.MapRect(newBounds);
        }

        if ((newBounds.Left < 0 || newBounds.Top < 0) &&
            newBounds.Width <= viewportBounds.Width &&
            newBounds.Height <= viewportBounds.Height)
        {
            var translateMatrix = SKMatrix.CreateTranslation(
                -Math.Min(0, newBounds.Left),
                -Math.Min(0, newBounds.Top));

            scaleMatrix = scaleMatrix.PostConcat(translateMatrix);
        }

        // Apply the scaling matrix
        var matrix = scaleMatrix;

        if (centerOnPosition)
        {
            var mappedDesiredCenter = matrix.MapPoint(x, y);

            var translateX = mappedDesiredCenter.X - viewportBounds.MidX;
            var translateY = mappedDesiredCenter.Y - viewportBounds.MidY;

            var translateMatrix = SKMatrix.CreateTranslation(-translateX, -translateY);

            matrix = matrix.PostConcat(translateMatrix);
        }

        return matrix;
    }

    private static bool IsOutsideViewport(SKRect inner, SKRect outer)
    {
        return inner.Left < outer.Left ||
               inner.Top < outer.Top ||
               inner.Right > outer.Right ||
               inner.Bottom > outer.Bottom;
    }

    private static bool IsEntirelyWithin(SKRect inner, SKRect outer)
    {
        return inner.Width < outer.Width && inner.Height < outer.Height;
    }

    private static SKRect Round(SKRect rect)
    {
        return new SKRect(
            (float)Math.Round(rect.Left, MidpointRounding.AwayFromZero),
            (float)Math.Round(rect.Top, MidpointRounding.AwayFromZero),
            (float)Math.Round(rect.Right, MidpointRounding.AwayFromZero),
            (float)Math.Round(rect.Bottom, MidpointRounding.AwayFromZero));
    }
}