﻿using System;
using Assets.Scripts.Managers;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class PlayerController : MonoBehaviour
    {
        #region Declarations --------------------------------------------------
        private Vector2 _feetContactBox;

        [HideInInspector] 
        public Rigidbody2D rigidBody;

        [HideInInspector] 
        public Animator animator;

        [HideInInspector] 
        public Vector2 size;

        [HideInInspector] 
        public LevelManager levelManager;

        [Tooltip("This is how deep the player's feet overlap with the ground")] //TODO change tooltip
        public float groundedSkin = 0.05f;

        public LayerMask groundLayer;
        public LayerMask killZoneLayer;
        public bool isTouchingWall;
        public bool isGrounded;
        public bool isInKillZone;
        public bool invulnerable;
        public bool dead;

        [HideInInspector]
        public bool firstGrounded;

        [Range(0.1f, 10f)] public float invulnerabilityWindow;
        
        [Range(0.1f, 1f)]
        public float flashTimer;
        
        public Vector3 respawnPosition;

        [Header("Movement Settings")]

        [Range(1, 10)] 
        [Tooltip("Default: 5")]
        public float moveSpeed = 5f;

        [Header("Jump Settings")]

        [Range(1, 10)]
        [Tooltip("Default: 7.5")]
        public float jumpVelocity;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;
        public bool canJump = true;

        private bool canWallJumpLeft = true;
        private bool canWallJumpRight = true;

        [Header("Wall Jump Settings")]
        public int lowGravityTimeout = 15;
        public float lowGravityFallMultiplier = 1f;
        public float verticalVelocity = 5f;
        public float horizontalVelocity = 10.5f;

        private int lowGravityTimer = 0;
        #endregion


        #region Private/Protected Methods -------------------------------------

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            size = GetComponent<BoxCollider2D>().size;
            respawnPosition = transform.position;
            levelManager = FindObjectOfType<LevelManager>();
            _feetContactBox = new Vector2(size.x, groundedSkin);
        }

        private void Update()
        {
            if (!firstGrounded && isGrounded)
                firstGrounded = true;
            if(!isGrounded && gameObject.GetComponent<Rigidbody2D>().velocity.y < 0)
                animator.SetBool("Falling", true);
            else if (isGrounded)
                animator.SetBool("Falling", false);

            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("TouchingWall", this.isTouchingWall);
            animator.SetFloat("SpeedX", Math.Abs(rigidBody.velocity.x));
            Move();
            Jump();

            if(lowGravityTimer > 0)
            {
                lowGravityTimer --;
            }
        }

        private void FixedUpdate()
        {
            var boxCenter = (Vector2) transform.position + (size.y + _feetContactBox.y) * 0.5f * Vector2.down;

            isGrounded = !this.isTouchingWall && Physics2D.OverlapBox(boxCenter, _feetContactBox, 0f, groundLayer);
            isInKillZone = Physics2D.OverlapBox(boxCenter, _feetContactBox, 0f, killZoneLayer);
        }

        private void OnTriggerEnter2D(Component other)
        {
            if (other.CompareTag("KillZone")) 
                levelManager.RespawnPlayer();

            if (other.CompareTag("Checkpoint")) 
                respawnPosition = other.transform.position;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("MovingPlatform"))
            {
                transform.parent = other.transform;
            }

            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                Vector2 pointOfContact = other.GetContact(0).normal;

                if (pointOfContact == Vector2.left || pointOfContact == Vector2.right)
                {
                    this.isGrounded = false;
                    this.isTouchingWall = true;
                }

                else if (pointOfContact == Vector2.up)
                {
                    this.isGrounded = true;
                    this.isTouchingWall = false;
                    this.canWallJumpLeft = true;
                    this.canWallJumpRight = true;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("MovingPlatform")) 
                transform.parent = null;

            this.isTouchingWall = false;
            this.isGrounded = false;
        }

        private void Move()
        {
            var activeVelocity = rigidBody.velocity;
            float xScale;

            if (this.isTouchingWall)
            {
                return;
            }

            if (InputManager.HorizontalDir == InputManager.HorizontalDirections.Left && canWallJumpLeft)
            {
                if (activeVelocity.x > -moveSpeed)
                {
                    activeVelocity.x -= 0.5f;
                }

                xScale = -1;
            }

            else if (InputManager.HorizontalDir == InputManager.HorizontalDirections.Right && canWallJumpRight)
            {
                if (activeVelocity.x < moveSpeed)
                {
                    activeVelocity.x += 0.5f;
                }

                xScale = 1;
            }

            else
            {
                rigidBody.velocity = new Vector2(0f, rigidBody.velocity.y);
                return;
            }

            rigidBody.velocity = new Vector2(activeVelocity.x, rigidBody.velocity.y);
            transform.localScale = new Vector3(xScale, 1f, 1f);
        }

        public void Jump(bool forceJump = false, float velocity = 0f)
        {
            // Should perform normal jump
            if (this.canJump && this.isGrounded && (InputManager.JumpButton || forceJump))
            {
                if (velocity <= 0f)
                {
                    velocity = jumpVelocity;
                }

                this.rigidBody.AddForce(Vector2.up * velocity, ForceMode2D.Impulse);
            }

            else if (this.isTouchingWall && InputManager.JumpButton)
            {
                lowGravityTimer = lowGravityTimeout;
                 float direction = this.levelManager.player.rigidBody.transform.localScale.x;

                // Looking left
                if (direction < 0 && this.canWallJumpLeft)
                {
                    this.canWallJumpLeft = false;
                    this.canWallJumpRight = true;

                    this.rigidBody.AddForce(
                        new Vector2(horizontalVelocity, verticalVelocity),
                        ForceMode2D.Impulse
                    );
                }

                // Looking right
                else if (direction > 0 && this.canWallJumpRight)
                {
                    this.canWallJumpRight = false;
                    this.canWallJumpLeft = true;

                    this.rigidBody.AddForce(
                        new Vector2(horizontalVelocity * -1, verticalVelocity),
                        ForceMode2D.Impulse
                    );
                }
            }

            var multiplier = lowGravityTimer > 0 ? lowGravityFallMultiplier : fallMultiplier;

            if (isTouchingWall)
            {
                multiplier = 0.3f;
            }

            if (rigidBody.velocity.y < 0)
                rigidBody.gravityScale = multiplier;

            else if (rigidBody.velocity.y > 0 && !Input.GetButton("Jump"))
                rigidBody.gravityScale = lowJumpMultiplier;

            else
                rigidBody.gravityScale = 1f;









            //if (canJump && (InputManager.JumpButton || forceJump) && isGrounded)
            //{
            //    if (velocity <= 0f)
            //    {
            //        velocity = jumpVelocity;
            //    }

            //    if (this.isTouchingWall)
            //    {
            //        this.rigidBody.AddForce(
            //            new Vector2(this.levelManager.player.transform.localScale.x, 1).normalized * velocity,
            //            ForceMode2D.Impulse
            //        );
            //    }

            //    else
            //    {
            //        this.rigidBody.AddForce(Vector2.up * velocity, ForceMode2D.Impulse);
            //    }

            //    isGrounded = false;
            //}

            //if (!this.isTouchingWall)
            //{
            //    if (rigidBody.velocity.y < 0)
            //        rigidBody.gravityScale = fallMultiplier;

            //    else if (rigidBody.velocity.y > 0 && !Input.GetButton("Jump"))
            //        rigidBody.gravityScale = lowJumpMultiplier;

            //    else
            //        rigidBody.gravityScale = 1f;
            //}
        }

        public void Kill()
        {
            gameObject.SetActive(false);
            dead = true;
        }

        public void Respawn()
        {
            gameObject.SetActive(true);
            dead = false;
        }

        #endregion
    }
}