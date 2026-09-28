using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkeletonArcherAI : MonoBehaviour
{
    [Header("ステータス")]
    public int maxHealth = 800;
    private int currentHealth;
    private bool isDead = false;

    [Header("移動・距離の設定")]
    public float moveSpeed = 3.0f;
    [Tooltip("プレイヤーにどこまで近づくか（この距離より近づかない）")]
    public float stoppingDistance = 10f;
    [Tooltip("攻撃を開始する距離")]
    public float attackDistance = 12f;

    [Header("攻撃（弓）の設定")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float arrowSpeed = 25f;
    public float attackCooldown = 3.0f;

    [Tooltip("弓を構えた時の体のひねり。")]
    public float aimRotationOffset = -95f;

    [Header("障害物回避の設定")]
    public float avoidDistance = 2.0f;
    public float turnSpeed = 8.0f;

    [Header("段差ジャンプ設定")]
    public float jumpForce = 6.0f;
    public float maxJumpHeight = 1.8f;
    public float stepCheckDistance = 1.0f;
    public float stepCheckHeight = 0.25f;
    public float jumpCooldown = 0.8f;
    private float lastJumpTime = -10f;

    [Header("貫通設定")]
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

    // ★追加：完全に1発しか撃たせないための鉄壁のフラグ
    private bool hasFiredArrowInThisAttack = false;

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
        catch { }
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

        bool isAttacking = stateInfo.IsName("Attack");
        bool isGettingHit = stateInfo.IsName("GetHit");
        bool isDying = stateInfo.IsName("Die");

        if (isDying || animator.IsInTransition(0)) return;

        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 direction = (targetPos - transform.position).normalized;

        if (isAttacking)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

            if (direction != Vector3.zero)
            {
                Quaternion aimRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, aimRotationOffset, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRotation, Time.deltaTime * turnSpeed);
            }

            // ★大改修：フラグがfalseの時だけ撃つ！一度撃ったらこの攻撃が終わるまで絶対に撃たない
            if (stateInfo.normalizedTime >= 0.5f && !hasFiredArrowInThisAttack)
            {
                hasFiredArrowInThisAttack = true; // ここでロックをかける
                ShootArrow();
            }

            // 攻撃が終わるまで待機
            if (stateInfo.normalizedTime < 0.9f) return;
        }
        else
        {
            // ★大改修：攻撃アニメーション「以外」のステート（IdleやRun）に完全に移行した時だけ、フラグを解除する
            hasFiredArrowInThisAttack = false;
        }

        if (isGettingHit)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            if (stateInfo.normalizedTime < 0.9f) return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= stoppingDistance)
        {
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
            }

            if (distance <= attackDistance && Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
            else
            {
                ChangeAnimation("Idle");
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

        ChangeAnimation("Run");
    }

    bool CheckStepAndJump()
    {
        Vector3 lowOrigin = transform.position + Vector3.up * stepCheckHeight;
        Vector3 highOrigin = transform.position + Vector3.up * maxJumpHeight;

        bool hasLowObstacle = Physics.Raycast(lowOrigin, transform.forward, out RaycastHit lowHit, stepCheckDistance);
        if (!hasLowObstacle || !IsObstacle(lowHit.collider)) return false;

        bool hasHighObstacle = Physics.Raycast(highOrigin, transform.forward, out RaycastHit highHit, stepCheckDistance);
        if (hasHighObstacle && IsObstacle(highHit.collider)) return false;

        if (IsGrounded() && Time.time - lastJumpTime >= jumpCooldown)
        {
            if (rb != null) rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
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

        // ここでも念のためフラグをfalseにしておく
        hasFiredArrowInThisAttack = false;

        ChangeAnimation("Attack");

        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, aimRotationOffset, 0);
        }
    }

    void ShootArrow()
    {
        if (arrowPrefab != null && arrowSpawnPoint != null && player != null)
        {
            Vector3 targetPos = player.position + Vector3.up * 1.2f;
            Vector3 shootDirection = (targetPos - arrowSpawnPoint.position).normalized;

            Quaternion arrowRotation = Quaternion.FromToRotation(Vector3.up, shootDirection);
            GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowRotation);

            Rigidbody arrowRb = arrow.GetComponent<Rigidbody>();
            if (arrowRb == null) arrowRb = arrow.AddComponent<Rigidbody>();

            arrowRb.useGravity = false;
            arrowRb.velocity = shootDirection * arrowSpeed;

            Destroy(arrow, 5f);
        }
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
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Attack"))
            {
                return;
            }

            ChangeAnimation("GetHit");
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
        ChangeAnimation("Die");
        if (myCollider != null) myCollider.enabled = false;
        Destroy(gameObject, 3f);
    }

    void ChangeAnimation(string newState)
    {
        if (currentState == newState) return;
        animator.Play(newState, 0, 0f);
        currentState = newState;
    }
}