using UnityEngine;

public interface IPhysicsMAterial
{
    PhysicsMaterial m_baseMaterial { get; set; }
    PhysicsMaterial m_SlipperyMaterial{ get; set; }

    void ApplySlipperyMaterial();
    void ResetMaterial();


}
