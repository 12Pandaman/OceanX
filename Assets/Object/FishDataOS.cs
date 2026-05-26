using UnityEngine;

[CreateAssetMenu(fileName = "NewFishData", menuName = "Fishing Game/Fish Data Asset")]
public class FishDataSO : ScriptableObject
{
    [Header("Visual & Prefab")]
    public GameObject fishVisualPrefab; // โมเดล 3D ของปลาตัวนี้

    [Header("Fish Identity")]
    public string fishHeader;
    public string fishName;
    
    [Header("Fish Info")]
    [TextArea(3, 10)]
    public string fishInfo;

    // ฟังก์ชันความสะดวก: ใช้แปลงข้อมูลจากตัว ScriptableObject ไปเป็นโครงสร้างที่ส่งผ่าน Network ได้
    public FishData GenerateRuntimeData()
    {
        return new FishData
        {
            fishHeader = this.fishHeader,
            fishName = this.fishName,
            fishInfo = this.fishInfo
        };
    }
}