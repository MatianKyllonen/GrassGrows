using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public PlayerController player;
    public BoardGrid board;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
