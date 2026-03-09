using UnityEngine;

public class SliceableObject : MonoBehaviour
{
    [SerializeField] private bool canBeSliced = true;
    [SerializeField] private Material innerMaterial;
    [SerializeField] private float minSliceSize = 0.1f;

    private const float SeparationForce = 1.5f;
    private const float PieceLifetime = 3f;
    private const float MaxAngularVelocity = 90f;

    private MaterialData materialData;

    public bool CanBeSliced => canBeSliced;
    public MaterialData CurrentMaterialData => materialData;

    public void AssignMaterialData(MaterialData data)
    {
        materialData = data;
        if (data != null && data.InnerMaterial != null)
            innerMaterial = data.InnerMaterial;
    }

    public bool Slice(Vector3 cutStart, Vector3 cutEnd)
    {
        if (!canBeSliced) return false;

        Plane cuttingPlane = ComputeCuttingPlane(cutStart, cutEnd);
        Material surfaceMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        Material capMaterial = innerMaterial != null ? innerMaterial : surfaceMaterial;

        MeshSlicer.SliceResult result = MeshSlicer.Slice(gameObject, cuttingPlane, surfaceMaterial, capMaterial);

        if (!result.IsValid)
        {
            Debug.LogWarning("[SliceableObject] Slice produced no valid result");
            return false;
        }

        if (!MeetsSizeRequirement(result.PositiveSide) || !MeetsSizeRequirement(result.NegativeSide))
        {
            Destroy(result.PositiveSide);
            Destroy(result.NegativeSide);
            return false;
        }

        Vector3 planeNormal = cuttingPlane.normal;

        ConfigureSlicedPiece(result.PositiveSide, capMaterial, planeNormal);
        ConfigureSlicedPiece(result.NegativeSide, capMaterial, -planeNormal);

        CameraShake.Instance?.Shake(0.1f, 0.2f);

        Destroy(gameObject);
        return true;
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
        sliceable.materialData = materialData;

        ConfigureRigidbody(piece, forceDirection);
        ConfigurePhysicsMaterial(piece);

        Destroy(piece, PieceLifetime);
    }

    private void ConfigureRigidbody(GameObject piece, Vector3 forceDirection)
    {
        var rb = piece.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(forceDirection * SeparationForce, ForceMode.Impulse);
        rb.angularVelocity = new Vector3(
            0f, 0f,
            Random.Range(-MaxAngularVelocity, MaxAngularVelocity) * Mathf.Deg2Rad);

        if (materialData != null)
        {
            float gravityScale = materialData.GravityScale;
            rb.mass *= gravityScale;
        }
    }

    private void ConfigurePhysicsMaterial(GameObject piece)
    {
        if (materialData == null || materialData.BounceElasticity <= 0f) return;

        var collider = piece.GetComponent<Collider>();
        if (collider == null) return;

        var physicsMat = new PhysicsMaterial
        {
            bounciness = materialData.BounceElasticity,
            bounceCombine = PhysicsMaterialCombine.Maximum
        };
        collider.material = physicsMat;
    }
}
