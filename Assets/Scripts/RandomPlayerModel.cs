using UnityEngine;
using Unity.Netcode;

public class RandomPlayerModel : NetworkBehaviour
{
    [Header("Models")]
    public GameObject[] models;   // Drag all your character models here in Inspector

    // Synced so all clients see the same model for each player
    private NetworkVariable<int> modelIndex = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Server picks a random model index
        if (IsServer)
            modelIndex.Value = Random.Range(0, models.Length);

        // All clients apply the chosen model
        modelIndex.OnValueChanged += OnModelIndexChanged;
        ApplyModel(modelIndex.Value);
    }

    void OnModelIndexChanged(int oldIndex, int newIndex)
    {
        ApplyModel(newIndex);
    }

    void ApplyModel(int index)
    {
        if (models == null || models.Length == 0) return;

        // Disable all models first
        for (int i = 0; i < models.Length; i++)
        {
            if (models[i] != null)
                models[i].SetActive(false);
        }

        // Enable the chosen one
        if (index >= 0 && index < models.Length && models[index] != null)
            models[index].SetActive(true);
    }

    public override void OnDestroy()
    {
        modelIndex.OnValueChanged -= OnModelIndexChanged;
    }
}