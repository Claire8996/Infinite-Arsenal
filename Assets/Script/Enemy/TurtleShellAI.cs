using System.Collections;
using UnityEngine;

public class TurtleShellAI : MonoBehaviour
{
    [Header("タートルシェルのステータス")]
    public int maxHealth = 1500;
    private int currentHealth;
    private bool isDead = false;

    [Header("タートルシェルの設定")]
    public float moveSpeed = 1.5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2.0f;
    public int damage = 15;

    [Header("エフェクト設定")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.15f;

    private Transform player;
    private Animator animator;
    private float lastAttackTime;
    private bool hasDealtDamage = false;
    private string currentState = "";

    private Renderer[] renderers;
    private Color[] originalColors;

    void Start()
    {
        currentHealth = maxHealth;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        animator = GetComponent<Animator>();

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

        bool isAttacking = stateInfo.IsName("Attack01");
        bool isGettingHit = stateInfo.IsName("GetHit");

        if (animator.IsInTransition(0)) return;

        // ==============================================
        // ★大改修：物理(Collider)に頼らない確実なダメージ判定
        // ==============================================
        if (isAttacking)
        {
            if (stateInfo.normalizedTime >= 0.5f && !hasDealtDamage)
            {
                float currentDist = Vector3.Distance(transform.position, player.position);

                if (currentDist <= attackRange + 0.5f)
                {
                    PlayerHealth hpScript = player.GetComponent<PlayerHealth>();
                    if (hpScript != null) hpScript.TakeDamage(damage);
                }
                hasDealtDamage = true;
            }

            if (stateInfo.normalizedTime < 0.9f) return;
        }

        if (isGettingHit && stateInfo.normalizedTime < 0.9f) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown) Attack();
            else ChangeAnimation("IdleNormal");
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

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log("タートルシェルに " + damageAmount + " のダメージ！ 残りHP: " + currentHealth);

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            ChangeAnimation("GetHit");
            animator.Play("GetHit", 0, 0f);
        }
    }

    private IEnumerator DamageFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = damageColor;
        }
        yield return new WaitForSeconds(flashDuration);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !isDead) renderers[i].material.color = originalColors[i];
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