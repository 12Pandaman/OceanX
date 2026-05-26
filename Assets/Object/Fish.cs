using Unity.Netcode;
using UnityEngine;

public class NetworkFish : NetworkBehaviour
{
    public NetworkVariable<FishData> fishData = new NetworkVariable<FishData>();

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float wanderRadius = 5f;

    private Vector3 targetPosition;
    private Vector3 spawnOrigin;
    
    public bool isPaused = false; // หยุดปลาเมื่อผู้เล่นกำลังดูข้อมูล

    public override void OnNetworkSpawn()
    {
        fishData.OnValueChanged += OnFishDataChanged;
        spawnOrigin = transform.position;
        GetNewWanderPosition();
        
        UpdateFishVisuals(fishData.Value);
    }

    public override void OnNetworkDespawn()
    {
        fishData.OnValueChanged -= OnFishDataChanged;
    }

    private void Update()
    {
        if (!IsClient) return;
        if (isPaused) return;

        // ระบบว่ายน้ำแบบสุ่ม (Client-Side Simulation)
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            GetNewWanderPosition();
        }
    }

    private void GetNewWanderPosition()
    {
        Vector3 randomCircle = Random.insideUnitSphere * wanderRadius;
        randomCircle.y = Random.Range(-0.5f, 0.5f); 
        targetPosition = spawnOrigin + randomCircle;
    }

    private void OnFishDataChanged(FishData previousValue, FishData newValue)
    {
        UpdateFishVisuals(newValue);
    }

    private void UpdateFishVisuals(FishData data)
    {
        // อัปเดตข้อมูลปลา
        gameObject.name = $"Fish_{data.fishName}";
    }
}