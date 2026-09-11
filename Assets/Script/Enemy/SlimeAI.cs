using System.Collections; // ★追加：コルーチン（数秒待機する処理）を使うために必要
using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    [Header("スライムのステータス")]
    public int maxHealth = 1000;
    private int currentHealth;
    private bool isDead = false;

    [Header("スライムの設定")]
    public float moveSpeed = 2.0f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2.0f;
    public int damage = 10;

    [Header("エフェクト設定")]
    [Tooltip("ダメージを受けた時に点滅する色")]
    public Color damageColor = Color.red; // ★追加：ダメージ時の色
    [Tooltip("色が変わっている時間（秒）")]
    public float flashDuration = 0.15f;   // ★追加：点滅時間

    private Transform player;
    private Animator animator;
    private float lastAttackTime;
    private bool hasDealtDamage = false;
    private string currentState = "";

    // ★追加：色を変えるための変数
    private Renderer[] renderers;
    private Color[] originalColors;

    void Start()
    {
        currentHealth = maxHealth;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        animator = GetComponent<Animator>();

        // ★追加：スライムの3Dモデル（Renderer）を全て取得し、最初の「元の色」を記憶しておく
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    void Update()
    {
        if (DisablePlayerControl.IsEventActive) return;
        if (player == null || isDead) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0) || (stateInfo.IsName("Attack01") && stateInfo.normalizedTime < 0.9f))
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
            else
            {
                ChangeAnimation("IdleNormal");
            }
        }
        else
        {
            Chase();
        }
    }

    void Chase()
    {
        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPos);
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        ChangeAnimation("WalkFWD");
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        hasDealtDamage = false;

        ChangeAnimation("Attack01");
    }

    void ChangeAnimation(string newState)
    {
        if (currentState == newState) return;

        animator.CrossFade(newState, 0.1f);
        currentState = newState;
    }

    void OnTriggerStay(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Player") && currentState == "Attack01" && !hasDealtDamage)
        {
            PlayerHealth hpScript = other.GetComponent<PlayerHealth>();

            if (hpScript != null)
            {
                hpScript.TakeDamage(damage);
            }

            hasDealtDamage = true;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log("スライムに " + damageAmount + " のダメージ！ 残りHP: " + currentHealth);

        // ★追加：ダメージを受けたら赤く光らせる処理（コルーチン）をスタート
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            ChangeAnimation("GetHit");
        }
    }

    // ★追加：一瞬だけ色を変えて、また元に戻す処理
    private IEnumerator DamageFlash()
    {
        // 1. 全てのパーツを赤色（damageColor）にする
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = damageColor;
            }
        }

        // 2. 指定した時間（0.15秒）だけここで処理をストップして待つ
        yield return new WaitForSeconds(flashDuration);

        // 3. 時間が経ったら元の色に戻す
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !isDead) // 待っている間に死んで消滅していないかチェック
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    void Die()
    {
        isDead = true;
        currentHealth = 0;

        ChangeAnimation("Die");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}