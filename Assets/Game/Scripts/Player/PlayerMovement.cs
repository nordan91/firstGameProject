using System.Diagnostics;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float _walkspeed;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _walkSprintTransition;
    [SerializeField]
    private InputManager _input;
    [SerializeField]
    private float _jumpForce;
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;
    [SerializeField]
    private LayerMask _groundLayer;
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbCheckDistance;
    [SerializeField]
    private float _climbCheckRadius;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;
    [SerializeField]
    private float _climbSpeed;
    [SerializeField]
    private Transform _cameraTransform;
    [SerializeField]
    private CameraManager _cameraManager;
    [SerializeField]
    private float _crouchspeed;
    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private float _airdrag;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _maxGlideAngle;
    [SerializeField]
    private float _minGlideAngle;
    [SerializeField]
    private float _resetComboInterval;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private LayerMask _hitLayer;


    private bool _isGrounded;
    private float _speed;
    private Rigidbody _rb;
    private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;
    private PlayerStance _playerStance;
    private Animator _animator;
    private CapsuleCollider _collider;
    private bool _isPunching;
    private int _combo = 0;
    private Coroutine _resetCombo;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _speed = _walkspeed;
        _playerStance = PlayerStance.Stand;
        HideAndLockCursor();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider>();
    }
    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelInput += cancelClimb;
        _cameraManager.OnCameraSwitch += changeCamera;
        _input.OnCrouchInput += crouch;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlideInput += CancelGlide;
        _input.OnPunchInput += Punch;

    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelInput -= cancelClimb;
        _input.OnCrouchInput -= crouch;
        _cameraManager.OnCameraSwitch -= changeCamera;
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlideInput -= CancelGlide;
        _input.OnPunchInput -= Punch;
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
        CheckClimbBounds();
        glide();
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isCrouching = _playerStance == PlayerStance.Crouch;
        bool isPlayerGliding = _playerStance == PlayerStance.Glide;

        if (isPlayerStanding || isCrouching && !_isPunching)
        {
            switch (_cameraManager.CameraState)
            {
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;
                    _rb.AddForce(movementDirection * Time.deltaTime * _speed);
                    break;
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1)
                    {
                        float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rb.AddForce(movementDirection * Time.deltaTime * _speed);
                    }
                    break;
                default:
                    break;
            }
            Vector3 velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
            _animator.SetFloat("Velocity", velocity.magnitude * axisDirection.magnitude);
            _animator.SetFloat("Velocity Z", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("Velocity X", velocity.magnitude * axisDirection.x);

        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;
            movementDirection = horizontal + vertical;
            _rb.AddForce(movementDirection * Time.deltaTime * _climbSpeed);
            Vector3 velocity = new Vector3(_rb.velocity.x, _rb.velocity.y, 0);
            _animator.SetFloat("climbVelocityY", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("climbVelocityX", velocity.magnitude * axisDirection.x);
        }

        else if (isPlayerGliding)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideAngle, _maxGlideAngle);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
        }

    }

    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
            if (_speed < _sprintSpeed)
            {
                _speed = _speed + _walkSprintTransition * Time.deltaTime;
            }

        }
        else
        {
            if (_speed > _walkspeed)
            {
                _speed = _speed - _walkSprintTransition * Time.deltaTime;
            }
        }
    }

    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rb.AddForce(jumpDirection * Time.deltaTime * _jumpForce);
            _animator.SetTrigger("Jump");
        }
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("isGrounded", _isGrounded);
        if (_isGrounded)
        {
            CancelGlide();
        }
    }

    private void CheckStep()
    {
        bool isHItLowerStep = Physics.Raycast(_groundDetector.position, transform.forward, _stepCheckerDistance, _groundLayer);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset, transform.forward, _stepCheckerDistance, _groundLayer);

        if (isHItLowerStep && !isHitUpperStep)
        {
            Vector3 stepUpForce = (Vector3.up + transform.forward) * _stepForce;
            _rb.AddForce(stepUpForce, ForceMode.Impulse);
        }
    }

    private void CheckClimbBounds()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            bool isStillClimbing = Physics.CheckSphere(_climbDetector.position, _climbCheckRadius, _climbableLayer);
            if (!isStillClimbing)
            {
                cancelClimb();
            }
        }
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbingWall = Physics.Raycast(
        _climbDetector.position,
        transform.forward,
        out RaycastHit hit,
        _climbCheckDistance,
        _climbableLayer);

        bool isNotClimbing = _playerStance != PlayerStance.Climb;
        if (isInFrontOfClimbingWall && _isGrounded && isNotClimbing)
        {
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset - transform.forward * 0.2f;
            _playerStance = PlayerStance.Climb;
            _rb.useGravity = false;
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(60f);
            _animator.SetBool("isClimbing", true);
            _collider.center = Vector3.up * 1.3f;

        }
    }

    private void cancelClimb()
    {
        bool isClimbing = _playerStance == PlayerStance.Climb;
        if (isClimbing)
        {
            _playerStance = PlayerStance.Stand;
            _rb.useGravity = true;
            transform.position -= transform.forward;
            _speed = _walkspeed;
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(45f);
            _animator.SetBool("isClimbing", false); 
            _collider.center = Vector3.up * 0.9f;
        }
    }

    private void changeCamera()
    {
        _animator.SetTrigger("ChangeCamera");
    }

    private void crouch()
    {
        if (_playerStance == PlayerStance.Stand)
        {
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("isCrouch", true);
            _speed = _crouchspeed;
            _collider.height = 1.0f;
            _collider.center = Vector3.up * 0.5f;
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            // Check if there's space to stand up
            Vector3 rayStart = transform.position + Vector3.up * 1.0f; // Top of crouch collider
            float rayDistance = 0.8f; // Height difference between crouch and stand
            if (!Physics.Raycast(rayStart, Vector3.up, rayDistance))
            {
                _playerStance = PlayerStance.Stand;
                _animator.SetBool("isCrouch", false);
                _speed = _walkspeed;
                _collider.height = 1.8f;
                _collider.center = Vector3.up * 0.9f;
            }
            // If obstructed, remain crouched
        }
    }
    private void glide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + _airdrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rb.AddForce(totalForce * Time.deltaTime);
        }
    }

    private void StartGlide()
    {
        if (!_isGrounded && _playerStance != PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("isGliding", true);
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);

        }
    }

    private void CancelGlide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("isGliding", false);
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        }
    }

    private void Punch()
    {
        if (!_isPunching && _playerStance == PlayerStance.Stand)
        {
            _isPunching = true;
            if (_combo < 3)
            {
                _combo = _combo + 1;
            }
            else
            {
                _combo = 1;
            }
            _animator.SetInteger("Combo", _combo);
            _animator.SetTrigger("Punch");
        }

    }

    private void EndPunch()
    {
        _isPunching = false;
        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }
        _resetCombo = StartCoroutine(ResetCombo());
    }

    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _combo = 0;
    }

    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position,
        _hitDetectorRadius,
        _hitLayer);
        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }
}

