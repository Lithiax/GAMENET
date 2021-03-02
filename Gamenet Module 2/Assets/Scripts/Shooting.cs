using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Shooting : MonoBehaviourPunCallbacks
{
    public Camera camera;
    public GameObject hitEffectPrefab;

    public GameObject gameUI;

    [Header("HP Related Stuff")]
    public float startHealth = 100;
    private float health;
    public Image healthBar;

    private Animator animator;
    private int killCount;
    private bool isDead;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        health = startHealth;
        healthBar.fillAmount = health / startHealth;
        isDead = false;
    }

    public void Fire()
    {
        RaycastHit hit;
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out hit, 200))
        {
            PhotonView hitPhotonView = hit.collider.gameObject.GetComponent<PhotonView>();
            Debug.Log(hit.collider.gameObject.name);

            photonView.RPC("CreateHitEffects", RpcTarget.All, hit.point);

            if(hit.collider.gameObject.CompareTag("Player") && !hitPhotonView.IsMine)
            {
                hit.collider.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.AllBuffered, 25, photonView.ViewID);
            }

        }
    }

    [PunRPC]
    public void TakeDamage(int damage, int damageDealerID, PhotonMessageInfo info)
    {
        this.health -= damage;
        this.healthBar.fillAmount = health / startHealth;

        if (health <= 0 && !isDead)
        {
            isDead = true;

            PhotonView killer = PhotonView.Find(damageDealerID);
            if(killer.GetComponent<PhotonView>().IsMine)
            {
                killer.gameObject.GetComponent<Shooting>().UpdateKillCount();
            }
            
            //Debug.Log("Killer ID " + damageDealerID);
            //Debug.Log("Your ID " + photonView.ViewID);
            //Debug.Log(killer.Owner.NickName + " killed " + photonView.Owner.NickName);

            GameManager.instance.LogKillFeed(info.Sender.NickName, info.photonView.Owner.NickName);
            
            Die();
        }
    }

    [PunRPC]
    public void CreateHitEffects(Vector3 position)
    {
        GameObject hitEffectGameObject = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        Destroy(hitEffectGameObject, 0.2f);
    }

    public void Die()
    {
        if(photonView.IsMine)
        {
            animator.SetBool("IsDead", true);
            StartCoroutine(respawnCountDown());
        }
    }

    IEnumerator respawnCountDown()
    {
        GameObject respawnText = GameObject.Find("RespawnText");
        float respawnTime = 5.0f;
        while (respawnTime > 0)
        {
            yield return new WaitForSeconds(1.0f);
            respawnTime--;

            transform.GetComponent<PlayerMovementController>().enabled = false;
            respawnText.GetComponent<Text>().text = "You are killed. Respawning in " + respawnTime.ToString(".00");
        }

        animator.SetBool("IsDead", false);
        respawnText.GetComponent<Text>().text = " ";
        int randomPointX = Random.Range(-20, 20);
        int randomPointZ = Random.Range(-20, 20);

        this.transform.position = GameManager.instance.GetRandomSpawnPoint();
        transform.GetComponent<PlayerMovementController>().enabled = true;
        

        photonView.RPC("RegainHealth", RpcTarget.AllBuffered);
        
    }

    [PunRPC]
    public void RegainHealth()
    {
        health = 100;
        healthBar.fillAmount = health / startHealth;
        isDead = false;
    }
    public void UpdateKillCount()
    {
        killCount++;
        GameObject killCountText = GameObject.Find("KillCount");
        killCountText.GetComponent<Text>().text = "kills: " + killCount.ToString();
        Debug.Log("Current Kills: " + killCount.ToString());

        if(killCount >= 1)
        {
            GameManager.instance.gameObject.GetComponent<PhotonView>().RPC("WinGame", RpcTarget.AllBuffered, photonView.Owner.NickName);
        }
    }
}
