using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject lobbyPanel;

    [Header("Lobby List")]
    public Transform contentPanel;
    public GameObject playerNamePrefab;

    [Header("Buttons")]
    public Button readyButton;
    public Button startGameButton;

    public NetworkVariable<bool> IsGameStarted = new NetworkVariable<bool>(false);
    private Dictionary<ulong, GameObject> playerUIList = new Dictionary<ulong, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            IsGameStarted.Value = true;
        }

        IsGameStarted.OnValueChanged += OnGameStarted;

        // ข้ามหน้า Lobby UI ไปเลย และดึงเมาส์กลับเข้าเกม (เข้าเกมทันที)
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        // ถ้าเกมเริ่มแล้ว (หรือเริ่มทันที) ให้ซ่อนเมาส์
        if (IsGameStarted.Value)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        IsGameStarted.OnValueChanged -= OnGameStarted;
    }

    private void Update()
    {
        // เราจะไม่ใช้หน้า Lobby แล้ว จึงไม่ต้องทำงานอะไรในนี้
    }

    private void UpdateLobbyUI()
    {
        PlayerStateSync[] allPlayers = FindObjectsOfType<PlayerStateSync>();

        // 1. ลบ UI ของคนที่ออกเกม
        List<ulong> toRemove = new List<ulong>();
        foreach (var clientId in playerUIList.Keys)
        {
            bool found = false;
            foreach (var p in allPlayers)
            {
                if (p.OwnerClientId == clientId) { found = true; break; }
            }
            if (!found) toRemove.Add(clientId);
        }
        foreach (var id in toRemove)
        {
            Destroy(playerUIList[id]);
            playerUIList.Remove(id);
        }

        // 2. อัปเดตรายชื่อคนใน Lobby
        foreach (var player in allPlayers)
        {
            if (!playerUIList.ContainsKey(player.OwnerClientId))
            {
                GameObject newUI = Instantiate(playerNamePrefab, contentPanel);
                playerUIList.Add(player.OwnerClientId, newUI);
            }

            GameObject uiEntry = playerUIList[player.OwnerClientId];
            TMP_Text nameText = uiEntry.GetComponentInChildren<TMP_Text>(); 

            if (nameText != null)
            {
                string status = player.IsReady.Value ? "<color=#4CAF50>Ready</color>" : "<color=orange>Waiting</color>";
                string role = player.RoleIndex.Value == 0 ? "Survivor" : "Monster";
                nameText.text = $"[{status}] {player.PlayerName.Value} ({role})";
            }
        }
    }

    public void OnReadyButtonClicked()
    {
        // 1. เช็คว่าตัวละครของเราโหลดเสร็จมีตัวตนอยู่บนโลกแล้วหรือยัง
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            // 2. ดึงสคริปต์ของ "ตัวเราเอง" เท่านั้นมาใช้งาน
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerStateSync myPlayer))
            {
                myPlayer.ToggleReadyServerRpc();
            }
        }
    }

    public void OnStartGameButtonClicked()
    {
        if (IsServer)
        {
            IsGameStarted.Value = true; 
        }
    }

    private void CheckIfAllReady()
    {
        if (!IsServer || startGameButton == null) return;

        PlayerStateSync[] allPlayers = FindObjectsOfType<PlayerStateSync>();
        bool isEveryoneReady = true;

        foreach (var p in allPlayers)
        {
            if (!p.IsReady.Value)
            {
                isEveryoneReady = false;
                break;
            }
        }

        // เปิดให้กด Start ได้เมื่อทุกคน Ready
        startGameButton.interactable = (isEveryoneReady && allPlayers.Length > 0);
    }

    private void OnGameStarted(bool oldValue, bool newValue)
    {
        if (newValue == true)
        {
            // ปิดหน้า Lobby UI เมื่อเกมเริ่ม
            if (lobbyPanel != null) lobbyPanel.SetActive(false);

            // ซ่อนเมาส์เพื่อกลับสู่โหมดบังคับตัวละคร
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
}