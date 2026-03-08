using UnityEngine;

public class SliceableObject : MonoBehaviour
{
    [SerializeField] private bool canBeSliced = true;
    [SerializeField] private Material innerMaterial;
    [SerializeField] private float minSliceSize = 0.1f;

    private const float SeparationForce = 1.5f;

    public bool CanBeSliced => canBeSliced;

    public void Slice(Vector3 cutStart, Vector3 cutEnd)
    {
        if (!canBeSliced) return;

        Plane cuttingPlane = ComputeCuttingPlane(cutStart, cutEnd);
        Material surfaceMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        Material capMaterial = innerMaterial != null ? innerMaterial : surfaceMaterial;

        MeshSlicer.SliceResult result = MeshSlicer.Slice(gameObject, cuttingPlane, surfaceMaterial, capMaterial);

        if (!result.IsValid)
        {
            Debug.LogWarning("[SliceableObject] Slice produced no valid result");
            return;
        }

        if (!MeetsSizeRequirement(result.PositiveSide) || !MeetsSizeRequirement(result.NegativeSide))
        {
            Destroy(result.PositiveSide);
            Destroy(result.NegativeSide);
            return;
        }

        Vector3 planeNormal = cuttingPlane.normal;

        ConfigureSlicedPiece(result.PositiveSide, capMaterial, planeNormal);
        ConfigureSlicedPiece(result.NegativeSide, capMaterial, -planeNormal);

        CameraShake.Instance?.Shake(0.1f, 0.2f);

        Destroy(gameObject);
    }

    private static Plane ComputeCuttingPlane(Vector3 cutStart, Vector3 cutEnd)
    {
        Vector3 cutDirection = (cutEnd - cutStart).normalized;
        Vector3 planeNormal = Vector3.Cross(cutDirection, Vector3.forward).normalized;
        Vector3 planePoint = (cutStart + cutEnd) * 0.5f;
        return new Plane(planeNormal, planePoint);
    }

    private bool MeetsSizeRequirement(GameObject piece)
    {
        var meshFilter = piece.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return false;

        Bounds bounds = meshFilter.sharedMesh.bounds;
        return bounds.size.x >= minSliceSize &&
               bounds.size.y >= minSliceSize &&
               bounds.size.z >= minSliceSize;
    }

    private void ConfigureSlicedPiece(GameObject piece, Material capMaterial, Vector3 forceDirection)
    {
        var sliceable = piece.AddComponent<SliceableObject>();
        sliceable.canBeSliced = true;
        sliceable.innerMaterial = capMaterial;
        sliceable.minSliceSize = minSliceSize;

        var rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(forceDirection * SeparationForce, ForceMode.Impulse);
    }
}
