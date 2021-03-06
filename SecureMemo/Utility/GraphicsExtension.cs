﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SecureMemo.Utility
{
    /// <summary>
    ///     GraphicsExtension
    /// </summary>
    internal static class GraphicsExtension
    {
        /// <summary>
        ///     Generates the rounded rectangle.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        private static GraphicsPath GenerateRoundedRectangle(this Graphics graphics, RectangleF rectangle, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0.0F)
            {
                path.AddRectangle(rectangle);
                path.CloseFigure();
                return path;
            }

            if (radius >= Math.Min(rectangle.Width, rectangle.Height) / 2.0)
                return graphics.GenerateCapsule(rectangle);
            float diameter = radius * 2.0F;
            var sizeF = new SizeF(diameter, diameter);
            var arc = new RectangleF(rectangle.Location, sizeF);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        ///     Generates the capsule.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="baseRect">The base rect.</param>
        /// <returns></returns>
        private static GraphicsPath GenerateCapsule(this Graphics graphics, RectangleF baseRect)
        {
            var path = new GraphicsPath();
            try
            {
                float diameter;
                RectangleF arc;
                if (baseRect.Width > baseRect.Height)
                {
                    diameter = baseRect.Height;
                    var sizeF = new SizeF(diameter, diameter);
                    arc = new RectangleF(baseRect.Location, sizeF);
                    path.AddArc(arc, 90, 180);
                    arc.X = baseRect.Right - diameter;
                    path.AddArc(arc, 270, 180);
                }
                else if (baseRect.Width < baseRect.Height)
                {
                    diameter = baseRect.Width;
                    var sizeF = new SizeF(diameter, diameter);
                    arc = new RectangleF(baseRect.Location, sizeF);
                    path.AddArc(arc, 180, 180);
                    arc.Y = baseRect.Bottom - diameter;
                    path.AddArc(arc, 0, 180);
                }
                else
                {
                    path.AddEllipse(baseRect);
                }
            }
            catch
            {
                path.AddEllipse(baseRect);
            }
            finally
            {
                path.CloseFigure();
            }

            return path;
        }

        /// <summary>
        ///     Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius
        ///     for the arcs that make the rounded edges.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="pen">The pen.</param>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to draw.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to draw.</param>
        /// <param name="width">Width of the rectangle to draw.</param>
        /// <param name="height">Height of the rectangle to draw.</param>
        /// <param name="radius">The radius of the arc used for the rounded edges.</param>
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, float x, float y, float width, float height, float radius)
        {
            var rectangle = new RectangleF(x, y, width, height);
            GraphicsPath path = graphics.GenerateRoundedRectangle(rectangle, radius);
            SmoothingMode old = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawPath(pen, path);
            graphics.SmoothingMode = old;
        }

        /// <summary>
        ///     Draws a rounded rectangle specified by a pair of coordinates, a width, a height and the radius
        ///     for the arcs that make the rounded edges.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="pen">The pen.</param>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to draw.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to draw.</param>
        /// <param name="width">Width of the rectangle to draw.</param>
        /// <param name="height">Height of the rectangle to draw.</param>
        /// <param name="radius">The radius of the arc used for the rounded edges.</param>
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, int x, int y, int width, int height, int radius)
        {
            graphics.DrawRoundedRectangle(pen, Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(width), Convert.ToSingle(height), Convert.ToSingle(radius));
        }

        /// <summary>
        ///     Fills the interior of a rounded rectangle specified by a pair of coordinates, a width, a height
        ///     and the radius for the arcs that make the rounded edges.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="brush">System.Drawing.Brush that determines the characteristics of the fill.</param>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to fill.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to fill.</param>
        /// <param name="width">Width of the rectangle to fill.</param>
        /// <param name="height">Height of the rectangle to fill.</param>
        /// <param name="radius">The radius of the arc used for the rounded edges.</param>
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, float x, float y, float width, float height, float radius)
        {
            var rectangle = new RectangleF(x, y, width, height);
            GraphicsPath path = graphics.GenerateRoundedRectangle(rectangle, radius);
            SmoothingMode old = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillPath(brush, path);
            graphics.SmoothingMode = old;
        }

        /// <summary>
        ///     Fills the interior of a rounded rectangle specified by a pair of coordinates, a width, a height
        ///     and the radius for the arcs that make the rounded edges.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="brush">System.Drawing.Brush that determines the characteristics of the fill.</param>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle to fill.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle to fill.</param>
        /// <param name="width">Width of the rectangle to fill.</param>
        /// <param name="height">Height of the rectangle to fill.</param>
        /// <param name="radius">The radius of the arc used for the rounded edges.</param>
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, int x, int y, int width, int height, int radius)
        {
            graphics.FillRoundedRectangle(brush, Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(width), Convert.ToSingle(height), Convert.ToSingle(radius));
        }

        /// <summary>
        ///     Fills the rounded rectangle.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rect.</param>
        /// <param name="radius">The radius.</param>
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rect, int radius)
        {
            graphics.FillRoundedRectangle(brush, Convert.ToSingle(rect.X), Convert.ToSingle(rect.Y), Convert.ToSingle(rect.Width), Convert.ToSingle(rect.Height), Convert.ToSingle(radius));
        }
    }
}