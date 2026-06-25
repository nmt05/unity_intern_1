using UnityEngine;
using System;
using System.Collections;
public class CustomerController : MonoBehaviour
{
    private BasicInput _inputs;
    private CameraControl mainCamera;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] public float jumpForce = 15f; // Tách lực nhảy ra biến riêng cho dễ chỉnh
    [SerializeField] public float fallMultiplier = 2.5f; // Hệ số kéo nhân vật rơi xuống nhanh hơn
    [SerializeField] public float lowJumpMultiplier = 2f; // Hệ số nếu người chơi chỉ nhấp nhẹ nút nhảy
    private Vector2 _inputsMove;
    private bool _isGrounded;

    [Header("Camera and Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 15f;
    private Vector2 _inputsLook;
        private float _xRotation = 0f;

    private Rigidbody _rb;
    private BoxCollider _boxCollider;
    private void Awake()
    {
        _inputs = new BasicInput();
        mainCamera = GameObject.Find("Main Camera").GetComponent<CameraControl>();
        _rb = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();
        // audioSource = GameManager.instance.GetComponent<AudioSource>(); // Lấy AudioSource từ GameObject này
        // dashClip = GameManager.instance.GetDashAudio();
        // jumpClip = GameManager.instance.GetJumpAudio();
        // shootClip = GameManager.instance.GetShootAudio();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => _inputs.Enable();
    private void OnDisable() => _inputs.Disable();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _inputs.Movement.Jump.performed += ctx => Jump();
        _inputs.Movement.Shoot.performed += ctx => Attack();
        // _inputs.Movement.ChangeCam.performed += ctx =>  mainCamera.GetComponent<CameraControl>().change_camera_mode();
        _inputs.Movement.Dash.performed += ctx => Dash();
        _inputs.Action.Esc.performed += ctx => Esc();
    }

    void Update()
    {   
        _inputsMove = _inputs.Movement.Move.ReadValue<Vector2>();
        _inputsLook = _inputs.Movement.Look.ReadValue<Vector2>();

        float mouseX = _inputsLook.x * mouseSensitivity * Time.deltaTime;
        float mouseY = _inputsLook.y * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        // if (mainCamera != null){mainCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);}


        // Vector3 moveDir = transform.forward * _inputsMove.y + transform.right * _inputsMove.x;
        // transform.Translate(moveDir * movementSpeed * Time.deltaTime, Space.World);
        // W/S (_inputsMove.y) di chuyển theo trục X của thế giới (Vector3.right)
        // A/D (_inputsMove.x) di chuyển theo trục Z của thế giới (Vector3.forward)
        Vector3 moveDir = Vector3.right * _inputsMove.x + Vector3.forward * _inputsMove.y;

        // Di chuyển theo hệ tọa độ thế giới (Space.World)
        transform.Translate(moveDir * movementSpeed * Time.deltaTime, Space.World);
        if (moveDir != Vector3.zero) 
        {
            // Tạo góc xoay nhắm về hướng moveDir
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            
            // Xoay mượt mà từ góc hiện tại sang góc mới (thay số 10f bằng tốc độ xoay bạn muốn)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // ================= HỆ THỐNG RAYCAST MULTI-RAY CHO CUBE =================
        int layerMask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("Map"));
        
        // 1. Tính toán kích thước bán kính của Cube dựa trên Collider (đã tính Scale)
        Vector3 center = _boxCollider.bounds.center;
        Vector3 extents = _boxCollider.bounds.extents; // Nửa kích thước của Cube (X, Y, Z)

        // Dịch tâm phát tia xuống gần đáy Cube một chút để tránh tự va chạm với chính nó
        float bottomY = center.y - extents.y + 0.05f; 
        float rayLength = 0.15f; // Tia chỉ cần nhô ra dưới đáy một đoạn ngắn

        // 2. Định nghĩa danh sách các điểm xuất phát (9 điểm dưới đáy)
        Vector3[] rayOrigins = new Vector3[9];
        
        // Điểm ở chính tâm đáy
        rayOrigins[0] = new Vector3(center.x, bottomY, center.z);
        
        // 4 Đỉnh (Corners)
        rayOrigins[1] = new Vector3(center.x + extents.x, bottomY, center.z + extents.z); // Trước - Phải
        rayOrigins[2] = new Vector3(center.x - extents.x, bottomY, center.z + extents.z); // Trước - Trái
        rayOrigins[3] = new Vector3(center.x + extents.x, bottomY, center.z - extents.z); // Sau - Phải
        rayOrigins[4] = new Vector3(center.x - extents.x, bottomY, center.z - extents.z); // Sau - Trái

        // 4 Cạnh (Edges)
        rayOrigins[5] = new Vector3(center.x + extents.x, bottomY, center.z); // Cạnh Phải
        rayOrigins[6] = new Vector3(center.x - extents.x, bottomY, center.z); // Cạnh Trái
        rayOrigins[7] = new Vector3(center.x, bottomY, center.z + extents.z); // Cạnh Trước
        rayOrigins[8] = new Vector3(center.x, bottomY, center.z - extents.z); // Cạnh Sau

        // 3. Vòng lặp bắn các tia Raycast
        bool groundedCheck = false;

        for (int i = 0; i < rayOrigins.Length; i++)
        {
            // Bắn từng tia thẳng xuống đất
            bool hit = Physics.Raycast(rayOrigins[i], Vector3.down, rayLength, layerMask);
            
            // Vẽ tia ra Scene để debug (Chạm = Xanh lá, Không chạm = Đỏ)
            Debug.DrawRay(rayOrigins[i], Vector3.down * rayLength, hit ? Color.green : Color.red);

            // Chỉ cần 1 trong 9 tia chạm đất, coi như nhân vật đang đứng trên đất
            if (hit)
            {
                groundedCheck = true;
            }
        }

        // Gán kết quả cuối cùng cho biến trạng thái
        _isGrounded = groundedCheck;
        mainCamera.Move();
    }
    void FixedUpdate()
    {
         _rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        // // 1. Khi nhân vật ĐANG RƠI XUỐNG (vận tốc Y < 0)
        // if (_rb.linearVelocity.y < 0)
        // {
        //     // Nhân thêm trọng lực với hệ số fallMultiplier để rơi "bùm" xuống đất cực kỳ dứt khoát
        //     _rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        // }
        // // 2. Khi nhân vật ĐANG NHẢY LÊN nhưng người chơi ĐÃ THẢ NÚT NHẢY (Nhảy thấp)
        // else if (_rb.linearVelocity.y > 0 && !_inputs.Movement.Jump.IsPressed())
        // {
        //     // Áp dụng trọng lực lớn hơn để ghìm nhân vật lại, tạo cú nhảy ngắn
        //     _rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        // }
    }
    public void Jump()
    {
        if (_isGrounded)
        {
            // GetComponent<Rigidbody>().AddForce(Vector3.up * 10, ForceMode.Impulse);
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
        }
        
    }
    public void Esc(){

    }
    public void Dash()
    {


    }
    private void Attack()
    {

    }

    }