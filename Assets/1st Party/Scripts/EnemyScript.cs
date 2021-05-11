using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    Rigidbody[] rigidbodies;
    bool isAlive = true;

    private Animator animator;
    private Transform mainCameraTransform;
    public Rigidbody rbPlayer;
    private FieldOfView fow;

    public bool wasShot;

    public LayerMask targetLayersMask;

    Coroutine shootingCoroutine;

    public AudioSource akShot;

    // Start is called before the first frame update
    void Start()
    {
        mainCameraTransform = Camera.main.transform;
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        fow = GetComponent<FieldOfView>();
        ToggleRagdoll(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (fow.FindTarget())
        {
            transform.LookAt(fow.bestTarget);
            if (shootingCoroutine == null)
            {
                StartCoroutine(ShootingCoroutine());
            }
            
        }
    }

    public void Killed(bool wasShot)
    {
        if (isAlive)
        {
            this.wasShot = wasShot;
            animator.enabled = false;
            ToggleRagdoll(false);
            isAlive = false;
        }
    }
    
    public void ToggleRagdoll(bool v)
    {
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = v;
            if (!v)
            {
                if (wasShot)
                {
                    rigidbody.AddForce(mainCameraTransform.forward * 500);
                } else
                {
                    rigidbody.AddForce(mainCameraTransform.forward * 500 + rbPlayer.velocity * 100);
                }
            }
        }
    }

    Vector3 gunHeight = new Vector3(0, 1.4f, 0);
    private float movementShotSpreadCoefficient = 0.04f;
    private float stationaryShotSpread = 0.05f;
    /// <summary>
    /// Handles individual shots and hit registration
    /// </summary>
    private void Shoot()
    {
        if (Physics.Raycast(transform.position + gunHeight, transform.forward, out RaycastHit hit, 100, targetLayersMask))
        {
            if (hit.transform.tag != "Enemy") //Won't shoot if another enemy is directly in front of them
            {
                animator.SetTrigger("Attack");
                akShot.Play();
                float shotSpread = rbPlayer.velocity.magnitude * movementShotSpreadCoefficient + stationaryShotSpread;
                if (Physics.Raycast(transform.position + gunHeight, transform.TransformDirection(new Vector3((1 - 2 * Random.value) * shotSpread, (1 - 2 * Random.value) * shotSpread, 1)), out hit, 100, targetLayersMask))
                {
                    if (hit.transform.tag == "Player")
                    {
                        hit.transform.SendMessage("Hit");
                    }
                    else if (hit.transform.tag == "Enemy") //Can still miss and kill other enemies
                    {
                        hit.transform.SendMessage("Killed");
                    }
                }
            }
        }
    }

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
