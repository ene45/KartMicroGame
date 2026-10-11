using System.Collections.Generic;
using UnityEngine;

namespace MedievalCartoon.Editor
{
    // Source meshes are stored as native assets; no runtime mesh generation is required.
    internal static class MedievalMeshes
    {
        sealed class Writer
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> indices = new List<int>();
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Cross(b-a,c-a).sqrMagnitude < .00000001f) return;
                if (Vector3.Dot(Vector3.Cross(b-a,c-a), outward) < 0) { var t=b; b=c; c=t; }
                int n=vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                foreach (var p in new[] {a,b,c})
                {
                    Vector3 d=new Vector3(Mathf.Abs(outward.x),Mathf.Abs(outward.y),Mathf.Abs(outward.z));
                    uv.Add(d.y>d.x && d.y>d.z ? new Vector2(p.x,p.z) : d.x>d.z ? new Vector2(p.z,p.y) : new Vector2(p.x,p.y));
                }
                indices.Add(n); indices.Add(n+1); indices.Add(n+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
            { Tri(a,b,c,outward); Tri(a,c,d,outward); }
            public Mesh Finish(string name)
            {
                var m=new Mesh {name=name};
                m.SetVertices(vertices); m.SetUVs(0,uv); m.SetTriangles(indices,0);
                m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
                return m;
            }
        }

        public static Mesh RoundedBox()
        {
            var w=new Writer();
            float[] grid={-.5f,-.40f,.40f,.5f};
            Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(var n in normals)
            {
                Vector3 u = Mathf.Abs(n.y)>.9f ? Vector3.right : Vector3.up;
                Vector3 v=Vector3.Cross(n,u);
                for(int i=0;i<3;i++) for(int j=0;j<3;j++)
                {
                    Vector3 a=Round(n*.5f+u*grid[i]+v*grid[j]);
                    Vector3 b=Round(n*.5f+u*grid[i+1]+v*grid[j]);
                    Vector3 c=Round(n*.5f+u*grid[i+1]+v*grid[j+1]);
                    Vector3 d=Round(n*.5f+u*grid[i]+v*grid[j+1]);
                    w.Quad(a,b,c,d,n);
                }
            }
            return w.Finish("Bloque_Biselado");
        }
        static Vector3 Round(Vector3 p)
        {
            Vector3 q=new Vector3(Mathf.Clamp(p.x,-.4f,.4f),Mathf.Clamp(p.y,-.4f,.4f),Mathf.Clamp(p.z,-.4f,.4f));
            return q+(p-q).normalized*.1f;
        }
        public static Mesh Lathe(string name,Vector2[] profile,int sides=16)
        {
            var w=new Writer();
            for(int r=0;r<profile.Length-1;r++) for(int i=0;i<sides;i++)
            {
                float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
                Vector3 p=new Vector3(Mathf.Cos(a)*profile[r].x,profile[r].y,Mathf.Sin(a)*profile[r].x);
                Vector3 q=new Vector3(Mathf.Cos(b)*profile[r].x,profile[r].y,Mathf.Sin(b)*profile[r].x);
                Vector3 s=new Vector3(Mathf.Cos(a)*profile[r+1].x,profile[r+1].y,Mathf.Sin(a)*profile[r+1].x);
                Vector3 t=new Vector3(Mathf.Cos(b)*profile[r+1].x,profile[r+1].y,Mathf.Sin(b)*profile[r+1].x);
                Vector2 edge=profile[r+1]-profile[r];
                Vector3 normal=new Vector3(Mathf.Cos((a+b)*.5f)*edge.y,-edge.x,Mathf.Sin((a+b)*.5f)*edge.y);
                w.Quad(p,q,t,s,normal);
            }
            return w.Finish(name);
        }
        public static Mesh Sphere()
        {
            var profile=new List<Vector2>();
            for(int i=0;i<=12;i++) {float a=i*Mathf.PI/12;profile.Add(new Vector2(Mathf.Sin(a),-Mathf.Cos(a)));}
            return Lathe("Esfera_Facetada",profile.ToArray(),20);
        }
        public static Mesh Extrude(string name,Vector2[] points,float depth)
        {
            var w=new Writer(); Vector2 center=Vector2.zero;
            foreach(var p in points)center+=p; center/=points.Length;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Length];
                w.Tri(new Vector3(center.x,center.y,-depth/2),new Vector3(a.x,a.y,-depth/2),new Vector3(b.x,b.y,-depth/2),Vector3.back);
                w.Tri(new Vector3(center.x,center.y,depth/2),new Vector3(a.x,a.y,depth/2),new Vector3(b.x,b.y,depth/2),Vector3.forward);
                w.Quad(new Vector3(a.x,a.y,-depth/2),new Vector3(b.x,b.y,-depth/2),new Vector3(b.x,b.y,depth/2),new Vector3(a.x,a.y,depth/2),new Vector3(b.y-a.y,a.x-b.x,0));
            }
            return w.Finish(name);
        }
        public static Mesh Arc(float radius,float thickness,float degrees,float depth)
        {
            var w=new Writer(); int steps=Mathf.CeilToInt(degrees/10);
            for(int i=0;i<steps;i++)
            {
                float a=i*degrees/steps*Mathf.Deg2Rad,b=(i+1)*degrees/steps*Mathf.Deg2Rad;
                Vector3 innerA=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
                Vector3 innerB=new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0);
                Vector3 outerA=innerA.normalized*(radius+thickness),outerB=innerB.normalized*(radius+thickness);
                Vector3 z=Vector3.forward*depth*.5f,n=(innerA+innerB).normalized;
                w.Quad(innerA+z,innerB+z,outerB+z,outerA+z,Vector3.forward);
                w.Quad(outerA-z,outerB-z,innerB-z,innerA-z,Vector3.back);
                w.Quad(outerA+z,outerB+z,outerB-z,outerA-z,n);
                w.Quad(innerB+z,innerA+z,innerA-z,innerB-z,-n);
                if(i==0)w.Quad(innerA-z,innerA+z,outerA+z,outerA-z,new Vector3(0,-1,0));
                if(i==steps-1)w.Quad(outerB-z,outerB+z,innerB+z,innerB-z,new Vector3(-Mathf.Sin(b),Mathf.Cos(b),0));
            }
            return w.Finish("Arco_"+radius+"_"+degrees);
        }
        public static Mesh Banner(bool longBanner)
        {
            var w=new Writer(); float height=longBanner ? 3:1.7f;
            for(int y=0;y<8;y++) for(int x=0;x<6;x++)
            {
                Vector3 a=Cloth(x,y,height),b=Cloth(x+1,y,height),c=Cloth(x+1,y+1,height),d=Cloth(x,y+1,height);
                w.Quad(a,b,c,d,Vector3.forward);w.Quad(d,c,b,a,Vector3.back);
            }
            return w.Finish("Estandarte_"+height);
        }
        static Vector3 Cloth(int x,int y,float height)
        {
            float u=x/6f,v=y/8f;
            return new Vector3((u-.5f)*1.4f,-v*height-(y==8 ? (1-Mathf.Abs(u-.5f)*2)*.4f:0),Mathf.Sin(u*6.28f+v*2)*.08f*v);
        }
    }
}
