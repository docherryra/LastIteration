using Fusion;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform gunPoint;
    [SerializeField] private GameObject riflePrefab;
    [SerializeField] private GameObject shotgunPrefab;
    [SerializeField] private GameObject pistolPrefab;

    // ��Ʈ��ũ ����ȭ�Ǵ� ���� ���� Ÿ��
    [Networked] private int CurrentWeaponType { get; set; }

    private GameObject currentWeaponInstance;
    private PlayerState playerState;
    private int lastKillCount = -1;
    private int lastWeaponType = -1;

    public override void Spawned()
    {
        playerState = GetComponent<PlayerState>();

        if (Object.HasStateAuthority)
        {
            // ������ �ʱ� ���� ����
            CurrentWeaponType = 0; // �������� ����
        }

        // ��� Ŭ���̾�Ʈ�� ���� ����
        UpdateWeaponVisual();
    }

    private void Update()
    {
        // �׽�Ʈ�� Ű �Է�
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("�׽�Ʈ: �������� ����");
                RPC_RequestWeaponChange(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("�׽�Ʈ: �������� ����");
                RPC_RequestWeaponChange(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("�׽�Ʈ: �������� ����");
                RPC_RequestWeaponChange(2);
            }
        }

        // ������ ų ���� ���� ���� ���� ó��
        if (Object.HasStateAuthority)
        {
            if (playerState != null)
            {
                int currentKills = (int)playerState.GetKill();
                if (currentKills != lastKillCount)
                {
                    lastKillCount = currentKills;
                    UpdateWeaponByKills();
                }
            }
        }

        // ��� Ŭ���̾�Ʈ: ���� Ÿ���� ����Ǹ� �ð��� ������Ʈ
        if (CurrentWeaponType != lastWeaponType)
        {
            lastWeaponType = CurrentWeaponType;
            UpdateWeaponVisual();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestWeaponChange(int weaponType)
    {
        // ������ ���� Ÿ�� ���� ����
        CurrentWeaponType = weaponType;
    }

    private void UpdateWeaponByKills()
    {
        if (playerState == null) return;

        int kills = (int)playerState.GetKill();
        int newWeaponType = GetWeaponTypeByKills(kills);

        if (newWeaponType != CurrentWeaponType)
        {
            CurrentWeaponType = newWeaponType;
        }
    }

    private int GetWeaponTypeByKills(int kills)
    {
        if (kills >= 10 && kills <= 15)
            return 2; // ����
        else if (kills >= 5 && kills <= 9)
            return 1; // ����
        else
            return 0; // ����
    }

    private void UpdateWeaponVisual()
    {
        // ���� ���� ����
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }

        // ���� Ÿ�Կ� ���� ������ ����
        GameObject weaponPrefab = null;
        string weaponName = "";

        switch (CurrentWeaponType)
        {
            case 0:
                weaponPrefab = riflePrefab;
                weaponName = "����";
                break;
            case 1:
                weaponPrefab = shotgunPrefab;
                weaponName = "����";
                break;
            case 2:
                weaponPrefab = pistolPrefab;
                weaponName = "����";
                break;
        }

        // �� ���� ���� (���ÿ�����)
        if (weaponPrefab != null && gunPoint != null)
        {
            currentWeaponInstance = Instantiate(weaponPrefab, gunPoint);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            Debug.Log($"[{(Object.HasStateAuthority ? "Server" : "Client")}] Weapon equipped: {weaponName}");
        }
    }

    public GameObject GetCurrentWeapon()
    {
        return currentWeaponInstance;
    }

    public int GetCurrentWeaponType()
    {
        return CurrentWeaponType;
    }

    // 발사 소리를 모든 클라이언트에 브로드캐스트
    public void BroadcastFireSound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        RPC_PlayFireSound(CurrentWeaponType, volume);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayFireSound(int weaponType, float volume)
    {
        if (currentWeaponInstance == null) return;

        AudioSource audio = currentWeaponInstance.GetComponent<AudioSource>();
        if (audio == null) return;

        AudioClip clip = null;
        var gun = currentWeaponInstance.GetComponent<Gun>();
        var shotgun = currentWeaponInstance.GetComponent<Shotgun>();

        if (gun != null)
            clip = gun.fireSound;
        else if (shotgun != null)
            clip = shotgun.fireSound;

        if (clip != null)
            audio.PlayOneShot(clip, volume);
    }
}