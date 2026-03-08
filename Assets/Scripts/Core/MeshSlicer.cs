using System.Collections.Generic;
using UnityEngine;

public static class MeshSlicer
{
    public struct SliceResult
    {
        public GameObject PositiveSide;
        public GameObject NegativeSide;
        public bool IsValid;
    }

    public static SliceResult Slice(
        GameObject target,
        Plane cuttingPlane,
        Material surfaceMaterial,
        Material capMaterial)
    {
        var meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return new SliceResult { IsValid = false };

        Mesh originalMesh = meshFilter.sharedMesh;
        Transform targetTransform = target.transform;

        Plane localPlane = TransformPlaneToLocal(cuttingPlane, targetTransform);

        Vector3[] vertices = originalMesh.vertices;
        Vector3[] meshNormals = originalMesh.normals;
        Vector2[] meshUvs = originalMesh.uv;
        int[] triangles = originalMesh.triangles;

        if (meshUvs == null || meshUvs.Length != vertices.Length)
            meshUvs = new Vector2[vertices.Length];

        var positiveMesh = new MeshData();
        var negativeMesh = new MeshData();
        var intersectionEdges = new List<EdgePair>();

        ClassifyAndClipTriangles(
            vertices, meshNormals, meshUvs, triangles,
            localPlane, positiveMesh, negativeMesh, intersectionEdges);

        if (positiveMesh.IsEmpty || negativeMesh.IsEmpty)
            return new SliceResult { IsValid = false };

        GenerateCap(intersectionEdges, localPlane, positiveMesh, negativeMesh);

        Vector3 positiveCenter = positiveMesh.ComputeCenter();
        Vector3 negativeCenter = negativeMesh.ComputeCenter();
        positiveMesh.OffsetVertices(-positiveCenter);
        negativeMesh.OffsetVertices(-negativeCenter);

        Vector3 worldPositiveCenter = targetTransform.TransformPoint(positiveCenter);
        Vector3 worldNegativeCenter = targetTransform.TransformPoint(negativeCenter);

        return new SliceResult
        {
            PositiveSide = BuildSlicedGameObject(
                positiveMesh, "SlicedPiece_Pos",
                worldPositiveCenter, targetTransform.rotation, targetTransform.lossyScale,
                surfaceMaterial, capMaterial),
            NegativeSide = BuildSlicedGameObject(
                negativeMesh, "SlicedPiece_Neg",
                worldNegativeCenter, targetTransform.rotation, targetTransform.lossyScale,
                surfaceMaterial, capMaterial),
            IsValid = true
        };
    }

    private static Plane TransformPlaneToLocal(Plane worldPlane, Transform transform)
    {
        Vector3 localNormal = transform.InverseTransformDirection(worldPlane.normal).normalized;
        Vector3 worldPoint = worldPlane.normal * (-worldPlane.distance);
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        return new Plane(localNormal, localPoint);
    }

    private static void ClassifyAndClipTriangles(
        Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles,
        Plane plane,
        MeshData positiveMesh, MeshData negativeMesh,
        List<EdgePair> intersectionEdges)
    {
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];

            var v0 = vertices[i0]; var v1 = vertices[i1]; var v2 = vertices[i2];
            var n0 = normals[i0];  var n1 = normals[i1];  var n2 = normals[i2];
            var uv0 = uvs[i0];    var uv1 = uvs[i1];    var uv2 = uvs[i2];

            float d0 = plane.GetDistanceToPoint(v0);
            float d1 = plane.GetDistanceToPoint(v1);
            float d2 = plane.GetDistanceToPoint(v2);

            bool p0 = d0 >= 0f, p1 = d1 >= 0f, p2 = d2 >= 0f;

            if (p0 && p1 && p2)
            {
                positiveMesh.AddTriangle(v0, v1, v2, n0, n1, n2, uv0, uv1, uv2);
            }
            else if (!p0 && !p1 && !p2)
            {
                negativeMesh.AddTriangle(v0, v1, v2, n0, n1, n2, uv0, uv1, uv2);
            }
            else
            {
                ClipTriangle(
                    v0, v1, v2, n0, n1, n2, uv0, uv1, uv2,
                    d0, d1, d2, p0, p1, p2,
                    positiveMesh, negativeMesh, intersectionEdges);
            }
        }
    }

    private static void ClipTriangle(
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 n0, Vector3 n1, Vector3 n2,
        Vector2 uv0, Vector2 uv1, Vector2 uv2,
        float d0, float d1, float d2,
        bool p0, bool p1, bool p2,
        MeshData positiveMesh, MeshData negativeMesh,
        List<EdgePair> intersectionEdges)
    {
        // Rotate vertices so the "solo" vertex (the one on a different side) is first.
        // Rotating preserves winding order.
        if (p0 != p1 && p0 != p2)
        {
            MeshData soloMesh = p0 ? positiveMesh : negativeMesh;
            MeshData pairMesh = p0 ? negativeMesh : positiveMesh;
            ClipWithSoloFirst(
                v0, v1, v2, n0, n1, n2, uv0, uv1, uv2,
                d0, d1, d2, soloMesh, pairMesh, intersectionEdges);
        }
        else if (p1 != p0 && p1 != p2)
        {
            MeshData soloMesh = p1 ? positiveMesh : negativeMesh;
            MeshData pairMesh = p1 ? negativeMesh : positiveMesh;
            ClipWithSoloFirst(
                v1, v2, v0, n1, n2, n0, uv1, uv2, uv0,
                d1, d2, d0, soloMesh, pairMesh, intersectionEdges);
        }
        else
        {
            MeshData soloMesh = p2 ? positiveMesh : negativeMesh;
            MeshData pairMesh = p2 ? negativeMesh : positiveMesh;
            ClipWithSoloFirst(
                v2, v0, v1, n2, n0, n1, uv2, uv0, uv1,
                d2, d0, d1, soloMesh, pairMesh, intersectionEdges);
        }
    }

    private static void ClipWithSoloFirst(
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 n0, Vector3 n1, Vector3 n2,
        Vector2 uv0, Vector2 uv1, Vector2 uv2,
        float d0, float d1, float d2,
        MeshData soloMesh, MeshData pairMesh,
        List<EdgePair> intersectionEdges)
    {
        float t01 = d0 / (d0 - d1);
        float t02 = d0 / (d0 - d2);

        Vector3 intersection1 = Vector3.Lerp(v0, v1, t01);
        Vector3 intersection2 = Vector3.Lerp(v0, v2, t02);
        Vector3 normalAtI1 = Vector3.Lerp(n0, n1, t01).normalized;
        Vector3 normalAtI2 = Vector3.Lerp(n0, n2, t02).normalized;
        Vector2 uvAtI1 = Vector2.Lerp(uv0, uv1, t01);
        Vector2 uvAtI2 = Vector2.Lerp(uv0, uv2, t02);

        soloMesh.AddTriangle(
            v0, intersection1, intersection2,
            n0, normalAtI1, normalAtI2,
            uv0, uvAtI1, uvAtI2);

        pairMesh.AddTriangle(
            intersection1, v1, v2,
            normalAtI1, n1, n2,
            uvAtI1, uv1, uv2);

        pairMesh.AddTriangle(
            intersection1, v2, intersection2,
            normalAtI1, n2, normalAtI2,
            uvAtI1, uv2, uvAtI2);

        intersectionEdges.Add(new EdgePair(intersection1, intersection2));
    }

    private static void GenerateCap(
        List<EdgePair> edges,
        Plane localPlane,
        MeshData positiveMesh, MeshData negativeMesh)
    {
        if (edges.Count == 0) return;

        Vector3 centroid = ComputeEdgeCentroid(edges);
        Vector3 planeNormal = localPlane.normal;

        Vector3 uAxis = ComputePlaneTangent(planeNormal);
        Vector3 vAxis = Vector3.Cross(planeNormal, uAxis).normalized;

        Vector2 centroidUv = ProjectToCapUv(centroid, centroid, uAxis, vAxis);

        foreach (EdgePair edge in edges)
        {
            Vector2 uvStart = ProjectToCapUv(edge.Start, centroid, uAxis, vAxis);
            Vector2 uvEnd = ProjectToCapUv(edge.End, centroid, uAxis, vAxis);

            Vector3 cross = Vector3.Cross(edge.Start - centroid, edge.End - centroid);
            bool alignedWithNormal = Vector3.Dot(cross, planeNormal) > 0f;

            if (alignedWithNormal)
            {
                positiveMesh.AddCapTriangle(
                    centroid, edge.Start, edge.End,
                    planeNormal, centroidUv, uvStart, uvEnd);
                negativeMesh.AddCapTriangle(
                    centroid, edge.End, edge.Start,
                    -planeNormal, centroidUv, uvEnd, uvStart);
            }
            else
            {
                positiveMesh.AddCapTriangle(
                    centroid, edge.End, edge.Start,
                    planeNormal, centroidUv, uvEnd, uvStart);
                negativeMesh.AddCapTriangle(
                    centroid, edge.Start, edge.End,
                    -planeNormal, centroidUv, uvStart, uvEnd);
            }
        }
    }

    private static Vector3 ComputeEdgeCentroid(List<EdgePair> edges)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (EdgePair edge in edges)
        {
            sum += edge.Start + edge.End;
            count += 2;
        }
        return sum / count;
    }

    private static Vector2 ProjectToCapUv(Vector3 point, Vector3 center, Vector3 uAxis, Vector3 vAxis)
    {
        Vector3 offset = point - center;
        return new Vector2(
            Vector3.Dot(offset, uAxis) * 0.5f + 0.5f,
            Vector3.Dot(offset, vAxis) * 0.5f + 0.5f);
    }

    private static Vector3 ComputePlaneTangent(Vector3 normal)
    {
        Vector3 reference = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.9f
            ? Vector3.up
            : Vector3.right;
        return Vector3.Cross(normal, reference).normalized;
    }

    private static GameObject BuildSlicedGameObject(
        MeshData meshData, string objectName,
        Vector3 position, Quaternion rotation, Vector3 scale,
        Material surfaceMaterial, Material capMaterial)
    {
        Mesh mesh = meshData.BuildMesh();

        var go = new GameObject(objectName);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        go.AddComponent<MeshFilter>().mesh = mesh;

        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.materials = new[] { surfaceMaterial, capMaterial };

        var meshCollider = go.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        return go;
    }

    private readonly struct EdgePair
    {
        public readonly Vector3 Start;
        public readonly Vector3 End;

        public EdgePair(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }
    }

    private class MeshData
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<int> surfaceTriangles = new();
        private readonly List<int> capTriangles = new();

        public bool IsEmpty => surfaceTriangles.Count == 0;

        public void AddTriangle(
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 n0, Vector3 n1, Vector3 n2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            int baseIndex = vertices.Count;
            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);
            normals.Add(n0); normals.Add(n1); normals.Add(n2);
            uvs.Add(uv0); uvs.Add(uv1); uvs.Add(uv2);
            surfaceTriangles.Add(baseIndex);
            surfaceTriangles.Add(baseIndex + 1);
            surfaceTriangles.Add(baseIndex + 2);
        }

        public void AddCapTriangle(
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 capNormal,
            Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            int baseIndex = vertices.Count;
            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);
            normals.Add(capNormal); normals.Add(capNormal); normals.Add(capNormal);
            uvs.Add(uv0); uvs.Add(uv1); uvs.Add(uv2);
            capTriangles.Add(baseIndex);
            capTriangles.Add(baseIndex + 1);
            capTriangles.Add(baseIndex + 2);
        }

        public Vector3 ComputeCenter()
        {
            if (vertices.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (Vector3 v in vertices) sum += v;
            return sum / vertices.Count;
        }

        public void OffsetVertices(Vector3 offset)
        {
            for (int i = 0; i < vertices.Count; i++)
                vertices[i] = vertices[i] + offset;
        }

        public Mesh BuildMesh()
        {
            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray(),
                subMeshCount = 2
            };
            mesh.SetTriangles(surfaceTriangles, 0);
            mesh.SetTriangles(capTriangles, 1);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
