using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;

public class PlayerContoller : MonoBehaviour
{
    public float initialVelocity;
    public AudioClip endSound;
    public AudioClip engineSound;
    public AudioClip explosionSound;
    public AudioClip outOfBoundsSound;

    private bool initialBurn = false;
    private bool launched = false;
    private bool stopped = false;
    private float planetaryAttraction;
    private Vector3 mouseWorldSpace;

    // Cached references (for performance)
    private Rigidbody2D player;
    private GameObject[] planets;
    private Animator animator;
    private AudioSource source;
    private SpriteRenderer spriteRenderer;
    private GameObject UImenu;
    private Text frameText;
    private Button[] levelButtons;
    private Text levelButtonText;
    private GameObject tutorialPanel;
    private Scene thisScene;

    // Use this for initialization
    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        planets = GameObject.FindGameObjectsWithTag("Obstacle");
        animator = GetComponent<Animator>();
        source = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

		// Cache the menu reference and hide the menu.
        UImenu = GameObject.FindWithTag("Menu");
        UImenu.SetActive(false);

		// The 3 menu outcomes are: "ship crashed", "ship lost" and "success".
		// Because 2 out of the 3 possiblities are the same, the default will be set to the "Ship lost" settings.
        frameText = UImenu.GetComponentInChildren<Text>();
		frameText.text = "Ship Lost!";

		// The top button is "Retry" in the first 2 scenarios.
        levelButtons = UImenu.GetComponentsInChildren<Button>();
        levelButtonText = levelButtons[0].GetComponentInChildren<Text>();
        levelButtonText.text = "Retry level";
		levelButtons[0].onClick.AddListener(GameController.instance.LoadLevel);

		// The bottom button is always "save and quit".
        levelButtons[1].onClick.AddListener(GameController.instance.SaveAndQuit);

        tutorialPanel = GameObject.FindWithTag("Tutorial");

        thisScene = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    void Update()
    {
        // Input is only read before the ship is launched.
        if ( !launched && Input.touchCount > 0)
        {
            Touch myTouch = Input.GetTouch(0);

            // The tutorial UI component does not exist on most levels, so "tutorialPanel" will be "null".
            if ( !tutorialPanel )
            {
                mouseWorldSpace = Camera.main.ScreenToWorldPoint(myTouch.position);
                mouseWorldSpace.z = -1000.0f;
                transform.LookAt(mouseWorldSpace, Vector3.forward);
                transform.eulerAngles = new Vector3(0, 0, -transform.eulerAngles.z);

                if (myTouch.phase == TouchPhase.Ended)
                {
                    animator.SetTrigger("Launched");
                    source.PlayOneShot(engineSound, 1F);
                    launched = true;
                }
            }
            // On levels where the tutorial UI component does exist, it is destroyed when the user taps the phone.
            else if ( myTouch.phase == TouchPhase.Ended)
            {
                Destroy(tutorialPanel);
            }
        }
    }

    void FixedUpdate()
    {
        if (launched)
        {
            // When the player launches, we give him a quick boost before letting gravity take over.
            if (!initialBurn)
            {
                player.AddRelativeForce(new Vector2(0, initialVelocity));
                initialBurn = true;
            }

            if (!stopped)
            {
                foreach (GameObject planet in planets)
                {
                    // The force of gravity is defined as: Force = (Gravitational constant * mass1 * mass2)/distance^2.
                    // Because player's mass is negligible compared to the planet, we can lump the Gravitational constant and the
                    // planetary mass into a single "Planetary Attraction" variable (technically the Standard Gravitational Parameter).
                    Vector2 distance = planet.transform.position - transform.position;
                    float distanceSquared = distance.sqrMagnitude;
                    planetaryAttraction = planet.GetComponent<PlanetScript>().planetaryAttraction;

                    // Make sure I'm not dividing by zero.
                    if (distanceSquared != 0)
                    {
                        // "distance.normalized" gives a direction and converts our result to a Vector.
                        player.AddForce(planetaryAttraction * distance.normalized / distanceSquared);
                    }
                }

                // The player snaps to "up" when the mouse button is clicked and then snaps back to the aimed angle.
                // My guess is that this is the velocity vector briefly being zero between Update() and FixedUpdate().
                // This "if" statement prevents this "blip" to zero.
                if (player.velocity.magnitude > 0)
                {
                    // This faces the player in the direction that he is moving.
                    transform.up = player.velocity;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
	{
        // Stop physics from acting on the player.
        player.Sleep();
        stopped = true;

        // Bring up the menu and disconnect the code from the top menu button.
        UImenu.SetActive(true);

        if (other.gameObject.CompareTag("Background"))
		{
            // Play the "lost" audio clip.
            source.PlayOneShot(outOfBoundsSound, 1F);
        }
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            // Play the "destroyed" animation and audio clip.
            animator.SetTrigger("Destroyed");
            source.PlayOneShot(explosionSound, 1F);
        }
        else if (other.gameObject.CompareTag("Finish"))
        {
            // Turn off the ship and play the docking animation. (The animation is on the space station object.)
            spriteRenderer.enabled = false;
            other.gameObject.GetComponent<Animator>().SetTrigger("Docked");
            source.PlayOneShot( endSound, 1F );

            // Check to see how much time has passed since the last ad was played.
            // Time.time is the number of seconds since the game started.  ( Here I'm playing ads every 3 minutes i.e. "> 3 * 60" )
            if (Time.time - GameController.instance.timeSinceLastAd > 3 * 60)
                GameController.instance.ShowAd();

            // This sends the Standard "Level Complete" event to the Unity Analytics server.
            AnalyticsEvent.LevelComplete(thisScene.name);

            // Swap out the Pause menu title.
            frameText.text = "Success!";
            // Swap out the top menu button text.
            levelButtonText.text = "Next Level";

			// The current level is not incremented upon quitting, so a "Resume" from the main menu would require you to replay the level.
			// Therefore, I'm incrementing the current level here.
			GameController.instance.currentScene++;
        }
    }
}