﻿using System.Collections;
using UnityEngine;

/// <summary>
/// Handles enemy shooting, rotation, and death behaviors
/// </summary>
public class EnemyScript : MonoBehaviour
{
    private Rigidbody[] rigidbodies;

    private Animator animator;
    private Transform mainCameraTransform;
    private Rigidbody rbPlayer;
    private FieldOfView fow;
    private PlayerController playerController;

    private bool wasShot;
    private bool isAlive = true;

    public LayerMask targetLayersMask;

    Coroutine shootingCoroutine;

    public AudioSource akShot;

    private float rotationSpeed = 360f * Mathf.Deg2Rad;

    private Vector3 headOffset = new(0, 1.75f, 0);

    private float upperViewAngle = 0.577350269f; // Tan(30)

    private Vector3 gunHeight = new(0, 1.4f, 0);
    private float movementShotSpreadCoefficient = 0.005f;
    private float stationaryShotSpread = 0.01f;

    // Start is called before the first frame update
    void Start()
    {
        mainCameraTransform = Camera.main.transform;
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        fow = GetComponent<FieldOfView>();
        playerController = GetComponent<PlayerController>();
        playerController.SetArsenal("AK-74M");
        rbPlayer = GameObject.Find("Player").GetComponent<Rigidbody>();
        ToggleRagdoll(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAlive)
        {
            return;
        }

        if (PauseMenu.GameIsPaused)
        {
            return;
        }

        if (fow.FindTarget(transform.position + headOffset))
        {
            //transform.LookAt(fow.bestTarget.position - Vector3.up);
            Vector3 rotateTowards = Vector3.Normalize(fow.bestTarget.position - Vector3.up - transform.position);
            //rotateTowards.y = Mathf.Min(Mathf.Max(-0.5f, rotateTowards.y), 0.5f);
            float horizontalMagnitude = Mathf.Sqrt(Mathf.Pow(rotateTowards.x, 2) + Mathf.Pow(rotateTowards.z, 2));
            if (horizontalMagnitude != 0 && rotateTowards.y / horizontalMagnitude > upperViewAngle)
            {
                rotateTowards.y = horizontalMagnitude * upperViewAngle;
            }
            transform.forward = Vector3.RotateTowards(
                transform.forward,
                rotateTowards,
                rotationSpeed * Time.deltaTime, 10);
            if (shootingCoroutine == null)
            {
                shootingCoroutine = StartCoroutine(ShootingCoroutine());
            }
        }
        else
        {
            StopShooting();
            transform.forward = Vector3.RotateTowards(
                transform.forward,
                -Vector3.forward,
                rotationSpeed * Time.deltaTime, 10);
        }
    }

    /// <summary>
    /// Stops the enemy from shooting, toggles ragdoll, and sets wasShot and isAlive
    /// </summary>
    /// <param name="wasShot">True if shot, false if killed with sword</param>
    public void Killed(bool wasShot)
    {
        if (isAlive)
        {
            StopAllCoroutines();
            this.wasShot = wasShot;
            animator.enabled = false;
            ToggleRagdoll(false);
            isAlive = false;
            tag = "DeadEnemy";
            rbPlayer.SendMessage("IncrementKills");
        }
    }

    /// <summary>
    /// Toggles rigidbody physics and applies force to rigidbody when turning rigidbody physics off
    /// </summary>
    /// <param name="rigidBodyOn">Whether to turn rigidbody physics on or off, true or false</param>
    public void ToggleRagdoll(bool rigidBodyOn)
    {
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = rigidBodyOn;
        }
    }

    /// <summary>
    /// Handles individual shots and hit registration
    /// </summary>
    private void Shoot()
    {
        if (Physics.Raycast(transform.position + gunHeight, transform.forward, out RaycastHit hit, 100, targetLayersMask))
        {
            if (!hit.transform.CompareTag("Enemy")) //Won't shoot if another enemy is directly in front of them
            {
                animator.SetTrigger("Attack");
                akShot.Play();
                float shotSpread = rbPlayer.velocity.magnitude * movementShotSpreadCoefficient + stationaryShotSpread;
                if (Physics.Raycast(transform.position + gunHeight, transform.TransformDirection(new Vector3((1 - 2 * Random.value) * shotSpread, (1 - 2 * Random.value) * shotSpread, 1)), out hit, 100, targetLayersMask))
                {
                    if (hit.transform.CompareTag("Player"))
                    {
                        hit.transform.SendMessage("Hit");
                    }
                    else if (hit.transform.CompareTag("Enemy")) //Can still miss and kill other enemies
                    {
                        hit.transform.SendMessage("Killed");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Stops ShootingCoroutine and sets aiming to false in the animator
    /// </summary>
    private void StopShooting()
    {
        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine);
            shootingCoroutine = null;
            animator.SetBool("Aiming", false);
        }
    }

    /// <summary>
    /// Handles enemy shooting pattern
    /// </summary>
    /// <returns></returns>
    IEnumerator ShootingCoroutine()
    {
        animator.SetBool("Aiming", true);

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Aiming"))
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        Shoot();
        yield return new WaitForSeconds(0.5f);
        Shoot();
        yield return new WaitForSeconds(0.5f);
        Shoot();
        yield return new WaitForSeconds(1f);

        shootingCoroutine = null;
    }
}