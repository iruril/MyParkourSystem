using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public Camera MyCamera { get; private set; }
    public GameObject Weapon = null;
    public Transform Muzzle = null;
    public GameObject MuzzleFlash = null;
    private Transform _aimPoint = null;

    private CharacterController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    public GroundChecker MyGroundChecker;
    private TPSCamController _myTPSCam;
    [SerializeField] private LayerMask _IgnoreRaycast;
    [SerializeField] private GameObject _bloodEffect = null;

    public Vector3 PlayerVelocity { get; private set; } = Vector3.zero;
    public Vector3 PlayerVelocityOnAim { get; private set; } = Vector3.zero;

    #region Input Varibales
    private float _horizontalInput;
    private float _verticalInput;
    private float _rotationHorizontalInput;
    private float _rotationVerticalInput;
    #endregion

    private Vector3 _playerMoveOrientedForward;
    private Vector3 _playerMoveOrientedRight;
    private Quaternion _playerRotation;
    private bool _isRotating;

    #region Movement Trigger Restriction Variables
    private HashSet<KeyCode> keysToCheck = new HashSet<KeyCode>{ KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    int numberOfKeysPressed;
    public bool IsOnDynamicMove { get; private set; } = false;
    public bool IsJumping { get; private set; } = false;
    public PlayerParkour.JumpState JumpMode { get; private set; } = PlayerParkour.JumpState.None;

    public enum MoveMode{
        Default,
        Aim
    }
    public MoveMode CurrentMode { get; private set; } = MoveMode.Default;

    private Coroutine _myCoroutine;
    #endregion

    #region Shooting Variables
    public float AimSpeed = 0.2f;
    private Vector3 _refVelocity = Vector3.zero;
    public float FireRate = 0.1f;
    private float _fireTime = 0f;
    private LineRenderer _projectileLine;
    private WaitForSeconds shotDuration = new WaitForSeconds(0.02f);
    #endregion

    private void Awake()
    {
        MyCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _aimPoint = GameObject.FindWithTag("AimPoint").transform;
    }

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        MyGroundChecker = this.GetComponent<GroundChecker>();
        _myTPSCam = this.GetComponent<TPSCamController>();

        _projectileLine = this.GetComponent<LineRenderer>();
        _projectileLine.enabled = false;
        MuzzleFlash = Muzzle.GetChild(0).gameObject;
        MuzzleFlash.SetActive(false);
    }

    void Update()
    {
        _playerMoveOrientedForward = _myTPSCam.CamTarget.forward;
        _playerMoveOrientedRight = _myTPSCam.CamTarget.right;

        numberOfKeysPressed = keysToCheck.Count(key => Input.GetKey(key));
        if (Input.GetMouseButton(1) && !IsJumping && !IsOnDynamicMove)
        {
            if (CurrentMode != MoveMode.Aim)
            {
                CurrentMode = MoveMode.Aim;
                this.transform.rotation = _myTPSCam.CamTarget.rotation;
                _myTPSCam.ActivateAimModeCam();
            }
        }
        else
        {
            if (CurrentMode != MoveMode.Default)
            {
                CurrentMode = MoveMode.Default;
                _myTPSCam.DeactivateAimModeCam();
            }
        }

        switch (CurrentMode)
        {
            case MoveMode.Default:
                #region Default Player Update
                if (MuzzleFlash.activeSelf) MuzzleFlash.SetActive(false);
                if (Weapon.activeSelf) Weapon.SetActive(false);
                GetInput();
                CalculatePlayerTransformByInput(); //Calculated PlayerVelocity is based on 'FixedTime', so we smooth this with 'Time'.
                #endregion
                break;
            case MoveMode.Aim:
                #region Aim-Mode Player Update
                if (!Weapon.activeSelf) Weapon.SetActive(true);
                AimModeInput(); 
                ShootingInput();
                CalculatePlayerTransformByInputOnAim();
                #endregion
                break;
        }
    }

    private void FixedUpdate() //For Velocity Calculations
    {
        switch (CurrentMode)
        {
            case MoveMode.Default:
                #region Default Player FixedUpdate
                if (!IsOnDynamicMove) //While Isn't on Parkour Action
                {
                    if (MyGroundChecker.IsGrounded())
                    {
                        JumpMode = _playerParkour.CheckRay(); //Predicts Next JumpMode
                    }

                    if (_isRotating)
                    {
                        PlayerRotate();
                    }
                    PlayerMove();
                }
                #endregion
                break;
            case MoveMode.Aim:
                #region Aim-Mode Player FixedUpdate
                PlayerAimModeMove();
                #endregion
                break;
        }     
    }
    #region Default Player Control Fields
    #region Player Input Fields
    private void GetInput()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical"); 

        if (numberOfKeysPressed < 3)
        {
            _rotationHorizontalInput = Input.GetAxisRaw("Horizontal");
            _rotationVerticalInput = Input.GetAxisRaw("Vertical");
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsJumping = true;
        }
    }

    private void CalculatePlayerTransformByInput()
    {
        if (_rotationHorizontalInput != 0 || _rotationVerticalInput != 0)
        {
            _playerRotation = Quaternion.Euler(0, CalculateRotationAngle(_rotationHorizontalInput, _rotationVerticalInput), 0);
            _isRotating = true;
        }
        _player.Move(PlayerVelocity * Time.deltaTime);
    }
    #endregion

    #region Player Default Move And Rotation Fields
    private void PlayerMove() //Combine XZ-Plane Velocity and Y-Axis Velocity. 
    {
        Vector3 xzPlaneVel = PlayerXZPlaneVelocity(); //Calculates XZ-Plane Velocity
        float yAxisVel = PlayerYAxisVelocity(); //Calculates Y-Axis Velocity
        PlayerVelocity = new Vector3(xzPlaneVel.x, yAxisVel, xzPlaneVel.z); //Combine
    }

    private Vector3 PlayerXZPlaneVelocity() //Calculates XZ-Plane Velocity By Player's Input
    {
        Vector3 moveVel = _playerMoveOrientedForward * _verticalInput + _playerMoveOrientedRight * _horizontalInput;
        Vector3 moveDir = moveVel.normalized; //Direction

        float moveSpeed = Mathf.Min(moveVel.magnitude, 1.0f) * _playerStat.MyCurrentSpeed;

        return moveDir * moveSpeed;
    }

    private float PlayerYAxisVelocity() //Calculates Y-Axis Velocity By Player's Input
    {
        if (!MyGroundChecker.IsGrounded()) // If isn't on ground, then apply Gravity force
        {
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        if (IsJumping) //while get input 'Jump'
        {
            switch (JumpMode)
            {
                case PlayerParkour.JumpState.DefaultJump: //Do Default Jump
                    IsJumping = false;
                    return PlayerVelocity.y + _playerStat.JumpPower;
                case PlayerParkour.JumpState.Vault: //Do Vault Action
                    if (!IsOnDynamicMove && _myCoroutine == null)
                    {
                        IsOnDynamicMove = true;
                        _myCoroutine = StartCoroutine(DoVault());
                    }
                    else
                    {
                        JumpMode = PlayerParkour.JumpState.None;
                        IsJumping = false;
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y);
                case PlayerParkour.JumpState.JumpClimb: //Do JumpClimb Action
                    if (!IsOnDynamicMove && _myCoroutine == null)
                    {
                        IsOnDynamicMove = true;
                        _myCoroutine = StartCoroutine(DoHopClimb());
                    }
                    else
                    {
                        JumpMode = PlayerParkour.JumpState.None;
                        IsJumping = false;
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y); //Do Climb Action
                case PlayerParkour.JumpState.Climb:
                    IsJumping = false;
                    return PlayerVelocity.y + _playerStat.JumpPower;
                default:
                    IsJumping = false;
                    return Mathf.Max(0.0f, PlayerVelocity.y); //If There's no condition
            }
        }
        else
        {
            return Mathf.Max(0.0f, PlayerVelocity.y); //Default Y-Axis Velocity
        }
    }

    private float CalculateRotationAngle(float h, float v) //Calculates Radian-Angle By Vector2(h,v)
    {
        return _myTPSCam.CamTarget.rotation.eulerAngles.y + Mathf.Atan2(h, v) * Mathf.Rad2Deg;
    }

    private void PlayerRotate() //Do Character Rotation By Player's Input
    {
        if (PlayerVelocity.magnitude < 0.1f || (_rotationVerticalInput == 0 && _rotationHorizontalInput == 0))
        {
            _isRotating = false;
            return;
        }

        float step = Time.fixedDeltaTime * _playerStat.RotateSpeed; //Smooth Step for Prevent Ragging
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, _playerRotation, step);
    }
    #endregion

    #region Parkour Actions Fields
    private IEnumerator DoVault()
    {
        if (JumpMode != PlayerParkour.JumpState.Vault || !MyGroundChecker.IsGrounded())
        {
            IsJumping = false;
            yield break;
        }

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        Vector3 startPoint = this.transform.position;
        Vector3 vaultPoint = _playerParkour.StepPoint;
        IsJumping = false;

        //First Step Action (Move to Step Point)
        float lerpTime = _playerParkour.ParkourJumpTime;
        float currentTime = 0;
        while (currentTime < _playerParkour.ParkourJumpTime)
        {
            currentTime += Time.deltaTime;
            this.transform.position = Vector3.Slerp(startPoint, vaultPoint, currentTime / lerpTime);
            yield return null;
        }

        //Second Step Action
        Vector3 vaultDir = this.transform.forward;
        lerpTime = _playerParkour.ParkourVaultTime - _playerParkour.ParkourJumpTime;
        currentTime = 0;
        float speed = _playerParkour.ParkourMoveSpeed;
        while (currentTime < lerpTime)
        {
            currentTime += Time.deltaTime;
            PlayerVelocity = new Vector3(vaultDir.x * speed,
                                         PlayerVelocity.y - _playerStat.GravityForce * Time.deltaTime,
                                         vaultDir.z * speed);
            speed = speed - Time.deltaTime;
            yield return null;
        }

        IsOnDynamicMove = false;
        _myCoroutine = null;
        JumpMode = PlayerParkour.JumpState.None;
        yield break;
    }

    private IEnumerator DoHopClimb()
    {
        if (JumpMode != PlayerParkour.JumpState.JumpClimb || !MyGroundChecker.IsGrounded())
        {
            IsJumping = false;
            yield break;
        }

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        Vector3 startPoint = this.transform.position;
        Vector3 endPoint = _playerParkour.StepPoint;
        Vector3 endPointXZ = new Vector3(endPoint.x, 0, endPoint.z);
        Vector3 startPointXZ = new Vector3(startPoint.x, 0, startPoint.z);
        Vector3 climbPoint = _playerParkour.StepPoint - (endPointXZ - startPointXZ).normalized * 0.3f;
        IsJumping = false;

        float lerpTime;
        float currentTime;

        switch (_playerParkour.StepMode)
        {
            case PlayerParkour.StepState.Lowest:
                lerpTime = _playerParkour.ParkourClimbTime;
                currentTime = 0;
                while (currentTime < lerpTime)
                {
                    currentTime += Time.deltaTime;
                    this.transform.position = Vector3.Slerp(startPoint, endPoint, currentTime / lerpTime);

                    yield return null;
                }
                break;
            case PlayerParkour.StepState.Middle:
                //First Step Action (Move to Step Point)
                lerpTime = _playerParkour.ParkourClimbTime;
                currentTime = 0;
                while (currentTime < lerpTime)
                {
                    currentTime += Time.deltaTime;
                    this.transform.position = Vector3.Slerp(startPoint, climbPoint, currentTime / lerpTime);

                    yield return null;
                }

                //Second Step Action
                lerpTime = (_playerParkour.ParkourJumpClimbTime - 0.2f) - lerpTime;
                currentTime = 0;
                while (currentTime < lerpTime)
                {
                    currentTime += Time.deltaTime;
                    this.transform.position = Vector3.Slerp(climbPoint, endPoint, currentTime / lerpTime);

                    yield return null;
                }
                break;
            case PlayerParkour.StepState.Highest:
                Vector3 secondClimbPoint = climbPoint;
                climbPoint.y = climbPoint.y - 1.0f;

                //First Step Action
                lerpTime = _playerParkour.ParkourClimbTime;
                currentTime = 0;
                while (currentTime < lerpTime)
                {
                    currentTime += Time.deltaTime;
                    this.transform.position = Vector3.Slerp(startPoint, climbPoint, currentTime / lerpTime);

                    yield return null;
                }

                //Second Step Action
                lerpTime = _playerParkour.ParkourClimbTime;
                currentTime = 0;
                while (currentTime < lerpTime)
                {
                    currentTime += Time.deltaTime;
                    this.transform.position = Vector3.Lerp(climbPoint, secondClimbPoint, currentTime / lerpTime);

                    yield return null;
                }

                //Third Step Action
                lerpTime = _playerParkour.ParkourJumpClimbTime - 2 * _playerParkour.ParkourClimbTime;
                currentTime = 0;
                while (currentTime < lerpTime)
                {
                    currentTime += Time.deltaTime;
                    this.transform.position = Vector3.Lerp(secondClimbPoint, endPoint, currentTime / lerpTime);

                    yield return null;
                }
                break;
        }
        IsOnDynamicMove = false;
        _myCoroutine = null;
        JumpMode = PlayerParkour.JumpState.None;
        yield break;
    }
    #endregion
    #endregion

    #region Aim-Mode Player Control Fields
    private void AimModeInput()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
    }
    private void RotatePlayerOnAimMode()
    {
        this.transform.rotation = Quaternion.Euler(0, _myTPSCam.CamTarget.rotation.eulerAngles.y, 0);
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        Ray aimPointRay = MyCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(aimPointRay, out hitInfo, 300f, ~_IgnoreRaycast))
        {
            Vector3 aimPos;
            if (Vector3.Distance(hitInfo.point, this.transform.position) < 1.5f)
            {
                aimPos = aimPointRay.origin + aimPointRay.direction * 5f;
            }
            else
            {
                aimPos = hitInfo.point;
            }
            _aimPoint.position = Vector3.SmoothDamp(_aimPoint.position, aimPos, ref _refVelocity, AimSpeed);
        }
        else
        {
            Vector3 aimPos = aimPointRay.origin + aimPointRay.direction * 5f;
            _aimPoint.position = Vector3.SmoothDamp(_aimPoint.position, aimPos, ref _refVelocity, AimSpeed);
        }
    }

    private void PlayerAimModeMove()
    {
        RotatePlayerOnAimMode();

        Vector3 xzPlaneVel = PlayerXZPlaneVelocityOnAim(); //Calculates XZ-Plane Velocity
        float yAxisVel = PlayerYAxisVelocityOnAim(); //Calculates Y-Axis Velocity
        PlayerVelocity = new Vector3(xzPlaneVel.x, yAxisVel, xzPlaneVel.z); //Combine
    }
    private Vector3 PlayerXZPlaneVelocityOnAim()
    {
        Vector3 moveVel = _playerMoveOrientedForward * _verticalInput + _playerMoveOrientedRight * _horizontalInput;
        Vector3 moveDir = moveVel.normalized;

        float moveSpeed = Mathf.Min(moveVel.magnitude, 1.0f) * (_playerStat.MyCurrentSpeed / 2f);

        return moveDir * moveSpeed;
    }

    private float PlayerYAxisVelocityOnAim()
    {
        if (!MyGroundChecker.IsGrounded()) // If isn't on ground, then apply Gravity force
        {
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        return Mathf.Max(0.0f, PlayerVelocity.y); //Default Y-Axis Velocity
    }

    private void CalculatePlayerTransformByInputOnAim()
    {
        PlayerVelocityOnAim = transform.InverseTransformDirection(_player.velocity);
        _player.Move(PlayerVelocity * Time.deltaTime);
    }
    #endregion

    #region Player Shooting Guns On Aim-Mode Fields
    private void ShootingInput()
    {
        if (Input.GetMouseButton(0))
        {
            Shoot();
            if (_fireTime < FireRate)
            {
                _fireTime += Time.deltaTime;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (!MuzzleFlash.activeSelf) MuzzleFlash.SetActive(true);
        }
        else
        {
            if (MuzzleFlash.activeSelf) MuzzleFlash.SetActive(false);
        }
    }

    private void Shoot()
    {
        if (_fireTime < FireRate) return;
        RaycastHit hitInfo;
        _projectileLine.SetPosition(0, Muzzle.position);
        if (Physics.Raycast(Muzzle.position, Muzzle.forward, out hitInfo, 100f))
        {
            _projectileLine.SetPosition(1, hitInfo.point);
            if (hitInfo.transform.CompareTag("Enemy"))
            {
                Vector3 direction = hitInfo.normal;
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180;
                StartCoroutine(BloodEffect(hitInfo.point, angle));
                if (hitInfo.transform.GetComponent<IDamageable>() != null)
                {
                    hitInfo.transform.GetComponent<IDamageable>().TakeHit(_playerStat.WeaponDamage, Muzzle.forward);
                }
                if (hitInfo.transform.GetComponent<Rigidbody>() != null)
                {
                    hitInfo.transform.GetComponent<Rigidbody>().AddForce(Muzzle.forward * 20f, ForceMode.Impulse);
                }
            }
        }
        else
        {
            _projectileLine.SetPosition(1, Muzzle.position + Muzzle.forward * 100f);
        }
        StartCoroutine(ShootEffect());
        _fireTime = 0.0f;
    }
    private IEnumerator ShootEffect()
    {
        _projectileLine.enabled = true;
        yield return shotDuration;
        _projectileLine.enabled = false;
    }
    private IEnumerator BloodEffect(Vector3 position, float angle)
    {
        GameObject effect = Instantiate(_bloodEffect, position, Quaternion.Euler(new Vector3(0, angle + 90, 0)));
        effect.GetComponent<BFX_BloodSettings>().GroundHeight = this.transform.position.y;
        yield return new WaitForSeconds(10.0f);
        Destroy(effect);
    }
    #endregion
}
