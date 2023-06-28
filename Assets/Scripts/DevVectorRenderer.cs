using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DevVectorRenderer : MonoBehaviour
{
    public Material material;

    public struct DevVector 
    {
        public Vector3 start;
        public Vector3 dir;
        public Color color;
        public float duration;
        public float thickness;

        public float createdAt;


        public DevVector(Vector3 start, Vector3 dir, Color color, float duration, float thickness)
        {
            this.start = start;
            this.dir = dir;
            this.color = color;
            this.duration = duration;
            this.thickness = thickness;

            this.createdAt = Time.time;
        }
    }

    public List<DevVector> vectors = new List<DevVector>();

    void OnRenderObject()
    {
        RenderLines();
    }

    void OnDrawGizmos()
    {
        RenderLines();
    }

    void RenderLines()
    {
        GL.Begin(GL.QUADS);
        material.SetPass(0);
        foreach (var vector in vectors)
        {
            if (Time.time - vector.createdAt > vector.duration) continue;
            Vector3 start = vector.start;
            Vector3 end = vector.start + vector.dir;
            Vector3 direction = vector.dir.normalized;
            Vector3 left = Vector3.Cross(direction, transform.forward).normalized;
            Vector3 backward = Vector3.Cross(direction, left).normalized;
            
            GL.Color(vector.color);

            // Bottom
            GL.Vertex(start - left * vector.thickness);
            GL.Vertex(start + backward * vector.thickness);
            GL.Vertex(start + left * vector.thickness);
            GL.Vertex(start - backward * vector.thickness);

            // Top
            GL.Vertex(end - left * vector.thickness);
            GL.Vertex(end - backward * vector.thickness);
            GL.Vertex(end + left * vector.thickness);
            GL.Vertex(end + backward * vector.thickness);

            // Front
            GL.Vertex(start - left * vector.thickness);
            GL.Vertex(start - backward * vector.thickness);
            GL.Vertex(end - backward * vector.thickness);
            GL.Vertex(end - left * vector.thickness);

            // Back
            GL.Vertex(start + left * vector.thickness);
            GL.Vertex(start + backward * vector.thickness);
            GL.Vertex(end + backward * vector.thickness);
            GL.Vertex(end + left * vector.thickness);

            // Left
            GL.Vertex(start + backward * vector.thickness);
            GL.Vertex(start - left * vector.thickness);
            GL.Vertex(end - left * vector.thickness);
            GL.Vertex(end + backward * vector.thickness);

            // Right
            GL.Vertex(start - backward * vector.thickness);
            GL.Vertex(start + left * vector.thickness);
            GL.Vertex(end + left * vector.thickness);
            GL.Vertex(end - backward * vector.thickness);
        }
        GL.End();

        // Remove expired vectors
        vectors.RemoveAll(vector => Time.time - vector.createdAt > vector.duration);
    }

    public void AddDevVector(Vector3 start, Vector3 dir, Color color, float duration, float thickness = 0.02f)
    {
        vectors.Add(new DevVector(start, dir, color, duration, thickness));
    }
}