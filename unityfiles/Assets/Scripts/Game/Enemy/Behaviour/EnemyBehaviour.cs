using System;
using UnityEditor;
using UnityEngine;

public class EnemyBehavourChangeArgs : EventArgs {
	public EnemyBehaviour.BehaviourState NewBehaviourState { get; set; }
}

/// <summary>
/// Enemy AI logic. Controls enemies idle, patrol, chase and attack states.
/// 
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyBehaviour : MonoBehaviour {

	public enum BehaviourState {
		IDLE,
		PATROL,
		CHASE,
		ATTACK
	}

	[SerializeField]
	private bool visualizeVision = true;

	private readonly string WALK_TRIGGER_NAME = "walk";
	private readonly string CHASE_TRIGGER_NAME = "chase";
	private readonly string ATTACK_TRIGGER_NAME = "attack";

	private readonly int MAX_PATROLTIME = 5;
	private readonly int MIN_PATROLTIME = 3;

	private readonly int MAX_IDLE_TIME = 4;
	private readonly int MIN_IDLE_TIME = 2;
	private readonly float TIME_BETWEEN_ATTACKS = 1f;
	private readonly float ATTACK_DISTANCE = .5f;

	private BehaviourState CurrentState { get; set; }

	public float PatrolSpeed { get; set; }
	public float ChaseSpeed { get; set; }
	private float VisionLength { get; set; }
	private float PatrolTime { get; set; }
	private float IdleTime { get; set; }
	private float AttackTimer { get; set; }

	private Enemy Enemy { get; set; }
	private Transform EnemyTransform { get; set; }
	private Transform Vision { get; set; }
	private Animator Animator { get; set; }

	private Transform target { get; set; }

	private bool isFacingRight = true;

	public static event EventHandler<EnemyBehavourChangeArgs> EnemyBehaviourStateChangeEvent;

	private void Awake() {
		if (this.transform.parent.TryGetComponent(out Enemy enemy)) {
			this.Enemy = enemy;
		} else {
			throw new MissingComponentException("Cant find Enemy script i parent.");
		}
		this.Animator = this.GetComponent<Animator>();
	}
	private void Start() {
		this.target = FindObjectOfType<Player>().transform;
		this.InitializeEnemyBehaviour();
	}

	private void InitializeEnemyBehaviour() {

		var type = Enemy.EnemyType;
		this.PatrolSpeed = type.PatrolSpeed;
		this.ChaseSpeed = type.ChaseSpeed;
		this.VisionLength = type.VisionLength;

		EnemyTransform = Enemy.transform;
		this.Vision = Enemy.FrontPoint;

		this.ChooseRandomStartState();
	}

	private void FixedUpdate() {
		EnemyBehavourChangeArgs args = new EnemyBehavourChangeArgs();
		args.NewBehaviourState = CurrentState;

		switch (this.CurrentState) {
			case BehaviourState.IDLE:
				TryChangeToPatrol();
				SetChaseIfCanSeeTarget();
				break;
			case BehaviourState.PATROL:
				Move(this.PatrolSpeed);
				TryChangeToIdle();
				SetChaseIfCanSeeTarget();
				break;
			case BehaviourState.CHASE:
				if (!IsPlayerInAttackReach()) {
					Move(this.ChaseSpeed);
				}
				SetIdleIfLostSightOfTarget();
				SetAttackIfTargetIsInReach();
				break;
			case BehaviourState.ATTACK:
				this.Attack();
				break;
			default:
				Idle();
				break;
		}
		EnemyBehaviourStateChangeEvent?.Invoke(this, args);
	}

	private void ChooseRandomStartState() {
		if (UnityEngine.Random.Range(0, 2) == 0) {
			this.Idle();
		} else {
			this.Patrol();
		}
	}

	private void Move(float speed) {
		this.EnemyTransform.Translate(Vector3.right * Time.deltaTime * speed);
	}

	private void SetAttackIfTargetIsInReach() {
		if (IsPlayerInAttackReach() && AttackTimer < Time.time) {
			this.SetState(BehaviourState.ATTACK);
			AttackTimer = TIME_BETWEEN_ATTACKS + Time.time;
			this.Enemy.IsAttacking = true;
		} else if (AttackTimer < Time.time && this.Enemy.IsAttacking) {
			this.Enemy.IsAttacking = false;
		}
	}

	private void SetChaseIfCanSeeTarget() {

		if (CanSeeTarget()) {
			Chase();
		}
	}

	private bool CanSeeTarget() {
		Vector2 direction = (EnemyTransform.rotation.eulerAngles.y < 90) ? Vector2.right : Vector2.left;
		float eyeHorizontal = this.Vision.position.x;
		float eyeVertical = this.Vision.position.y;
		float eyeReach = eyeHorizontal + (VisionLength * direction.x);
		bool isVisible = false;
		// Check if target are in proper height of the eye
		if (eyeVertical + 5 >= target.transform.position.y && eyeVertical - 3 <= target.transform.position.y) {
			Vector3 debugVisionEndposition = Vision.position;
			if (isFacingRight && eyeHorizontal <= target.position.x && target.position.x < eyeReach) {
				debugVisionEndposition = target.position;
				isVisible = true;
			} else if (eyeHorizontal >= target.position.x && target.position.x > eyeReach) {
				debugVisionEndposition = target.position;
				isVisible = true;
			}

			// For visualizing the vision
			if (this.visualizeVision) {
				Debug.DrawLine(Vision.position, debugVisionEndposition, Color.magenta);
			}
		}
		return isVisible;
	}

	private void SetIdleIfLostSightOfTarget() {
		if (!CanSeeTarget()) {
			Idle();
		}
	}

	private void ChangeWalkingDirection() {
		if (this.isFacingRight) {
			this.EnemyTransform.rotation = new Quaternion(0f, 180f, 0f, 0f);
			this.isFacingRight = false;
		} else {
			this.EnemyTransform.rotation = new Quaternion(0f, 0, 0f, 0f);
			this.isFacingRight = true;
		}
	}

	private void TryChangeToIdle() {
		if (this.PatrolTime < Time.time) {
			Idle();
		}
	}

	private void TryChangeToPatrol() {
		if (this.IdleTime < Time.time) {
			Patrol();
		}
	}

	/// <summary>
	/// Checks if player is in reach for attack
	/// </summary>
	/// <param name="enemy">The enemy to potentially attack the player</param>
	/// <returns>True if in reach, false if not</returns>
	private bool IsPlayerInAttackReach() {
		bool inReach = false;
		if (Math.Abs(Vector2.Distance(Vision.position, target.position)) <= ATTACK_DISTANCE) {
			inReach = true;
		}
		return inReach;
	}

	/// <summary>
	/// Casts a 2D ray from the vision point and in the direction we are facing
	/// and checks if we hit a target. if the correct target is hit,
	/// return the instance, else null.
	/// </summary>
	/// <returns>target or null if not found</returns>
	private Transform TryGetTarget() {
		Transform target = null;
		Vector2 visionPosition = Vision.position;

		// Gets the direction
		Vector2 direction = (EnemyTransform.rotation.eulerAngles.y < 90) ? Vector2.right : Vector2.left;
		RaycastHit2D hitForward = Physics2D.Raycast(visionPosition, direction, VisionLength);

		if (hitForward.collider != null) {
			if (!hitForward.transform.TryGetComponent(out Player p)) {
				// if
			}

		}

		return target;
	}

	/// <summary>
	/// Prevents enemies to fall of the edge when patrolling
	/// </summary>
	private void OnTriggerExit2D(Collider2D _) {
		if (this.CurrentState == BehaviourState.PATROL) {
			Idle();
		}
	}

	private void SetState(BehaviourState state) {
		this.CurrentState = state;
	}

	private void Idle() {
		this.Animator.SetBool(this.WALK_TRIGGER_NAME, false);
		this.IdleTime = Time.time + UnityEngine.Random.Range(MIN_IDLE_TIME, MAX_IDLE_TIME);
		this.SetState(BehaviourState.IDLE);
	}

	private void Patrol() {
		this.Animator.SetBool(this.WALK_TRIGGER_NAME, true);
		this.PatrolTime = Time.time + UnityEngine.Random.Range(MIN_PATROLTIME, MAX_PATROLTIME);
		ChangeWalkingDirection();
		this.SetState(BehaviourState.PATROL);
	}

	private void Chase() {
		this.Animator.SetBool(this.WALK_TRIGGER_NAME, true);
		this.SetState(BehaviourState.CHASE);
	}

	private void Attack() {
		this.Animator.SetTrigger(this.ATTACK_TRIGGER_NAME);
		this.SetState(BehaviourState.CHASE);
	}
}