using System;
using SkiaSharp;

namespace MapTest;

public class CalculateMatrix
{
    /// <summary>
    /// Calculate a matrix that attempts to maximize the element bounds within the viewport
    /// </summary>
    /// <param name="elementBounds">The bounds of the element to scale to</param>
    /// <param name="viewportBounds">The bounds of the viewport</param>
    /// <param name="mapBounds">The total bounds of all map objects</param>
    /// <returns>A <see cref="SKMatrix"/> that applies the scaling and translation</returns>
    public static SKMatrix ForExtent(SKRect elementBounds, SKRect viewportBounds, SKRect mapBounds)
    {
        var paddedElementBounds = elementBounds;

        if (elementBounds != mapBounds)
        {
            // For elements that are smaller thant he total map bounds
            // we want to apply some padding to ensure that the entire
            // element is visible
            paddedElementBounds = Pad(elementBounds, 20);
        }

        var scale = CalculateScale(paddedElementBounds, viewportBounds);
        
        var matrix = SKMatrix.CreateScale(scale, scale, 0, 0);

        // Calculate the _scaled_ position of the center of the element
        var mappedDesiredCenter = matrix.MapPoint(paddedElementBounds.MidX, paddedElementBounds.MidY);

        // Determine by how much to translate so that the center of
        // the element is centered in the viewport
        var translateX = mappedDesiredCenter.X - viewportBounds.MidX;
        var translateY = mappedDesiredCenter.Y - viewportBounds.MidY;

        return matrix.PostConcat(SKMatrix.CreateTranslation(-translateX, -translateY));
    }
    
    /// <summary>
    /// Calculate a matrix that attempts to ensure that all map objects will be visible in the viewport
    /// </summary>
    /// <param name="viewportBounds">The bounds of the viewport</param>
    /// <param name="mapBounds">The total bounds of all map objects</param>
    /// <returns>A <see cref="SKMatrix"/> that applies the scaling and translation</returns>
    public static SKMatrix ToFitViewport(SKRect viewportBounds, SKRect mapBounds)
    {
        var scale = CalculateScale(mapBounds, viewportBounds);

        var matrix = SKMatrix.CreateScale(scale, scale, 0, 0);

        // Calculate the scaled bounds. We need this to center
        // the map when either height or width are not the same
        // as the viewport
        var newBounds = matrix.MapRect(mapBounds);

        // If the scaled bounds are narrower than the viewport
        // calculate the offset so that we horizontally center
        // the map
        var translateX = newBounds.Width < viewportBounds.Width
            ?  (viewportBounds.Width - newBounds.Width) / 2
            : 0;
        
        // If the scaled bounds are shorter than the viewport
        // calculate the offset so that we vertically center
        // the map
        var translateY = newBounds.Height < viewportBounds.Height
            ? (viewportBounds.Height - newBounds.Height) / 2
            : 0f;

        // Handle situations where top/left isn't at the origin
        translateX += -Math.Min(newBounds.Left, 0);
        translateY += -Math.Min(newBounds.Top, 0);

        return matrix.PostConcat(SKMatrix.CreateTranslation(translateX, translateY));
    }

    /// <summary>
    /// Calculate a matrix that attempts to scale to the given level and centered on the given position
    /// </summary>
    /// <param name="scale">The desired scale</param>
    /// <param name="x">The x coordinate to center on</param>
    /// <param name="y">The y coordinate to center on</param>
    /// <param name="centerOnPosition"><c>true</c> when the given position should be centered in the viewport</param>
    /// <param name="mapBounds">The total bounds of all map objects</param>
    /// <param name="viewportBounds">The bounds of the viewport</param>
    /// <param name="viewportCenterPosition"></param>
    /// <returns>A <see cref="SKMatrix"/> that applies the scaling and translation</returns>
    public static SKMatrix ForPoint(
        float scale, 
        float x, 
        float y, 
        bool centerOnPosition, 
        SKRect mapBounds,
        SKRect viewportBounds, 
        SKPoint viewportCenterPosition)
    {
        var scaleMatrix = SKMatrix.CreateScale(scale, scale, 0, 0);
        
        // Calculate the scaled bounds. We need this to center
        // the map when either height or width are not the same
        // as the viewport
        var newBounds = Round(scaleMatrix.MapRect(mapBounds));

        if (IsEntirelyWithin(newBounds, viewportBounds))
        {
            // Clip the lower zoom to ensure that you can't zoom out
            // further than the whole object being visible.
            scale = CalculateScale(mapBounds, viewportBounds);

            scaleMatrix = SKMatrix.CreateScale(scale, scale, x, y);

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

        // Apply the scaling matrix
        var matrix = scaleMatrix;

        if (centerOnPosition)
        {
            var mappedDesiredCenter = matrix.MapPoint(x, y);

            var translateX = mappedDesiredCenter.X - viewportCenterPosition.X;
            var translateY = mappedDesiredCenter.Y - viewportCenterPosition.Y;

            var translateMatrix = SKMatrix.CreateTranslation(-translateX, -translateY);
            
            matrix = matrix.PostConcat(translateMatrix);
            
            newBounds = matrix.MapRect(mapBounds);
        }

        // Ensure that the edges of the map always snap to the edges of the viewport
        if (newBounds.Right < viewportBounds.Right && newBounds.Width >= viewportBounds.Width)
        {
            var offsetX = viewportBounds.Right - newBounds.Right;

            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(offsetX, 0));

            newBounds = matrix.MapRect(mapBounds);
        }
        if(newBounds.Left > viewportBounds.Left && newBounds.Width >= viewportBounds.Width)
        {
            var offsetX = viewportBounds.Left - newBounds.Left;

            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(offsetX, 0));

            newBounds = matrix.MapRect(mapBounds);
        }
        if (newBounds.Top > viewportBounds.Top && newBounds.Height >= viewportBounds.Height)
        {
            var offsetY = viewportBounds.Top - newBounds.Top;

            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(0, offsetY));

            newBounds = matrix.MapRect(mapBounds);
        }
        if (newBounds.Bottom < viewportBounds.Bottom && newBounds.Height >= viewportBounds.Height)
        {
            var offsetY = viewportBounds.Bottom - newBounds.Bottom;

            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(0, offsetY));

            newBounds = matrix.MapRect(mapBounds);
        }
        
        // Recenter vertically
        if (newBounds.Height < viewportBounds.Height && Math.Abs(viewportBounds.MidY - newBounds.MidY) > 0.1)
        {
            var offset = viewportBounds.MidY - newBounds.MidY;

            var translateMatrix = SKMatrix.CreateTranslation(0, offset);

            matrix = matrix.PostConcat(translateMatrix);

            newBounds = matrix.MapRect(mapBounds);
        }
        
        // Recenter horizontally
        if (newBounds.Width < viewportBounds.Width && Math.Abs(viewportBounds.MidX - newBounds.MidX) > 0.1)
        {
            var offset = viewportBounds.MidX - newBounds.MidX;

            var translateMatrix = SKMatrix.CreateTranslation(offset, 0);

            matrix = matrix.PostConcat(translateMatrix);
        }

        return matrix;
    }

    /// <summary>
    /// Calculate a scale that will ensure that the inner bounds fit exactly to the outer bounds
    /// </summary>
    /// <remarks>When the inner bounds are taller than wide, the ratio is recalculated based on height thus ensuring the inner bounds will always fit</remarks>
    public static float CalculateScale(SKRect inner, SKRect outer)
    {
        var scale = outer.Width / inner.Width;

        // Check whether the inner bounds are taller
        // than wide. If that's the case the scale
        // needs to be calculated using height instead.
        if (scale * inner.Height > outer.Height)
        {
            scale = outer.Height / inner.Height;
        }

        return scale;
    }

    /// <summary>
    /// Determine whether the given bounds are (partially) outside of the viewport
    /// </summary>
    /// <param name="inner">The bounds to check</param>
    /// <param name="viewportBounds">The bounds of the viewport</param>
    /// <returns><c>true</c> when any part of the input is outside the viewport, otherwise <c>false</c></returns>
    private static bool IsOutsideViewport(SKRect inner, SKRect viewportBounds)
    {
        return inner.Left < viewportBounds.Left ||
               inner.Top < viewportBounds.Top ||
               inner.Right > viewportBounds.Right ||
               inner.Bottom > viewportBounds.Bottom;
    }

    /// <summary>
    /// Determine whether the given bounds are fully inside of the outer bounds
    /// </summary>
    /// <param name="inner">The bounds to check</param>
    /// <param name="outer">The bounds that <paramref name="inner"/> should fall inside</param>
    /// <returns><c>true</c> when the input is inside the outer bounds, otherwise <c>false</c></returns>
    private static bool IsEntirelyWithin(SKRect inner, SKRect outer)
    {
        return inner.Width < outer.Width && 
               inner.Height < outer.Height;
    }

    /// <summary>
    /// Round the input <see cref="SKRect"/> to values with zero decimals
    /// </summary>
    /// <remarks>
    /// <para>Values are rounded away from zero, <c>9.9</c> will become <c>10</c></para>
    /// <para>This method is useful when working with scaled rectangles where a value approaches a whole number but not quite, for example <c>999.999999</c> instead of <c>1000</c></para></remarks>
    /// <param name="input">The <see cref="SKRect"/> to scale</param>
    /// <returns>A <see cref="SKRect"/> with all values rounded to zero decimals</returns>
    private static SKRect Round(SKRect input)
    {
        return new SKRect(
            (float)Math.Round(input.Left, MidpointRounding.AwayFromZero),
            (float)Math.Round(input.Top, MidpointRounding.AwayFromZero),
            (float)Math.Round(input.Right, MidpointRounding.AwayFromZero),
            (float)Math.Round(input.Bottom, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Increase the size of the given bounds in all directions
    /// </summary>
    /// <param name="input">The <see cref="SKRect"/> to increase</param>
    /// <param name="padding">The value to increase by</param>
    /// <returns>A new <see cref="SKRect"/> instance with increased size</returns>
    private static SKRect Pad(SKRect input, int padding)
    {
        return new SKRect(
            input.Left - padding,
            input.Top - padding,
            input.Right + padding,
            input.Bottom + padding);
    }
}