
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TicTacToe : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject startPanel;
    public GameObject gamePanel;

    [Header("Menu Buttons")]
    public Button startButton;
    public Button backButton;
    public Button restartButton;

    [Header("Game UI")]
    public TMP_Text titleText;
    public TMP_Text turnText;
    public TMP_Text resultText;
    public TMP_Text scoreText;

    [Header("Board Buttons - Row Order")]
    public Button[] buttons = new Button[9];
    public TMP_Text[] cellTexts = new TMP_Text[9];

    [Header("Winning Line")]
    public RectTransform board;
    public RectTransform winningLine;

    [Header("Game Settings")]
    public float aiDelay = 0.5f;
    public int winningScore = 3;

    [Header("AI Difficulty")]
    [Range(0f, 1f)]
    public float aiRandomChance = 0.75f;

    private string[] cells = new string[9];

    private readonly int[,] winPatterns =
    {
        { 0, 1, 2 },
        { 3, 4, 5 },
        { 6, 7, 8 },
        { 0, 3, 6 },
        { 1, 4, 7 },
        { 2, 5, 8 },
        { 0, 4, 8 },
        { 2, 4, 6 }
    };

    private int xScore = 0;
    private int oScore = 0;

    private string currentPlayer = "O";

    private bool gameOver = true;
    private bool matchOver = false;
    private bool aiThinking = false;

    private Coroutine aiRoutine;

    private void Start()
    {
        // Connect menu buttons.
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogError("Assign StartButton in the Inspector.");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToMenu);
            backButton.onClick.AddListener(BackToMenu);
        }
        else
        {
            Debug.LogError("Assign BackButton in the Inspector.");
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartRound);
            restartButton.onClick.AddListener(RestartRound);
        }
        else
        {
            Debug.LogError("Assign RestartButton in the Inspector.");
        }

        // Validate the nine board cells.
        if (buttons == null || buttons.Length != 9 ||
            cellTexts == null || cellTexts.Length != 9)
        {
            Debug.LogError(
                "The game needs 9 Buttons and 9 TMP cell texts.");
            enabled = false;
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            if (buttons[i] == null || cellTexts[i] == null)
            {
                Debug.LogError(
                    "Assign Button and TMP Text for board cell " + i);
                enabled = false;
                return;
            }

            int index = i;

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(
                () => PlayerMove(index));
        }

        if (winningLine != null)
            winningLine.gameObject.SetActive(false);

        // Show Start Menu at launch.
        if (startPanel != null)
            startPanel.SetActive(true);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        UpdateScore();
    }

    // START BUTTON
    public void StartGame()
    {
        Debug.Log("START BUTTON CLICKED");

        StopAITurn();

        xScore = 0;
        oScore = 0;
        matchOver = false;

        UpdateScore();

        if (startPanel != null)
            startPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(true);

        RestartRound();
    }

    // BACK BUTTON
    public void BackToMenu()
    {
        Debug.Log("BACK BUTTON CLICKED");

        StopAITurn();

        gameOver = true;

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (startPanel != null)
            startPanel.SetActive(true);
    }

    // RESTART BUTTON
    // Resets the board but keeps the current match score.
    public void RestartRound()
    {
        Debug.Log("RESTART BUTTON CLICKED");

        StopAITurn();

        gameOver = false;
        matchOver = false;
        aiThinking = false;
        currentPlayer = "O";

        for (int i = 0; i < 9; i++)
        {
            cells[i] = "";

            if (cellTexts[i] != null)
                cellTexts[i].text = "";

            if (buttons[i] != null)
                buttons[i].interactable = true;
        }

        if (winningLine != null)
            winningLine.gameObject.SetActive(false);

        if (resultText != null)
            resultText.text = "";

        UpdateTurn();

        Debug.Log("BOARD RESET COMPLETE");
    }

    // HUMAN PLAYER: O
    private void PlayerMove(int index)
    {
        if (gameOver || matchOver || aiThinking)
            return;

        if (index < 0 || index >= 9)
            return;

        if (!string.IsNullOrEmpty(cells[index]))
            return;

        PlaceMove(index, "O");

        if (CheckGameEnd("O"))
            return;

        currentPlayer = "X";
        UpdateTurn();

        aiRoutine = StartCoroutine(AITurn());
    }

    // AI PLAYER: X
    private IEnumerator AITurn()
    {
        aiThinking = true;

        yield return new WaitForSeconds(aiDelay);

        if (!gameOver && !matchOver)
        {
            int move;

            // Sometimes choose a random move to make the AI easier.
            if (Random.value < aiRandomChance)
            {
                move = FindRandomMove();
            }
            else
            {
                move = FindBestMove();

                if (move < 0)
                    move = FindRandomMove();
            }

            if (move >= 0)
            {
                PlaceMove(move, "X");

                if (!CheckGameEnd("X"))
                {
                    currentPlayer = "O";
                    UpdateTurn();
                }
            }
        }

        aiThinking = false;
        aiRoutine = null;
    }

    // RANDOM LEGAL MOVE
    private int FindRandomMove()
    {
        List<int> available = new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (string.IsNullOrEmpty(cells[i]))
                available.Add(i);
        }

        if (available.Count == 0)
            return -1;

        return available[Random.Range(0, available.Count)];
    }

    // STRATEGIC AI MOVE
    private int FindBestMove()
    {
        int move = FindWinningMove("X");

        if (move >= 0)
            return move;

        move = FindWinningMove("O");

        if (move >= 0)
            return move;

        if (string.IsNullOrEmpty(cells[4]))
            return 4;

        int[] corners = { 0, 2, 6, 8 };

        foreach (int i in corners)
        {
            if (string.IsNullOrEmpty(cells[i]))
                return i;
        }

        int[] sides = { 1, 3, 5, 7 };

        foreach (int i in sides)
        {
            if (string.IsNullOrEmpty(cells[i]))
                return i;
        }

        return -1;
    }

    private int FindWinningMove(string player)
    {
        for (int i = 0; i < 9; i++)
        {
            if (!string.IsNullOrEmpty(cells[i]))
                continue;

            cells[i] = player;
            bool wins = HasWinner(player);
            cells[i] = "";

            if (wins)
                return i;
        }

        return -1;
    }

    // PLACE X OR O
    private void PlaceMove(int index, string player)
    {
        cells[index] = player;

        cellTexts[index].text = player;

        cellTexts[index].color =
            player == "O"
            ? new Color(0.25f, 0.65f, 1f)
            : new Color(1f, 0.25f, 0.3f);
    }

    // CHECK WIN OR DRAW
    private bool CheckGameEnd(string player)
    {
        int[] winningCells = GetWinningLine(player);

        if (winningCells != null)
        {
            gameOver = true;

            DrawWinningLine(
                winningCells[0],
                winningCells[2]);

            if (player == "O")
                oScore++;
            else
                xScore++;

            UpdateScore();
            DisableBoard();

            if (xScore >= winningScore ||
                oScore >= winningScore)
            {
                matchOver = true;

                resultText.text =
                    player + " WINS THE MATCH!";
            }
            else
            {
                resultText.text =
                    player + " WINS THE ROUND!";
            }

            return true;
        }

        bool full = true;

        foreach (string cell in cells)
        {
            if (string.IsNullOrEmpty(cell))
            {
                full = false;
                break;
            }
        }

        if (full)
        {
            gameOver = true;
            resultText.text = "DRAW!";
            DisableBoard();
            return true;
        }

        return false;
    }

    private void DisableBoard()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.interactable = false;
        }
    }

    private bool HasWinner(string player)
    {
        return GetWinningLine(player) != null;
    }

    private int[] GetWinningLine(string player)
    {
        for (int i = 0; i < winPatterns.GetLength(0); i++)
        {
            int a = winPatterns[i, 0];
            int b = winPatterns[i, 1];
            int c = winPatterns[i, 2];

            if (cells[a] == player &&
                cells[b] == player &&
                cells[c] == player)
            {
                return new int[] { a, b, c };
            }
        }

        return null;
    }

    // DRAW LINE THROUGH THE THREE WINNING CELLS
    private void DrawWinningLine(int first, int last)
    {
        if (winningLine == null || board == null)
            return;

        RectTransform firstRect =
            buttons[first].GetComponent<RectTransform>();

        RectTransform lastRect =
            buttons[last].GetComponent<RectTransform>();

        Vector3 firstWorld =
            firstRect.TransformPoint(firstRect.rect.center);

        Vector3 lastWorld =
            lastRect.TransformPoint(lastRect.rect.center);

        Vector2 start =
            board.InverseTransformPoint(firstWorld);

        Vector2 end =
            board.InverseTransformPoint(lastWorld);

        Vector2 direction = end - start;

        winningLine.anchoredPosition =
            (start + end) / 2f;

        winningLine.sizeDelta =
            new Vector2(direction.magnitude, 12f);

        winningLine.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg);

        winningLine.gameObject.SetActive(true);
        winningLine.SetAsLastSibling();
    }

    private void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "O: " + oScore + "     X: " + xScore;
        }
    }

    private void UpdateTurn()
    {
        if (turnText != null)
            turnText.text = currentPlayer + "'s TURN";
    }

    private void StopAITurn()
    {
        if (aiRoutine != null)
        {
            StopCoroutine(aiRoutine);
            aiRoutine = null;
        }

        aiThinking = false;
    }
}
