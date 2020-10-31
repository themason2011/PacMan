﻿using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.Analytics;

public class Ghost : MonoBehaviour
{

    public float moveSpeed = 5.9f;
    public float normalMoveSpeed = 5.9f;
    public float frightenedMoveSpeed = 2.9f;
    public float consumedMoveSpeed = 15f;

    public bool canMove = true;

    public int blinkyReleaseTimer = 0;
    public int pinkyReleaseTimer = 5;
    public int inkyReleaseTimer = 14;
    public int clydeReleaseTimer = 21;
    public float ghostReleaseTimer = 0;

    public int frightenedModeDuration = 10;
    public int startBlinkingAt = 7;

    public bool isInGhostHouse = false;

    public bool debugInky = false;

    public Node startingPosition;
    public Node homeNode;
    public Node ghostHouse;

    public int scatterModeTimer1 = 7;
    public int chaseModeTimer1 = 20;
    public int scatterModeTimer2 = 7;
    public int chaseModeTimer2 = 20;
    public int scatterModeTimer3 = 5;
    public int chaseModeTimer3 = 20;
    public int scatterModeTimer4 = 5;

    public Sprite eyesUp;
    public Sprite eyesDown;
    public Sprite eyesLeft;
    public Sprite eyesRight;

    public RuntimeAnimatorController ghostUp;
    public RuntimeAnimatorController ghostDown;
    public RuntimeAnimatorController ghostLeft;
    public RuntimeAnimatorController ghostRight;
    public RuntimeAnimatorController ghostBlue;
    public RuntimeAnimatorController ghostWhite;

    private AudioSource backgroundAudio;

    private float frightenedModeTimer = 0;
    private float blinkTimer = 0;

    private bool frightenedModeIsWhite = false;

    private int modeChangeIteration = 1;
    private float modeChangeTimer = 0;

    private float previousMoveSpeed;

    public enum Mode
    {
        Chase,
        Scatter,
        Frightened,
        Consumed
    }

    Mode currentMode = Mode.Scatter;
    Mode previousMode;

    public enum GhostType
    {
        Blinky,
        Pinky,
        Inky,
        Clyde
    }

    public GhostType ghostType;

    private GameObject pacMan;

    private Node currentNode, targetNode, previousNode;
    private Vector2 direction, nextDirection;

    // Start is called before the first frame update
    void Start()
    {
        SetDifficultyForLevel(GameBoard.playerLevel);

        backgroundAudio = GameObject.Find("Game").transform.GetComponent<AudioSource>();

        pacMan = GameObject.FindGameObjectWithTag("PacMan");

        Node node = GetNodeAtPosition(transform.localPosition);

        if(node != null)
        {
            currentNode = node;
        }

        if(isInGhostHouse)
        {
            direction = Vector2.up;
            currentNode = startingPosition;
            targetNode = currentNode.neighbors[0];
        }
        else
        {
            direction = Vector2.left;
            targetNode = ChooseNextNode();
        }

        previousNode = currentNode;

        UpdateAnimatorController();
    }

    // Update is called once per frame
    void Update()
    {
        if(canMove)
        {
            ModeUpdate();

            Move();

            ReleaseGhosts();

            CheckCollision();

            CheckIsInGhostHouse();
        }
    }

    void CheckIsInGhostHouse()
    {
        if(currentMode == Mode.Consumed)
        {
            GameObject tile = GetTileAtPosition(transform.position);

            if(tile != null)
            {
                if(tile.transform.GetComponent<Tile>() != null)
                {
                    if(tile.transform.GetComponent<Tile>().isGhostHouse)
                    {
                        moveSpeed = normalMoveSpeed;

                        Node node = GetNodeAtPosition(transform.position);

                        if(node != null)
                        {
                            currentNode = node;

                            direction = Vector2.up;
                            targetNode = currentNode.neighbors[0];

                            previousNode = currentNode;

                            currentMode = Mode.Chase;

                            UpdateAnimatorController();
                        }
                    }
                }
            }
        }
    }

    void CheckCollision()
    {
        Rect ghostRect = new Rect(transform.position, transform.GetComponent<SpriteRenderer>().sprite.bounds.size / 4);
        Rect pacManRect = new Rect(pacMan.transform.position, pacMan.transform.GetComponent<SpriteRenderer>().sprite.bounds.size / 4);

        if(ghostRect.Overlaps(pacManRect))
        {
            if(currentMode == Mode.Frightened)
            {
                Consumed();
            }
            else if(currentMode != Mode.Consumed)
            {
                GameObject.Find("Game").transform.GetComponent<GameBoard>().StartDeath();
            }
        }
    }

    void Consumed()
    {
        GameBoard.score += GameBoard.ghostConsumedRunningScore;

        currentMode = Mode.Consumed;
        previousMoveSpeed = moveSpeed;
        moveSpeed = consumedMoveSpeed;

        UpdateAnimatorController();

        GameObject.Find("Game").transform.GetComponent<GameBoard>().StartConsumed(this.GetComponent<Ghost>());

        GameBoard.ghostConsumedRunningScore *= 2;
    }

    void UpdateAnimatorController()
    {
        if (currentMode != Mode.Frightened && currentMode != Mode.Consumed)
        {
            if (direction == Vector2.up)
            {
                transform.GetComponent<Animator>().runtimeAnimatorController = ghostUp;
            }
            else if (direction == Vector2.down)
            {
                transform.GetComponent<Animator>().runtimeAnimatorController = ghostDown;
            }
            else if (direction == Vector2.left)
            {
                transform.GetComponent<Animator>().runtimeAnimatorController = ghostLeft;
            }
            else if (direction == Vector2.right)
            {
                transform.GetComponent<Animator>().runtimeAnimatorController = ghostRight;
            }
            else
            {
                transform.GetComponent<Animator>().runtimeAnimatorController = ghostLeft;
            }
        }
        else if (currentMode == Mode.Frightened)
        {
            transform.GetComponent<Animator>().runtimeAnimatorController = ghostBlue;
        }
        else if (currentMode == Mode.Consumed)
        {
            transform.GetComponent<Animator>().runtimeAnimatorController = null;

            if(direction == Vector2.up)
            {
                transform.GetComponent<SpriteRenderer>().sprite = eyesUp;
            }
            else if (direction == Vector2.down)
            {
                transform.GetComponent<SpriteRenderer>().sprite = eyesDown;
            }
            else if (direction == Vector2.left)
            {
                transform.GetComponent<SpriteRenderer>().sprite = eyesLeft;
            }
            else if (direction == Vector2.right)
            {
                transform.GetComponent<SpriteRenderer>().sprite = eyesRight;
            }
        }
    }

    void Move()
    {
        if(targetNode != currentNode && targetNode != null && !isInGhostHouse)
        {
            if(OverShotTarget())
            {
                currentNode = targetNode;

                transform.localPosition = currentNode.transform.position;

                GameObject otherPortal = GetPortal(currentNode.transform.position);

                if(otherPortal != null)
                {
                    transform.localPosition = otherPortal.transform.position;

                    currentNode = otherPortal.GetComponent<Node>();
                }

                targetNode = ChooseNextNode();

                previousNode = currentNode;

                currentNode = null;

                UpdateAnimatorController();
            }
            else
            {
                transform.localPosition += (Vector3)direction * moveSpeed * Time.deltaTime;
            }
        }
    }

    void ModeUpdate()
    {
        if(currentMode != Mode.Frightened)
        {
            modeChangeTimer += Time.deltaTime;

            if(modeChangeIteration == 1)
            {
                if(currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer1)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }

                if (currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer1)
                {
                    modeChangeIteration = 2;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }
            }
            else if(modeChangeIteration == 2)
            {
                if (currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer2)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }

                if (currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer2)
                {
                    modeChangeIteration = 3;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }
            }
            else if (modeChangeIteration == 3)
            {
                if (currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer3)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }

                if (currentMode == Mode.Chase && modeChangeTimer > chaseModeTimer3)
                {
                    modeChangeIteration = 4;
                    ChangeMode(Mode.Scatter);
                    modeChangeTimer = 0;
                }
            }
            else if (modeChangeIteration == 4)
            {
                if (currentMode == Mode.Scatter && modeChangeTimer > scatterModeTimer4)
                {
                    ChangeMode(Mode.Chase);
                    modeChangeTimer = 0;
                }
            }
        }
        else if (currentMode == Mode.Frightened)
        {
            frightenedModeTimer += Time.deltaTime;

            if(frightenedModeTimer >= frightenedModeDuration)
            {
                backgroundAudio.clip = GameObject.Find("Game").transform.GetComponent<GameBoard>().backgroundAudioNormal;
                backgroundAudio.Play();

                frightenedModeTimer = 0;
                ChangeMode(previousMode);
            }

            if(frightenedModeTimer >= startBlinkingAt)
            {
                blinkTimer += Time.deltaTime;

                if(blinkTimer >= 0.1f)
                {
                    blinkTimer = 0f;

                    if(frightenedModeIsWhite)
                    {
                        transform.GetComponent<Animator>().runtimeAnimatorController = ghostBlue;
                        frightenedModeIsWhite = false;
                    }
                    else
                    {
                        transform.GetComponent<Animator>().runtimeAnimatorController = ghostWhite;
                        frightenedModeIsWhite = true;
                    }
                }
            }
        }
    }

    void ChangeMode(Mode m)
    {
        if(currentMode == Mode.Frightened)
        {
            moveSpeed = previousMoveSpeed;
        }

        if(m == Mode.Frightened)
        {
            previousMoveSpeed = moveSpeed;
            moveSpeed = frightenedMoveSpeed;
        }

        if(currentMode != m)
        {
            previousMode = currentMode;
            currentMode = m;
        }

        UpdateAnimatorController();
    }

    void SetDifficultyForLevel(int level)
    {
        if (level == 2)
        {
            scatterModeTimer1 = 7;
            chaseModeTimer1 = 20;
            scatterModeTimer2 = 7;
            chaseModeTimer2 = 20;
            scatterModeTimer3 = 5;
            chaseModeTimer3 = 1033;
            scatterModeTimer4 = 1;

            frightenedModeDuration = 9;
            startBlinkingAt = 6;

            pinkyReleaseTimer = 4;
            inkyReleaseTimer = 12;
            clydeReleaseTimer = 18;

            moveSpeed = 6.9f;
            normalMoveSpeed = 6.9f;
            frightenedMoveSpeed = 3.9f;
            consumedMoveSpeed = 18f;
        }
        else if (level == 3)
        {
            scatterModeTimer1 = 7;
            chaseModeTimer1 = 20;
            scatterModeTimer2 = 7;
            chaseModeTimer2 = 20;
            scatterModeTimer3 = 5;
            chaseModeTimer3 = 1033;
            scatterModeTimer4 = 1;

            frightenedModeDuration = 8;
            startBlinkingAt = 5;

            pinkyReleaseTimer = 3;
            inkyReleaseTimer = 10;
            clydeReleaseTimer = 15;

            moveSpeed = 7.9f;
            normalMoveSpeed = 7.9f;
            frightenedMoveSpeed = 4.9f;
            consumedMoveSpeed = 20f;
        }
        else if (level == 4)
        {
            scatterModeTimer1 = 7;
            chaseModeTimer1 = 20;
            scatterModeTimer2 = 7;
            chaseModeTimer2 = 20;
            scatterModeTimer3 = 5;
            chaseModeTimer3 = 1033;
            scatterModeTimer4 = 1;

            frightenedModeDuration = 7;
            startBlinkingAt = 4;

            pinkyReleaseTimer = 2;
            inkyReleaseTimer = 8;
            clydeReleaseTimer = 13;

            moveSpeed = 8.9f;
            normalMoveSpeed = 8.9f;
            frightenedMoveSpeed = 4.9f;
            consumedMoveSpeed = 22f;
        }
        else if (level >= 5)
        {
            scatterModeTimer1 = 5;
            chaseModeTimer1 = 20;
            scatterModeTimer2 = 5;
            chaseModeTimer2 = 20;
            scatterModeTimer3 = 5;
            chaseModeTimer3 = 1037;
            scatterModeTimer4 = 1;

            frightenedModeDuration = 6;
            startBlinkingAt = 3;

            pinkyReleaseTimer = 2;
            inkyReleaseTimer = 6;
            clydeReleaseTimer = 10;

            moveSpeed = 9.9f;
            normalMoveSpeed = 9.9f;
            frightenedMoveSpeed = 6.9f;
            consumedMoveSpeed = 24f;
        }
    }

    public void MoveToStartingPosition()
    {
        if (transform.name != "Blinky")
        {
            isInGhostHouse = true;
        }

        transform.position = startingPosition.transform.position;

        if(isInGhostHouse)
        {
            direction = Vector2.up;
        }
        else
        {
            direction = Vector2.left;
        }

        UpdateAnimatorController();
    }

    public void Restart()
    {
        canMove = true;

        currentMode = Mode.Scatter;

        moveSpeed = normalMoveSpeed;

        previousMoveSpeed = 0;

        transform.position = startingPosition.transform.position;

        ghostReleaseTimer = 0;
        modeChangeIteration = 1;
        modeChangeTimer = 0;

        currentNode = startingPosition;

        if(isInGhostHouse)
        {
            direction = Vector2.up;
            targetNode = currentNode.neighbors[0];
        }
        else
        {
            direction = Vector2.left;
            targetNode = ChooseNextNode();
        }

        previousNode = currentNode;
        UpdateAnimatorController();
    }

    public void StartFrightenedMode()
    {
        if(currentMode != Mode.Consumed)
        {
            GameBoard.ghostConsumedRunningScore = 200;

            frightenedModeTimer = 0;
            backgroundAudio.clip = GameObject.Find("Game").transform.GetComponent<GameBoard>().backgroundAudioFrightened;
            backgroundAudio.Play();
            ChangeMode(Mode.Frightened);
        }
    }

    Vector2 GetBlinkyTargetTile()
    {
        Vector2 pacManPosition = pacMan.transform.localPosition;
        Vector2 targetTile = new Vector2(Mathf.RoundToInt(pacManPosition.x), Mathf.RoundToInt(pacManPosition.y));

        return targetTile;
    }

    Vector2 GetPinkyTargetTile()
    {
        Vector2 pacManPosition = pacMan.transform.localPosition;
        Vector2 pacManOrientation = pacMan.GetComponent<PacMan>().orientation;

        int pacManPositionX = Mathf.RoundToInt(pacManPosition.x);
        int pacManPositionY = Mathf.RoundToInt(pacManPosition.y);

        Vector2 pacManTile = new Vector2(pacManPositionX, pacManPositionY);
        Vector2 targetTile = pacManTile + (4 * pacManOrientation);

        return targetTile;
    }

    Vector2 GetInkyTargetTile()
    {
        Vector2 pacManPosition = pacMan.transform.localPosition;
        Vector2 pacManOrientation = pacMan.GetComponent<PacMan>().orientation;

        int pacManPositionX = Mathf.RoundToInt(pacManPosition.x);
        int pacManPositionY = Mathf.RoundToInt(pacManPosition.y);

        Vector2 pacManTile = new Vector2(pacManPositionX, pacManPositionY);
        Vector2 targetTile = pacManTile + (2 * pacManOrientation);

        Vector2 tempBlinkyPosition = GameObject.Find("Blinky").transform.localPosition;

        int blinkyPositionX = Mathf.RoundToInt(tempBlinkyPosition.x);
        int blinkyPositionY = Mathf.RoundToInt(tempBlinkyPosition.y);

        tempBlinkyPosition = new Vector2(blinkyPositionX, blinkyPositionY);

        float distance = GetDistance(tempBlinkyPosition, targetTile);
        distance *= 2;

        targetTile = new Vector2(tempBlinkyPosition.x + distance, tempBlinkyPosition.y + distance);

        return targetTile;
    }

    Vector2 GetClydeTargetTile()
    {
        Vector2 pacManPosition = pacMan.transform.localPosition;

        float distance = GetDistance(transform.localPosition, pacManPosition);
        Vector2 targetTile = Vector2.zero;

        if(distance > 8)
        {
            targetTile = new Vector2(Mathf.RoundToInt(pacManPosition.x), Mathf.RoundToInt(pacManPosition.y));
        }
        else if(distance < 8)
        {
            targetTile = homeNode.transform.position;
        }

        return targetTile;
    }

    Vector2 GetTargetTile()
    {
        Vector2 targetTile = Vector2.zero;

        if (ghostType == GhostType.Blinky)
        {
            targetTile = GetBlinkyTargetTile();
        }

        if (ghostType == GhostType.Pinky)
        {
            targetTile = GetPinkyTargetTile();
        }

        if (ghostType == GhostType.Inky)
        {
            targetTile = GetInkyTargetTile();
        }

        if (ghostType == GhostType.Clyde)
        {
            targetTile = GetClydeTargetTile();
        }

        return targetTile;
    }

    Vector2 GetRandomTile()
    {
        int x = Random.Range(0, 28);
        int y = Random.Range(0, 36);

        return new Vector2(x, y);
    }

    void ReleaseBlinky()
    {
        if (ghostType == GhostType.Blinky && isInGhostHouse)
        {
            isInGhostHouse = false;
        }
    }

    void ReleasePinky()
    {
        if(ghostType == GhostType.Pinky && isInGhostHouse)
        {
            isInGhostHouse = false;
        }
    }

    void ReleaseInky()
    {
        if (ghostType == GhostType.Inky && isInGhostHouse)
        {
            isInGhostHouse = false;
        }
    }

    void ReleaseClyde()
    {
        if (ghostType == GhostType.Clyde && isInGhostHouse)
        {
            isInGhostHouse = false;
        }
    }

    void ReleaseGhosts()
    {
        ghostReleaseTimer += Time.deltaTime;

        if (ghostReleaseTimer > blinkyReleaseTimer)
        {
            ReleaseBlinky();
        }

        if (ghostReleaseTimer > pinkyReleaseTimer)
        {
            ReleasePinky();
        }

        if (ghostReleaseTimer > inkyReleaseTimer)
        {
            ReleaseInky();
        }

        if (ghostReleaseTimer > clydeReleaseTimer)
        {
            ReleaseClyde();
        }
    }

    Node ChooseNextNode()
    {
        Vector2 targetTile = Vector2.zero;

        if(currentMode == Mode.Chase)
        {
            targetTile = GetTargetTile();
        }
        else if(currentMode == Mode.Scatter)
        {
            targetTile = homeNode.transform.position;
        }
        else if (currentMode == Mode.Frightened)
        {
            targetTile = GetRandomTile();
        }
        else if (currentMode == Mode.Consumed)
        {
            targetTile = ghostHouse.transform.position;
        }

        Node moveToNode = null;

        Node[] foundNodes = new Node[4];
        Vector2[] foundNodesDirection = new Vector2[4];

        int nodeCounter = 0;

        for(int i= 0; i<currentNode.neighbors.Length; i++)
        {
            if(currentNode.validDirections[i] != direction*-1)
            {
                if(currentMode != Mode.Consumed)
                {
                    GameObject tile = GetTileAtPosition(currentNode.transform.position);

                    if(tile.transform.GetComponent<Tile>().isGhostHouseEntrance == true)
                    {
                        if(currentNode.validDirections[i] != Vector2.down)
                        {
                            foundNodes[nodeCounter] = currentNode.neighbors[i];
                            foundNodesDirection[nodeCounter] = currentNode.validDirections[i];
                            nodeCounter++;
                        }
                    }
                    else
                    {
                        foundNodes[nodeCounter] = currentNode.neighbors[i];
                        foundNodesDirection[nodeCounter] = currentNode.validDirections[i];
                        nodeCounter++;
                    }
                }
                else
                {
                    foundNodes[nodeCounter] = currentNode.neighbors[i];
                    foundNodesDirection[nodeCounter] = currentNode.validDirections[i];
                    nodeCounter++;
                }
            }
        }

        if(foundNodes.Length == 1)
        {
            moveToNode = foundNodes[0];
            direction = foundNodesDirection[0];
        }

        if(foundNodes.Length > 1)
        {
            float leastDistance = 100000f;

            for(int i=0; i<foundNodes.Length; i++)
            {
                if(foundNodesDirection[i] != Vector2.zero)
                {
                    float distance = GetDistance(foundNodes[i].transform.position, targetTile);

                    if(distance < leastDistance)
                    {
                        leastDistance = distance;
                        moveToNode = foundNodes[i];
                        direction = foundNodesDirection[i];
                    }
                }
            }
        }

        return moveToNode;
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard>().board[(int)pos.x, (int)pos.y];

        if(tile != null)
        {
            if(tile.GetComponent<Node>() != null)
            {
                return tile.GetComponent<Node>();
            }
        }

        return null;
    }

    GameObject GetTileAtPosition(Vector2 pos)
    {
        int tileX = Mathf.RoundToInt(pos.x);
        int tileY = Mathf.RoundToInt(pos.y);

        GameObject tile = GameObject.Find("Game").transform.GetComponent<GameBoard>().board[tileX, tileY];

        if(tile != null)
        {
            return tile;
        }

        return null;
    }

    GameObject GetPortal(Vector2 pos)
    {
        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard>().board[(int)pos.x, (int)pos.y];

        if(tile != null)
        {
            if(tile.GetComponent<Tile>().isPortal)
            {
                GameObject otherPortal = tile.GetComponent<Tile>().portalReceiver;
                return otherPortal;
            }
        }
        return null;
    }

    bool OverShotTarget()
    {
        float nodeToTarget = LengthFromNode(targetNode.transform.position);
        float nodeToSelf = LengthFromNode(transform.localPosition);

        return nodeToSelf > nodeToTarget;
    }

    float LengthFromNode(Vector2 targetPosition)
    {
        Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
        return vec.sqrMagnitude;
    }

    float GetDistance(Vector2 posA, Vector2 posB)
    {
        float dx = posA.x - posB.x;
        float dy = posA.y - posB.y;

        float distance = Mathf.Sqrt(dx*dx + dy*dy);

        return distance;
    }
}
