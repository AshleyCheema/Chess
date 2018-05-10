using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Chessboard : MonoBehaviour
{
    public static Chessboard Instance { set; get; }
    private bool[,] AllowedMoves { set; get; }

    public Chessman[,] Chessmans { set; get; }
    private Chessman selectedChessman;

    private const float TILE_SIZE = 1.0f;
    private const float TILE_OFFSET = 0.5f;

    private int selectionX = -1;
    private int selectionY = -1;

    public List<GameObject> chessmanPrefabs;
    private List<GameObject> activeChessman;

    private Material previousMat;
    public Material selectedMat;

    public int[] enPassantMove { set; get; }

    private Quaternion orientation = Quaternion.Euler(0, 0, 0);

    public bool isWhiteTurn = true;

    RaycastHit hit;

    private void Start()
    {
        Instance = this;
        SpawnAllChessMans();
    }

    private void Update()
    {
        UpdateSelection();
        DrawChessboard();

        if(Input.GetMouseButtonDown (0))
        {
            if(selectionX >= 0 && selectionY >= 0)
            {
                if(selectedChessman == null)
                {
                    SelectChessman(selectionX, selectionY);
                }
                else
                {
                    MoveChessman(selectionX, selectionY);
                }
            }
        }
    }

    private void SelectChessman(int x, int y)
    {
        if (Chessmans[x, y] == null)
        {
            return;
        }

        if (Chessmans[x, y].isWhite != isWhiteTurn)
        {
            return;
        }

        bool hasAtLeastOneMove = false;
        AllowedMoves = Chessmans[x, y].PossibleMove();
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                if(AllowedMoves[i,j])
                {
                    hasAtLeastOneMove = true;
                }
            }
        }
        if(!hasAtLeastOneMove)
        {
            return;
        }

        selectedChessman = Chessmans[x, y];
        previousMat = selectedChessman.GetComponent<MeshRenderer>().material;
        selectedMat.mainTexture = previousMat.mainTexture;
        selectedChessman.GetComponent<MeshRenderer>().material = selectedMat;
        BoardHighlights.Instance.HighlightAllowedMove(AllowedMoves);
    }

    private void MoveChessman(int x, int y)
    {
        if(AllowedMoves[x,y])
        {
            Chessman c = Chessmans[x, y];

            if(c != null && c.isWhite != isWhiteTurn)
            {
                if(c.GetType() == typeof(King))
                {
                    EndGame();
                    return;
                }

                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
            }

            if(x == enPassantMove[0] && y == enPassantMove [1])
            {
                if(isWhiteTurn)
                {
                    c = Chessmans[x, y -1];
                }
                else
                {
                    c = Chessmans[x, y + 1];
                }
                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
            }

            enPassantMove[0] = -1;
            enPassantMove[1] = -1;
            if(selectedChessman.GetType() == typeof(Pawn))
            {
                if(y == 7)
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(1, x, y);
                    selectedChessman = Chessmans[x, y];
                    selectedChessman.isWhite = true;
                }
                else if(y == 0)
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(7, x, y);
                    selectedChessman = Chessmans[x, y];
                    selectedChessman.isWhite = false;
                }

                if(selectedChessman.CurrentY == 1 && y == 3)
                {
                    enPassantMove[0] = x;
                    enPassantMove[1] = y -1;
                }
                else if(selectedChessman.CurrentY == 6 && y == 4)
                {
                    enPassantMove[0] = x;
                    enPassantMove[1] = y +1;
                }
            }

            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(x, y);
            selectedChessman.SetPosition(x, y);
            Chessmans[x, y] = selectedChessman;
            isWhiteTurn = !isWhiteTurn;
        }

        selectedChessman.GetComponent<MeshRenderer>().material = previousMat;
        BoardHighlights.Instance.HideHighlights();

        selectedChessman = null;
    }

    private void UpdateSelection()
    {
        if(!Camera.main)
            return;

        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, LayerMask.GetMask("ChessPlane")))
        {
            //Debug.Log(hit.point);
            selectionX = (int)hit.point.x;
            selectionY = (int)hit.point.z;
        }
        else
        {
            selectionX = -1;
            selectionY = -1;
        }
        
    }

    private void SpawnChessman(int index, int x, int y)
    {
        GameObject go = Instantiate(chessmanPrefabs[index], GetTileCenter(x,y), orientation) as GameObject;
        go.transform.SetParent(transform);
        Chessmans[x,y] = go.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);
        activeChessman.Add(go);
        Chessmans[x, y].Init();
    }

    private void SpawnAllChessMans()
    {
        activeChessman = new List<GameObject>();
        Chessmans = new Chessman[8, 8];
        enPassantMove = new int[2] { -1, -1 };

        //Black Team
        //King
        SpawnChessman(0, 3, 7);
        //Queen             
        SpawnChessman(1, 4, 7);
        //Rooks             
        SpawnChessman(2, 0, 7);
        SpawnChessman(2, 7, 7);
        //Bishop            
        SpawnChessman(3, 2, 7);
        SpawnChessman(3, 5, 7);
        //Knights           
        SpawnChessman(4, 1, 7);
        SpawnChessman(4, 6, 7);
        //Pawns
        for(int i = 0; i < 8; i++)
        {
            SpawnChessman(5, i, 6);
        }

        //White Team
        //King
        SpawnChessman(6, 4, 0);
        //Queen
        SpawnChessman(7, 3, 0);
        //Rooks
        SpawnChessman(8, 0, 0);
        SpawnChessman(8, 7, 0);
        //Bishop
        SpawnChessman(9, 2, 0);
        SpawnChessman(9, 5, 0);
        //Knights
        SpawnChessman(10, 1, 0);
        SpawnChessman(10, 6, 0);
        //Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(11, i, 1);
        }
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;
        return origin;
    }

    private void DrawChessboard()
    {
        Vector3 widthLine = Vector3.right * 8;
        Vector3 heightLine = Vector3.forward * 8;

        for(int i = 0; i <= 8; i++)
        {
            Vector3 start = Vector3.forward * i;
            Debug.DrawLine(start, start + widthLine);

            for(int j = 0; j <= 8; j++)
            {
                start = Vector3.right * j;
                Debug.DrawLine(start, start + heightLine);
            }
        }

        if(selectionX >= 0 && selectionY >= 0)
        {
            Debug.DrawLine(Vector3.forward * selectionY + Vector3.right * selectionX,
                           Vector3.forward * (selectionY + 1) + Vector3.right * (selectionX + 1));

            Debug.DrawLine(Vector3.forward * (selectionY + 1) + Vector3.right * selectionX,
                           Vector3.forward * selectionY + Vector3.right * (selectionX + 1));
        }
    }

    private void EndGame()
    {
        if(isWhiteTurn)
        {
            Debug.Log("White team wins");
        }
        else
        {
            Debug.Log("Black team wins");
        }
        foreach(GameObject go in activeChessman)
        {
            Destroy(go);

            isWhiteTurn = true;
            BoardHighlights.Instance.HideHighlights();
        }

        SpawnAllChessMans();
    }

}
