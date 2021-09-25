using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Custom c# implementation of https://github.com/dfranx/Colly
public class Colly
{
    public enum CollisionType
    {
        None,	// no collision will occur
		Solid,	// collision will be handled
		Cross   // used for coins and other pick ups
    };
    public class Rect
    {
        public float X, Y, Width, Height;
        public Rect()
        {
            X = Y = Width = Height = 0;
        }
        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        public bool Intersects(Rect other, ref Rect intersection)
        {
            // following code is from: https://github.com/SFML/SFML/blob/247b03172c34f25a808bcfdc49f390d619e7d5e0/include/SFML/Graphics/Rect.inl#L109

            // Compute the min and max of the first rectangle on both axes
            float minX1 = Mathf.Min(X, X + Width);
            float maxX1 = Mathf.Max(X, X + Width);
            float minY1 = Mathf.Min(Y, Y + Height);
            float maxY1 = Mathf.Max(Y, Y + Height);


            // Compute the min and max of the second rectangle on both axes
            float minX2 = Mathf.Min(other.X, other.X + other.Width);
            float maxX2 = Mathf.Max(other.X, other.X + other.Width);
            float minY2 = Mathf.Min(other.Y, other.Y + other.Height);
            float maxY2 = Mathf.Max(other.Y, other.Y + other.Height);


            // Compute the intersection boundaries
            float interLeft = Mathf.Max(minX1, minX2);
            float interTop = Mathf.Max(minY1, minY2);
            float interRight = Mathf.Min(maxX1, maxX2);
            float interBottom = Mathf.Min(maxY1, maxY2);


            // If the intersection is valid (positive non zero area), then there is an intersection
            if (interLeft < interRight && interTop < interBottom)
            {
                intersection = new Rect(interLeft, interTop, interRight - interLeft, interBottom - interTop);
                return true;
            }

            intersection = new Rect();
            return false;
        }
        public static bool operator ==(Rect a, Rect b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        public static bool operator !=(Rect a, Rect b) => a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;

        //Added to keep compiler happy, not used
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Rect)obj);
        }
        public override int GetHashCode()
        {
            //Not used so doesn't matter
            return (int)X;
        }
    }
    public class GridWorld
    {
        // create a grid world with given width and height and cell size
        public GridWorld(int width, int height, int cellW, int cellH)
        {
            m_cellH = cellH;
            m_cellW = cellW;
            m_w = width;
            m_h = height;

            m_grid = new int[m_h, m_w];
            GetCollisionType = (int id) => id == 0 || id == 11 ? CollisionType.None : CollisionType.Solid;
        }

        // set/get object on a given position
        public void SetObject(int x, int y, int id) { m_grid[y, x] = id; }
        public int GetObject(int x, int y) { return m_grid[y, x]; }

        // get world size
        public int GetWidth() { return m_w; }
        public int GetHeight() { return m_h; }

        // Check for collision between player and the grid world.
        // NOTE: read World.Check() comment to read about the "steps" parameter

        public Vector2 Check(int steps, Rect bounds, Vector2 goal)
        {

            Rect intersect = new Rect();

            // the increment per axis for each step
            float xInc = (goal.x - bounds.X) / steps;
            float yInc = (goal.y - bounds.Y) / steps;

            // calculate subregion that needs to be checked
            Rect checkRegion = new Rect(Mathf.Min(goal.x, bounds.X), Mathf.Min(goal.y, bounds.Y), Mathf.Max(goal.x, bounds.X +bounds.Width), Mathf.Max(goal.y, bounds.Y + bounds.Height));
            checkRegion.X = Mathf.Max(0, (int)(checkRegion.X - bounds.Width) / m_cellW);
            checkRegion.Y = Mathf.Max(0, (int)(checkRegion.Y - bounds.Height) / m_cellH);
            checkRegion.Width = Mathf.Min((int)(checkRegion.Width + bounds.Width) / m_cellW, m_w - 1);
            checkRegion.Height = Mathf.Min((int)(checkRegion.Height + bounds.Height) / m_cellH, m_h - 1);

            for (int i = 0; i < steps; i++)
            {
                // increment along x axis and check for collision
                bounds.X += xInc;
                for (int y = (int)checkRegion.Y; y <= (int)checkRegion.Height; y++)
                {
                    for (int x = (int)checkRegion.X; x <= (int)checkRegion.Width; x++)
                    {
                        int id = m_grid[y, x];
                        CollisionType type = GetCollisionType(id); // fetch the collision type through the filter GetCollisionType

                        if (type == CollisionType.None) // no collision checking needed? just skip the body
                            continue;

                        Rect cell = new Rect(x* m_cellW, y* m_cellH, m_cellW, m_cellH);

                        if (cell.Intersects(bounds, ref intersect))
                        {
                            // call the user function
                            //if (func != nullptr)
                            //    func(id, x, y, true, this);

                            // only check for collision if we encountered a solid object
                            if (type != CollisionType.Solid)
                                continue;

                            float xInter = intersect.Width;
                            float yInter = intersect.Height;

                            if (xInter < yInter)
                            {
                                if (bounds.X < cell.X)
                                    xInter *= -1; // "bounce" in the direction that depends on the body and user position

                                bounds.X += xInter; // move the user back

                                break;
                            }
                        }
                    }
                }

                // increment along y axis and check for collision - repeat everything for Y axis
                bounds.Y += yInc;
                for (int y = (int)checkRegion.Y; y <= (int)checkRegion.Height; y++)
                {
                    for (int x = (int)checkRegion.X; x <= (int)checkRegion.Width; x++)
                    {
                        int id = m_grid[y, x];
                        CollisionType type = GetCollisionType(id);

                        if (type == CollisionType.None)
                            continue;

                        Rect cell = new Rect(x* m_cellW, y* m_cellH, m_cellW, m_cellH);

                        if (cell.Intersects(bounds, ref intersect))
                        {
                            //if (func != nullptr)
                            //    func(id, x, y, false, this);

                            if (type == CollisionType.Cross)
                                continue;

                            float xInter = intersect.Width;
                            float yInter = intersect.Height;

                            if (yInter < xInter)
                            {
                                if (bounds.Y < cell.Y)
                                    yInter *= -1;

                                bounds.Y += yInter;

                                break;
                            }
                        }
                    }
                }

            }

            return new Vector2(bounds.X, bounds.Y);
        }

        // a "filter" which tells us certain CollisionType for each tile ID
        public Func<int, CollisionType> GetCollisionType;

        private int m_w, m_h, m_cellW, m_cellH;
        private int[,] m_grid;
    }
}
