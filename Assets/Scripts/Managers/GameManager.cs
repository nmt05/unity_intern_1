using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public float bulletSpeed = 10f;
    public float survivalTimer = 15f; // Thời gian sống sót cần thiết để chiến thắng
    public bool IsGameOver { get; private set; } = false; // Biến
    // public AudioSource audioSource;
    // public AudioClip shoot;
    // public AudioClip hit;
    // public AudioClip theme;
    // public AudioClip dash;
    // public AudioClip jump;
    // public AudioClip killed;

    [SerializeField] private Transform UIRoot;
    // [SerializeField] private CameraControl cam;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (instance == null)
        {
            instance = this;
            // theme = GetThemeAudio();
            // audioSource = gameObject.AddComponent<AudioSource>();
            // if (theme != null)            {
            //     audioSource.clip = theme;
            //     audioSource.Play();
            //     audioSource.loop = true; 
            // }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    // void Update()
    // {
    //     // Nếu game đã kết thúc (thắng hoặc thua trước đó rồi) thì không đếm nữa
    //     if (IsGameOver) return; 

    //     // Mỗi giây trôi qua, trừ bớt thời gian vào biến đếm ngược
    //     survivalTimer -= Time.deltaTime;
    //     // cam.Move();
    //     // // KHI THỜI GIAN ĐẾM NGƯỢC VỀ 0 HOẶC NHỎ HƠN 0
    //     // if (survivalTimer <= 0)
    //     // {
    //     //     // 🎯 CHÍNH LÀ Ở ĐÂY: Sự kiện Thắng được bắt và kích hoạt!
    //     //     Victory(); 
    //     // }
    // }

    public void Hello()
    {
        Debug.Log("Hello");
    }
    public void Victory()
    {
        Debug.Log("Victory!");
    }
    public void Defeat()
    {
        Debug.Log("Defeat!");
    }
    // public AudioClip GetShootAudio()
    // {
    //     return shoot;
    // }
    // public AudioClip GetHitAudio()
    // {
    //     return hit;
    // }
    // public AudioClip GetThemeAudio()
    // {
    //     return theme;
    // }
    // public AudioClip GetDashAudio()
    // {
    //     return dash;
    // }
    // public AudioClip GetJumpAudio()
    // {
    //     return jump;
    // }
    // public AudioClip GetKilledAudio()
    // {
    //     return killed;
    // }
}
