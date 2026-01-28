using UnityEngine;

public class PersonMovement : MonoBehaviour
{
    public bool isInfected = false;

    [Range(0f, 1f)]
    public float infectionProgress = 0f;

    public float infectionTimeRequired = 3f; // seconds needed to infect
    public float infectionRadius = 0.8f;

    SpriteRenderer sr;

    public float moveSpeed = 1.5f;
    public float roamRadius = 5f;

    Vector2 targetPos;
    bool isMoving = false;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ChooseNewAction();
        UpdateColor();
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, targetPos) < 0.1f)
            {
                isMoving = false;
                Invoke(nameof(ChooseNewAction), Random.Range(1f, 3f));
            }
        }

        if (isInfected)
        {
            TryInfectOthers();
        }

    }

    void ChooseNewAction()
    {
        Vector2 newTarget;

        int attempts = 0;
        do
        {
            newTarget = (Vector2)transform.position + Random.insideUnitCircle * roamRadius;
            attempts++;
        }
        while (!IsInsideBounds(newTarget) && attempts < 10);

        targetPos = newTarget;
        isMoving = true;
    }

    bool IsInsideBounds(Vector2 pos)
    {
        return pos.x > -5.5f && pos.x < 5.5f &&
               pos.y > -5.5f && pos.y < 5.5f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            ChooseNewAction();
        }
    }
    void UpdateColor()
    {
        if (sr == null) return;

        Color healthy = Color.white;
        Color infected = Color.red;
        sr.color = Color.Lerp(healthy, infected, infectionProgress);
    }

    public void SetInfected()
    {
        isInfected = true;
        infectionProgress = 1f;
        UpdateColor();
    }

    void TryInfectOthers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            infectionRadius
        );

        foreach (Collider2D hit in hits)
        {
            PersonMovement other = hit.GetComponent<PersonMovement>();

            if (other != null && !other.isInfected)
            {
                other.infectionProgress += Time.deltaTime / infectionTimeRequired;
                other.UpdateColor();

                if (other.infectionProgress >= 1f)
                {
                    other.SetInfected();
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, infectionRadius);
    }



}
