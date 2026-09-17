using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CactusAI : MonoBehaviour
{
    [Header("サボテンのステータス")]
    public int maxHealth = 1100; // スライムより少し硬いくらいの想定
    private int currentHealth;
    private bool isDead = false;

    [Header("サボテンの設定")]
    public float moveSpeed = 2.2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2.0f;
    public int damage = 12;

    [Header("障害物回避の設定")]
    public float avoidDistance = 2.0f;
    public float turnSpeed = 8.0f;

    [Header("段差ジャンプ設定")]
    public float jumpForce = 6.0f;
    [Tooltip("ジャンプして飛び越えられる最大の壁の高さ")]
    public float maxJumpHeight = 1.8f;
    public float stepCheckDistance = 1.0f;
    public float stepCheckHeight = 0.25f;
    public float jumpCooldown = 0.8f;
    private float lastJumpTime = -10f;

    [Header("貫通設定")]
    [Tooltip("敵がすり抜けて通れるようにするオブジェクトのタグ名")]
    public string passThroughTag = "PassThrough";

    [Header("エフェクト設定")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.15f;

    [Header("HP表示UI")]
    public Image hpBarImage;
    public TextMeshProUGUI hpText;

    private Transform player;
    private Animator animator;
    private Rigidbody rb;
    private Collider myCollider;
    private float lastAttackTime;
    private bool hasDealtDamage = false;
    private string currentState = "";

    private Renderer[] renderers;
    private Color[] originalColors;

    void Start()
    {
        currentHealth = maxHealth;
        FindPlayer();

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }

        UpdateHPUI();
        SetupPassThrough();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void SetupPassThrough()
    {
        if (string.IsNullOrEmpty(passThroughTag)) return;

        try
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(passThroughTag);
            Collider[] myColliders = GetComponentsInChildren<Collider>();

            foreach (GameObject obj in targets)
            {
                if (obj == null) continue;
                Collider[] targetCols = obj.GetComponentsInChildren<Collider>();
                foreach (Collider myCol in myColliders)
                {
                    foreach (Collider targetCol in targetCols)
                    {
                        Physics.IgnoreCollision(myCol, targetCol, true);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("タグ '" + passThroughTag + "' の処理でエラー: " + e.Message);
        }
    }

    void Update()
    {
        if (DisablePlayerControl.IsEventActive)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (isDead) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // ★サボテン専用のアニメーション名に変更
        bool isAttacking = stateInfo.IsName("Cactus_Attack01");
        bool isGettingHit = stateInfo.IsName("Cactus_GetHit");
        bool isDying = stateInfo.IsName("Cactus_Die");

        if (isDying || animator.IsInTransition(0)) return;

        if (isAttacking)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

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

        if (isGettingHit)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            if (stateInfo.normalizedTime < 0.9f) return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
            else
            {
                // ★戦闘待機状態のアニメーション
                ChangeAnimation("Cactus_IdleBattle");
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
        Vector3 targetDir = (targetPos - transform.position).normalized * 2.0f;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 left = -transform.right;

        bool isStepAhead = CheckStepAndJump();

        if (!isStepAhead)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            RaycastHit hit;

            if (Physics.Raycast(rayOrigin, forward, out hit, avoidDistance))
            {
                if (IsObstacle(hit.collider)) targetDir += hit.normal * 3.0f;
            }
            else if (Physics.Raycast(rayOrigin, forward + right, out hit, avoidDistance))
            {
                if (IsObstacle(hit.collider)) targetDir += left * 3.0f;
            }
            else if (Physics.Raycast(rayOrigin, forward + left, out hit, avoidDistance))
            {
                if (IsObstacle(hit.collider)) targetDir += right * 3.0f;
            }
        }

        targetDir.y = 0f;

        if (targetDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        }

        if (rb != null)
        {
            Vector3 moveVelocity = transform.forward * moveSpeed;
            rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
        }

        // ★歩行アニメーション
        ChangeAnimation("Cactus_WalkFWD");
    }

    bool CheckStepAndJump()
    {
        Vector3 lowOrigin = transform.position + Vector3.up * stepCheckHeight;
        Vector3 highOrigin = transform.position + Vector3.up * maxJumpHeight;

        bool hasLowObstacle = Physics.Raycast(lowOrigin, transform.forward, out RaycastHit lowHit, stepCheckDistance);
        if (!hasLowObstacle || !IsObstacle(lowHit.collider)) return false;

        bool hasHighObstacle = Physics.Raycast(highOrigin, transform.forward, out RaycastHit highHit, stepCheckDistance);
        if (hasHighObstacle && IsObstacle(highHit.collider))
        {
            return false;
        }

        if (IsGrounded() && Time.time - lastJumpTime >= jumpCooldown)
        {
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            }
            lastJumpTime = Time.time;
        }

        return true;
    }

    bool IsGrounded()
    {
        if (Mathf.Abs(rb.velocity.y) > 2.0f) return false;

        RaycastHit[] groundHits = Physics.RaycastAll(transform.position + Vector3.up * 0.4f, Vector3.down, 0.6f);
        foreach (var gh in groundHits)
        {
            if (gh.collider != myCollider && !gh.collider.isTrigger && !gh.collider.CompareTag("Player") && !gh.collider.CompareTag("Enemy"))
            {
                return true;
            }
        }
        return false;
    }

    bool IsObstacle(Collider col)
    {
        if (col.CompareTag("Player") || col.CompareTag("Enemy")) return false;
        if (col.isTrigger) return false;

        if (!string.IsNullOrEmpty(passThroughTag))
        {
            try
            {
                if (col.CompareTag(passThroughTag) ||
                    (col.transform.parent != null && col.transform.parent.CompareTag(passThroughTag)) ||
                    col.transform.root.CompareTag(passThroughTag))
                {
                    return false;
                }
            }
            catch { }
        }

        return true;
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        hasDealtDamage = false;
        // ★攻撃アニメーション
        ChangeAnimation("Cactus_Attack01");

        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPos);
    }

    void ChangeAnimation(string newState)
    {
        if (currentState == newState) return;
        animator.Play(newState, 0, 0f);
        currentState = newState;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHPUI();
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // ★攻撃中ならひるまない（スーパーアーマー）
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Cactus_Attack01"))
            {
                return;
            }

            // ★被弾アニメーション
            ChangeAnimation("Cactus_GetHit");
        }
    }

    void UpdateHPUI()
    {
        if (hpBarImage != null) hpBarImage.fillAmount = (float)currentHealth / maxHealth;
        if (hpText != null) hpText.text = currentHealth.ToString();
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
        if (isDead) return;
        isDead = true;
        currentHealth = 0;
        UpdateHPUI();

        if (rb != null) rb.isKinematic = true;
        // ★死亡アニメーション
        ChangeAnimation("Cactus_Die");
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 3f);
    }
}