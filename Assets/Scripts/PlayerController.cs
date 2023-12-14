using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public Camera MyCamera { get; private set; }
    public GameObject Weapon = null;
    public Transform Muzzle = null;
    public ParticleSystem MuzzleFlash = null;
    public GameObject Bullet = null;
    private Transform _aimPoint = null;

    private CharacterController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    public GroundChecker MyGroundChecker { get; private set; }
    private TPSCamController _myTPSCam;
    [SerializeField] private LayerMask _IgnoreRaycast;
    [SerializeField] private float _jumpCoolDown = 1.0f;

    private Vector3 _snapGroundForce = Vector3.zero;
    public Vector3 PlayerVelocity { get; private set; } = Vector3.zero;
    public Vector3 PlayerVelocityOnAim { get; private set; } = Vector3.zero;

    #region Input Varibales
    public float InputAxisSensitivity = 0.5f;
    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
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
    public bool IsSpaceKeyAction { get; private set; } = false;
    private bool _isJumping = false;
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
    public float WeaponRPM = 850;
    private float _fireRate;
    private bool _isShooting = false;
    private WaitForSeconds _fireRateWFS = null;

    private Vector3 _aimPosition;
    private Vector3 _aimNormal;
    #endregion

    private void Awake()
    {
        MyCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _aimPoint = GameObject.FindWithTag("AimPoint").transform;
        _fireRate = 1 / (WeaponRPM / 60);
        _fireRateWFS = YieldCache.WaitForSeconds(_fireRate);
    }

    void Start()
    {
        MuzzleFlash.Stop();
        _player = this.GetComponent<CharacterController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        MyGroundChecker = this.GetComponent<GroundChecker>();
        _myTPSCam = this.GetComponent<TPSCamController>();
    }

    void Update()
    {
        if (_isJumping)
        {
            _snapGroundForce = Vector3.zero;
        }
        _playerMoveOrientedForward = new Vector3(_myTPSCam.CamTarget.forward.x, 0, _myTPSCam.CamTarget.forward.z).normalized;
        _playerMoveOrientedRight = new Vector3(_myTPSCam.CamTarget.right.x, 0, _myTPSCam.CamTarget.right.z).normalized;

        numberOfKeysPressed = keysToCheck.Count(key => Input.GetKey(key));
        if (Input.GetMouseButton(1) && !IsSpaceKeyAction && !IsOnDynamicMove)
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
                    if (MyGroundChecker.IsGround)
                    {
                        JumpMode = _playerParkour.CheckRay(); //Predicts Next JumpMode
                    }

                    if (_isRotating)
                    {
                        PlayerRotate();
                    }
                    PlayerMove();

                    _aimPoint.transform.position = this.transform.position + Vector3.up +  this.transform.forward;
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
        HorizontalInput = Input.GetAxis("Horizontal");
        VerticalInput = Input.GetAxis("Vertical");

        if (numberOfKeysPressed < 3)
        {
            _rotationHorizontalInput = Input.GetAxisRaw("Horizontal");
            _rotationVerticalInput = Input.GetAxisRaw("Vertical");
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _snapGroundForce = Vector3.zero;
            IsSpaceKeyAction = true;
            _isJumping = true;
        }
    }

    private void CalculatePlayerTransformByInput()
    {
        _snapGroundForce = Vector3.zero;
        if (_rotationHorizontalInput != 0 || _rotationVerticalInput != 0)
        {
            _playerRotation = Quaternion.Euler(0, CalculateRotationAngle(_rotationHorizontalInput, _rotationVerticalInput), 0);
            _isRotating = true;
        }

        if (!_isJumping)
        {
            if (MyGroundChecker.IsSnapGround && MyGroundChecker.IsGround)
            {
                _snapGroundForce = Vector3.down;
            }
        }
        _player.Move(PlayerVelocity * Time.deltaTime + _snapGroundForce);
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
        Vector3 moveVel = _playerMoveOrientedForward * VerticalInput + _playerMoveOrientedRight * HorizontalInput;
        Vector3 moveDir = moveVel.normalized; //Direction

        float moveSpeed = Mathf.Min(moveVel.magnitude, 1.0f) * _playerStat.MyCurrentSpeed;

        return moveDir * moveSpeed;
    }

    private float PlayerYAxisVelocity() //Calculates Y-Axis Velocity By Player's Input
    {
        if (!MyGroundChecker.IsGround) // If isn't on ground, then apply Gravity force
        {
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        if (IsSpaceKeyAction) //while get input 'Jump'
        {
            switch (JumpMode)
            {
                case PlayerParkour.JumpState.DefaultJump: //Do Default Jump
                    IsSpaceKeyAction = false; 
                    StartCoroutine(DefaultJump());
                    return PlayerVelocity.y + _playerStat.JumpPower;
                case PlayerParkour.JumpState.Vault: //Do Vault Action
                    if (!IsOnDynamicMove && _myCoroutine == null)
                    {
                        IsOnDynamicMove = true;
                        _myCoroutine = StartCoroutine(DoVault());
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y);
                case PlayerParkour.JumpState.JumpClimb: //Do JumpClimb Action
                    if (!IsOnDynamicMove && _myCoroutine == null)
                    {
                        IsOnDynamicMove = true;
                        _myCoroutine = StartCoroutine(DoHopClimb());
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y); //Do Climb Action
                case PlayerParkour.JumpState.Climb:
                    IsSpaceKeyAction = false;
                    return PlayerVelocity.y + _playerStat.JumpPower;
                default:
                    IsSpaceKeyAction = false;
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
    private IEnumerator DefaultJump()
    {
        yield return YieldCache.WaitForSeconds(_jumpCoolDown);
        _isJumping = false;
    }
    private IEnumerator DoVault()
    {
        if (JumpMode != PlayerParkour.JumpState.Vault || !MyGroundChecker.IsGround)
        {
            IsSpaceKeyAction = false;
            yield break;
        }

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        Vector3 startPoint = this.transform.position;
        Vector3 vaultPoint = _playerParkour.StepPoint;
        IsSpaceKeyAction = false;

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
        if (JumpMode != PlayerParkour.JumpState.JumpClimb || !MyGroundChecker.IsGround)
        {
            IsSpaceKeyAction = false;
            yield break;
        }

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        Vector3 startPoint = this.transform.position;
        Vector3 endPoint = _playerParkour.StepPoint;
        Vector3 endPointXZ = new Vector3(endPoint.x, 0, endPoint.z);
        Vector3 startPointXZ = new Vector3(startPoint.x, 0, startPoint.z);
        Vector3 climbPoint = _playerParkour.StepPoint - (endPointXZ - startPointXZ).normalized * 0.3f;
        IsSpaceKeyAction = false;

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
                lerpTime = _playerParkour.ParkourJumpClimbTime - 2 * _playerParkour.ParkourClimbTime;
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
        HorizontalInput = Input.GetAxis("Horizontal");
        VerticalInput = Input.GetAxis("Vertical");
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
                aimPos = aimPointRay.origin + aimPointRay.direction * 15f;
                _aimPosition = Vector3.zero;
                _aimNormal = Vector3.zero;
            }
            else
            {
                aimPos = hitInfo.point;
                _aimPosition = aimPos;
                _aimNormal = hitInfo.normal;
            }
            _aimPoint.position = Vector3.SmoothDamp(_aimPoint.position, aimPos, ref _refVelocity, AimSpeed);
        }
        else
        {
            Vector3 aimPos = aimPointRay.origin + aimPointRay.direction * 15f;
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
        Vector3 moveVel = _playerMoveOrientedForward * VerticalInput + _playerMoveOrientedRight * HorizontalInput;
        Vector3 moveDir = moveVel.normalized;

        float moveSpeed = Mathf.Min(moveVel.magnitude, 1.0f) * _playerStat.MyCurrentSpeedOnAim;

        return moveDir * moveSpeed;
    }

    private float PlayerYAxisVelocityOnAim()
    {
        if (!MyGroundChecker.IsGround) // If isn't on ground, then apply Gravity force
        {
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        return Mathf.Max(0.0f, PlayerVelocity.y); //Default Y-Axis Velocity
    }

    private void CalculatePlayerTransformByInputOnAim()
    {
        _snapGroundForce = Vector3.zero;
        PlayerVelocityOnAim = transform.InverseTransformDirection(_player.velocity);

        if (!_isJumping)
        {
            if (MyGroundChecker.IsSnapGround && MyGroundChecker.IsGround)
            {
                _snapGroundForce = Vector3.down;
            }
        }
        _player.Move(PlayerVelocity * Time.deltaTime + _snapGroundForce);
    }
    #endregion

    #region Player Shooting Guns On Aim-Mode Fields
    private void ShootingInput()
    {
        if (Input.GetMouseButton(0))
        {
            if(_isShooting == false) StartCoroutine(Shoot());
        }
    }

    private IEnumerator Shoot()
    {
        _isShooting = true;
        MuzzleFlash.Play();
        GameObject bullet = Instantiate(Bullet, Muzzle.position, Muzzle.rotation);
        bullet.GetComponent<ProjectileControl>().Initailize(_playerStat.WeaponDamage, _aimPosition, _aimNormal);
        yield return _fireRateWFS;
        _isShooting = false;
    }
    #endregion
}
