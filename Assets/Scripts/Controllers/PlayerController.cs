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
        public bool isGrounded;
        public bool isInKillZone;
        public bool invulnerable;
        public bool dead;

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
            animator.SetBool("Grounded", isGrounded);
            animator.SetFloat("SpeedX", Math.Abs(rigidBody.velocity.x));
            Move();
            Jump();
        }

        private void FixedUpdate()
        {
            var boxCenter = (Vector2) transform.position + (size.y + _feetContactBox.y) * 0.5f * Vector2.down;

            isGrounded = Physics2D.OverlapBox(boxCenter, _feetContactBox, 0f, groundLayer);
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
                transform.parent = other.transform;
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("MovingPlatform")) 
                transform.parent = null;
        }

        private void Move()
        {
            var activeVelocity = rigidBody.velocity;
            float xScale;
            
            
            if (InputManager.HorizontalDir == InputManager.HorizontalDirections.Left)
            {
                if (activeVelocity.x > -moveSpeed)
                {
                    activeVelocity.x -= 0.5f;
                }
                xScale = -1;
            }
            else if (InputManager.HorizontalDir == InputManager.HorizontalDirections.Right)
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
            if ((InputManager.JumpButton || forceJump) && isGrounded)
            {
                if (velocity <= 0f)
                    velocity = jumpVelocity;

                rigidBody.AddForce(Vector2.up * velocity, ForceMode2D.Impulse);
                isGrounded = false;
            }

            if (rigidBody.velocity.y < 0)
                rigidBody.gravityScale = fallMultiplier;
            else if (rigidBody.velocity.y > 0 && !Input.GetButton("Jump"))
                rigidBody.gravityScale = lowJumpMultiplier;
            else
                rigidBody.gravityScale = 1f;
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