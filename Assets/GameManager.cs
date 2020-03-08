using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Player vars")]
    public Rigidbody2D playerRb;
    public float moveSpeed;
    private float horizontalMovement;
    private float verticalMovement;
    private Vector3 velocity = Vector3.zero;

    [Header("Enemies System")]
    public float enemiesSpeed = 1f;
    List<GameObject> enemies = new List<GameObject>();
    public GameObject enemyPrefab;
    public float enemiesLifetime;
    public float enemiesDecreaseTime = 1f;
    public float enemiesdecreaseRadiusTime = 0.007f;
    public float enemiesSpawnRate = 4f;
    public GameObject[] spawnPoints;

    [Header("Flowers Params System")]
    public GameObject flowerPrefab;
    List<GameObject> flowers = new List<GameObject>();
    public GameObject[] flowerSpawnPoints;
    public float flowersSpawnRate = 3f;
    public int flowersInInventory = 0;
    public int flowersNeeded = 10;
    public float flowerPickUpRange = 2;

    [Header("Objectives System / Win / Loose")]
    public GameObject endGamePlate;
    public bool canLeave = false;
    public bool gameOver = false;
    public GameObject[] objectToHideOnWin;
    public GameObject[] objectToEnableOnWin;
    public Animator rocketAnimator;
    public GameObject[] objectToEnableOnLoose;

    [Header("UI System")]
    public GameObject addFlowerUI;
    public Text objectiveText;
    public float countdown = 30f;
    public Text countdownText;
    public GameObject fadePanel;

    [Header("Allies System")]
    public GameObject[] allies;
    public float allySpawnRate;

    private void Start()
    {
        objectiveText.text = flowersInInventory.ToString("00") + "/" + flowersNeeded.ToString("00");
        InvokeRepeating("Spawner", 0f, enemiesSpawnRate);
        InvokeRepeating("FlowerSpawner", 0f, flowersSpawnRate);
        InvokeRepeating("AllySpawner", allySpawnRate, allySpawnRate);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (gameOver)
        {
            return;
        }

        countdown -= Time.deltaTime;
        countdown = Mathf.Clamp(countdown, 0f, Mathf.Infinity);
        countdownText.text = string.Format("{0:00.00}", countdown);

        if(countdown <= 0)
        {
            gameOver = true;
            foreach (var item in objectToEnableOnLoose)
            {
                item.SetActive(true);
            }
        }

        // Get player movements
        horizontalMovement = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        verticalMovement = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        // Update enemies positions and scale + collider radius
        for (int i = 0; i < enemies.Count; i++)
        {
            Rigidbody2D enemy = enemies[i].GetComponent<Rigidbody2D>();
            Vector3 direction = (playerRb.transform.position - enemies[i].transform.position).normalized;
            enemy.MovePosition(enemies[i].transform.position + direction * enemiesSpeed * Time.deltaTime);
            enemies[i].transform.localScale -= new Vector3(1, 1, 1) * Time.deltaTime * enemiesDecreaseTime;
            enemies[i].GetComponent<CircleCollider2D>().radius -= enemiesdecreaseRadiusTime * Time.deltaTime;
        }

        // Check foreach flower if it near player, if so -> pick it up
        for (int i = 0; i < flowers.Count; i++)
        {
            if(Vector3.Distance(flowers[i].transform.position, playerRb.transform.position) <= flowerPickUpRange)
            {
                StartCoroutine(AddFlowerToInventory());
                GameObject flowerToDelete = flowers[i];
                flowers.Remove(flowerToDelete);
                Destroy(flowerToDelete);
            }
        }

        // enable end plate if enough flowers
        if(flowersInInventory >= flowersNeeded && !canLeave)
        {
            canLeave = true;
            endGamePlate.SetActive(true);
            StartCoroutine(BlinkEndGamePlate());
        }

        // if end plate enabled and player near -> end game
        if(canLeave && Vector3.Distance(endGamePlate.transform.position, playerRb.transform.position) <= 0.3f)
        {
            gameOver = true;
            playerRb.velocity = Vector3.zero;
            StartCoroutine(GameWon());
            Debug.Log("Game won !");
        }

    }

    // Read when the game is won
    public IEnumerator GameWon()
    {
        fadePanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        DestroyAllEnemies();
        foreach (var item in objectToHideOnWin)
        {
            item.SetActive(false);
        }
    
        foreach (var item in objectToEnableOnWin)
        {
            item.SetActive(true);
        }

        rocketAnimator.enabled = true;
        
    }

    public void DestroyAllEnemies()
    {
        foreach (var enemy in enemies)
        {
            enemy.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (gameOver)
        {
            return;
        }

        Vector3 targetVelocity = new Vector2(horizontalMovement, verticalMovement);
        playerRb.velocity = Vector3.SmoothDamp(playerRb.velocity, targetVelocity, ref velocity, .05f);
    }

    private void Spawner()
    {
        if (gameOver)
        {
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject newEnemy = Instantiate(enemyPrefab);
            newEnemy.transform.position = spawnPoints[i].transform.position;
            enemies.Add(newEnemy);
            StartCoroutine(DestroyEnemy(newEnemy));
        }
    }

    private void FlowerSpawner()
    {
        if (gameOver)
        {
            return;
        }

        int spawnerIndex = Mathf.RoundToInt(Random.Range(0, flowerSpawnPoints.Length));
        GameObject flower = Instantiate(flowerPrefab);
        flower.transform.position = flowerSpawnPoints[spawnerIndex].transform.position;
        flowers.Add(flower);
    }

    private void AllySpawner()
    {
        if (gameOver)
        {
            return;
        }

        int allyInArray = Mathf.RoundToInt(Random.Range(0, allies.Length));
        allies[allyInArray].SetActive(true);
        StartCoroutine(DisableAlly(allies[allyInArray]));
    }

    private IEnumerator DisableAlly(GameObject _ally)
    {
        yield return new WaitForSeconds(5);
        _ally.SetActive(false);
    }

    private IEnumerator DestroyEnemy(GameObject _enemy)
    {
        yield return new WaitForSeconds(enemiesLifetime);
        enemies.Remove(_enemy);
        Destroy(_enemy);
    }

    private IEnumerator AddFlowerToInventory()
    {
        flowersInInventory++;
        objectiveText.text = flowersInInventory.ToString("00") + "/" + flowersNeeded.ToString("00");
        addFlowerUI.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        addFlowerUI.SetActive(false);
    }

    public IEnumerator BlinkEndGamePlate()
    {
        while (true)
        {
            endGamePlate.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0f);
            yield return new WaitForSeconds(.5f);
            endGamePlate.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(.5f);
        }
    }

}
