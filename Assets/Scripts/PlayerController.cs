using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    #region Mobile Controller Variables
    private FloatingJoystick _myJoystick;
    #endregion

    public Camera _myCamera { get; set; } = null;
    public GameObject Weapon = null;
    public Transform Muzzle = null;
    public GameObject MuzzleFlash = null;

    private CharacterController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    private GroundChecker _myGroundChecker;
    [SerializeField] private LayerMask _IgnoreRaycast;
    [SerializeField] private GameObject _bloodEffect = null;
    private Outline _enemyOutline = null;

    public Vector3 PlayerVelocity { get; private set; } = Vector3.zero;
    public Vector3 PlayerVelocityBasedOnLookDir { get; private set; } = Vector3.zero;
    public bool MyIsGrounded { get; private set; } = false;
    public Vector3 LookTarget { get; private set; } = Vector3.zero;
    public bool IsAimOnEnemy { get; private set; } = false;

    #region Input Varibales
    private float _horizontalInput;
    private float _verticalInput;
    private float _rotationHorizontalInput;
    private float _rotationVerticalInput;
    private Vector3 _mouseLookPosition;
    #endregion

    private float _playerYAngleOffset = 0;
    private Vector3 _playerMoveOrientedForward;
    private Vector3 _playerMoveOrientedRight;
    private Quaternion _playerRotation;
    private bool _isRotating;
    private Vector3 _lookDirection;

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
    #endregion

    #region Shooting Variables
    public float FireRate = 0.1f;
    private float _fireTime = 0f;
    private LineRenderer _projectileLine;
    private WaitForSeconds shotDuration = new WaitForSeconds(0.02f);
    #endregion

    private void Awake()
    {
#if (UNITY_ANDROID || UNITY_IOS)
        _myJoystick = GameObject.Find("Joystick").GetComponent<FloatingJoystick>();   
#endif
    }

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _playerYAngleOffset = _myCamera.GetComponent<CameraController>().GetCameraOffsetAngles().y;
        _playerMoveOrientedForward = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.forward;
        _playerMoveOrientedRight = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.right;

        _projectileLine = this.GetComponent<LineRenderer>();
        _projectileLine.enabled = false;
        MuzzleFlash = Muzzle.GetChild(0).gameObject;
        MuzzleFlash.SetActive(false);
    }

    void Update()
    {
        numberOfKeysPressed = keysToCheck.Count(key => Input.GetKey(key));
        if (Input.GetMouseButton(1) && !IsJumping && !IsOnDynamicMove)
        {
            if(CurrentMode != MoveMode.Aim) CurrentMode = MoveMode.Aim;
        }
        else
        {
            if (CurrentMode != MoveMode.Default) CurrentMode = MoveMode.Default;
        }

        switch (CurrentMode)
        {
            case MoveMode.Default:
                #region Default Player Update
                if (MuzzleFlash.activeSelf) MuzzleFlash.SetActive(false);
                if (Weapon.activeSelf) Weapon.SetActive(false);
                if (_enemyOutline != null)
                {
                    _enemyOutline.enabled = false;
                    _enemyOutline = null;
                }
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
        MyIsGrounded = _myGroundChecker.IsGrounded();
        switch (CurrentMode)
        {
            case MoveMode.Default:
                #region Default Player FixedUpdate
                if (!IsOnDynamicMove) //While Isn't on Parkour Action
                {
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
        if (MyIsGrounded && !IsOnDynamicMove)
        {
            JumpMode = _playerParkour.CheckRay(); //Predicts Next JumpMode
        }

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
        if (!MyIsGrounded && !IsOnDynamicMove) // If isn't on ground, then apply Gravity force
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
                    if (!IsOnDynamicMove)
                    {
                        IsOnDynamicMove = true;
                        StartCoroutine(DoVault());
                    }
                    else
                    {
                        JumpMode = PlayerParkour.JumpState.None;
                        IsJumping = false;
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y);
                case PlayerParkour.JumpState.JumpClimb: //Do JumpClimb Action
                    if (!IsOnDynamicMove)
                    {
                        IsOnDynamicMove = true;
                        StartCoroutine(DoHopClimb());
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
        return _playerYAngleOffset + Mathf.Atan2(h, v) * Mathf.Rad2Deg;
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
        if (JumpMode != PlayerParkour.JumpState.Vault && !_myGroundChecker.IsGrounded())
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
            speed = speed - Time.deltaTime * 2;
            yield return null;
        }
        IsOnDynamicMove = false;
        JumpMode = PlayerParkour.JumpState.None;
        yield break;
    }

    private IEnumerator DoHopClimb()
    {
        if (JumpMode != PlayerParkour.JumpState.JumpClimb && !_myGroundChecker.IsGrounded())
        {
            IsJumping = false;
            yield break;
        }

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        Vector3 startPoint = this.transform.position;
        Vector3 endPoint = _playerParkour.StepPoint;
        Vector3 t1 = new Vector3(endPoint.x, 0, endPoint.z);
        Vector3 t2 = new Vector3(startPoint.x, 0, startPoint.z);
        Vector3 climbPoint = _playerParkour.StepPoint - (t1 - t2).normalized * 0.3f;
        IsJumping = false;

        if (_playerParkour.StepHeight <= 1.5f) //if StepHeoght is lower then 1.5m
        {
            //First Step Action (Move to Step Point)
            float lerpTime = _playerParkour.ParkourClimbTime;
            float currentTime = 0;
            while (currentTime < lerpTime)
            {
                currentTime += Time.deltaTime;
                this.transform.position = Vector3.Slerp(startPoint, endPoint, currentTime / lerpTime);

                yield return null;
            }

        }
        else if(_playerParkour.StepHeight is > 1.5f and <= 2.0f) //if StepPoint is higher then 1.5m, and lower then 2m
        {
            //First Step Action (Move to Step Point)
            float lerpTime = _playerParkour.ParkourClimbTime;
            float currentTime = 0;
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
        }
        else //if StepPoint is Higher than 2m
        {
            Vector3 secondClimbPoint = climbPoint;
            climbPoint.y = climbPoint.y - 1.0f;

            //First Step Action
            float lerpTime = _playerParkour.ParkourClimbTime;
            float currentTime = 0;
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
                this.transform.position = Vector3.Slerp(secondClimbPoint, endPoint, currentTime / lerpTime);

                yield return null;
            }
        }
        IsOnDynamicMove = false;
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
    private void RotateToAimPoint()
    {
        Ray aimPointRay = _myCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(aimPointRay, out hitInfo, 100f, ~_IgnoreRaycast))
        {
            if (hitInfo.transform.tag == "Enemy")
            {
                if(_enemyOutline != null)
                {
                    if (_enemyOutline != null)
                    {
                        _enemyOutline.enabled = false;
                        _enemyOutline = null;
                    }
                }
                _enemyOutline = hitInfo.transform.GetComponent<Outline>();
                if (_enemyOutline != null)
                {
                    _enemyOutline.enabled = true;
                }
                IsAimOnEnemy = true;
            }
            else
            {
                if (_enemyOutline != null)
                {
                    _enemyOutline.enabled = false;
                    _enemyOutline = null;
                }
                IsAimOnEnemy = false;
            }

            LookTarget = hitInfo.point;
            _mouseLookPosition = LookTarget;
            _mouseLookPosition.y = this.transform.position.y;
        }
        _lookDirection = _mouseLookPosition - this.transform.position;
        _lookDirection = _lookDirection.normalized;
        if (_lookDirection != Vector3.zero)
        {
            float step = Time.fixedDeltaTime * _playerStat.RotateSpeed;
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(_lookDirection), step);
        }
    }

    private void PlayerAimModeMove()
    {
        RotateToAimPoint();

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
        if (!MyIsGrounded && !IsOnDynamicMove) // If isn't on ground, then apply Gravity force
        {
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        return Mathf.Max(0.0f, PlayerVelocity.y); //Default Y-Axis Velocity
    }

    private void CalculatePlayerTransformByInputOnAim()
    {
        PlayerVelocityBasedOnLookDir = transform.InverseTransformDirection(_player.velocity);
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
        if (IsAimOnEnemy)
        {
            Vector3 dir = (LookTarget - Muzzle.position).normalized;
            if (Physics.Raycast(Muzzle.position, dir, out hitInfo, 100f))
            {
                _projectileLine.SetPosition(1, hitInfo.point);
                if(hitInfo.transform.tag == "Enemy")
                {
                    Vector3 direction = hitInfo.normal;
                    float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180;
                    StartCoroutine(BloodEffect(hitInfo.point, angle));
                    if (hitInfo.transform.GetComponent<IDamageable>() != null)
                    {
                        hitInfo.transform.GetComponent<IDamageable>().TakeHit(_playerStat.WeaponDamage, dir);
                    }
                    if (hitInfo.transform.GetComponent<Rigidbody>() != null)
                    {
                        hitInfo.transform.GetComponent<Rigidbody>().AddForce(dir * 20f, ForceMode.Impulse);
                    }
                }
            }
        }
        else
        {
            if (Physics.Raycast(Muzzle.position, Muzzle.transform.forward, out hitInfo, 100f))
            {
                _projectileLine.SetPosition(1, hitInfo.point);
            }
            else
            {
                _projectileLine.SetPosition(1, Muzzle.transform.forward * 100f);
            }
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
