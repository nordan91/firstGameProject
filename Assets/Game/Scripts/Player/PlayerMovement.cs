using UnityEngine;

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
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;
    [SerializeField]
    private float _climbSpeed;

    private bool _isGrounded;
    private float _speed;
    private Rigidbody _rb;
    private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;
    private PlayerStance _playerStance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _speed = _walkspeed;
        _playerStance = PlayerStance.Stand;
    }
    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelInput += cancelClimb;
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelInput -= cancelClimb;
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
    }
    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;

        if (isPlayerStanding)
        {
            if (axisDirection.magnitude >= 0.1)
            {
                float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                _rb.AddForce(movementDirection * Time.deltaTime * _speed);
            }
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;
            movementDirection = horizontal + vertical;
            _rb.AddForce(movementDirection * Time.deltaTime * _climbSpeed);
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
        }
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
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
            transform.position = hit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rb.useGravity = false;
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
        }
    }
}
