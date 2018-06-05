using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity.InputModule;


public class Chessboard : MonoBehaviour, IFocusable, IInputClickHandler
{
    public static Chessboard Instance { set; get; }
    private bool[,] AllowedMoves { set; get; }

    public Chessman[,] Chessmans { set; get; }
    private Chessman selectedChessman;

    //private const float TILE_SIZE = 1.0f;
    //private const float TILE_OFFSET = 0.5f;

    private int selectionX = -1;
    private int selectionY = -1;

    public List<GameObject> chessmanPrefabs;
    private List<GameObject> activeChessman;

    private Material previousMat;
    public Material selectedMat;

    public int[] enPassantMove { set; get; }

    private Quaternion orientation = Quaternion.Euler(0, 0, 0);

    public bool isWhiteTurn = true;

    public float TILE_SIZE;
    float TILE_OFFSET;

    public Transform GridStartPos;
    public Transform GridEndPos;

    private bool isFocused;

    RaycastHit hit;

    [SerializeField]
    private float TimerStartValue = 2.5f;
    private float timer;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        timer = TimerStartValue;

#region VERY COMPLEX CODE
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

#endregion

        TILE_SIZE = Vector3.Distance(GridStartPos.transform.position, GridEndPos.transform.position) / 8;
        TILE_OFFSET = Vector3.Distance(GridStartPos.transform.position, GridEndPos.transform.position) / 16;
        SpawnAllChessMans();
    }

    struct Tile
    {
        public Vector2 pos;
        public bool whiteIs;
    }

    private void Update()
    {
        //UpdateSelection();
        //DrawChessboard();

        if(!isWhiteTurn)
        {

            if (selectedChessman == null)
            {
                var blackPieces = activeChessman.Where(t => t.GetComponent<Chessman>().isWhite.Equals(false));
                Chessman cm = blackPieces.ElementAt((int)UnityEngine.Random.Range(0f, blackPieces.Count() - 1)).GetComponent<Chessman>();
                SelectChessman(cm.CurrentX, cm.CurrentY);
                //SelectChessman(x,y);
            }
            else
            {
                if (timer >= 0f)
                {
                    timer -= Time.deltaTime;
                    return;
                }
                timer = TimerStartValue;

                List<Tile> collection = new List<Tile>();
                List<Vector2> moves = new List<Vector2>();
                for (int i = 0; i < AllowedMoves.GetLength(0); i++)
                {
                    for (int j = 0; j < AllowedMoves.GetLength(1); j++)
                    {
                        if (AllowedMoves[i, j] == true)
                        {
                            Tile tileNew = new Tile();
                            tileNew.pos = new Vector2(i, j);
                            //tileNew.whiteIs = (Chessmans[i, j].isWhite) ? true : false;
                            if (Chessmans[i, j] != null && Chessmans[i, j].isWhite)
                            {
                                tileNew.whiteIs = Chessmans[i, j].isWhite;
                                collection.Add(tileNew);
                            }
                            else
                            {
                                moves.Add(new Vector2(i, j));
                            }
                        }
                    }
                }

                if(collection.Count > 0)
                {
                    Tile tile = collection[UnityEngine.Random.Range(0, collection.Count - 1)];
                    MoveChessman((int)tile.pos.x, (int)tile.pos.y);
                }
                else
                {
                    Vector2 newPos = moves[UnityEngine.Random.Range(0, moves.Count - 1)];
                    MoveChessman((int)newPos.x, (int)newPos.y);
                }

                //newMove = collection[(int)UnityEngine.Random.Range(0f, collection.Count - 1)];
                //MoveChessman((int)newMove.x, (int)newMove.y);

            }
        }

        if(isFocused == true)
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, LayerMask.GetMask("ChessPlane")))
            {
                //Debug.Log(hit.point);
                selectionX = (int)(hit.point.x / TILE_SIZE);
                selectionY = (int)(hit.point.z / TILE_SIZE);
                Debug.Log("x" + selectionX);
                Debug.Log("y" + selectionY);
            }
        }

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
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, LayerMask.GetMask("ChessPlane")))
            { 
                //Debug.Log(hit.point);
                selectionX = (int)(hit.point.x / TILE_SIZE);
                selectionY = (int)(hit.point.z / TILE_SIZE);
                Debug.Log("x" + selectionX);
                Debug.Log("y" + selectionY);
            }
            else
            {
                selectionX = -1;
                selectionY = -1;
            }
        }
    }

    //private float Map(float v, float s1, float ss1, float s2, float ss2)
    //{
    //    return (v - s1) / (ss1 - s1) * (ss2 - s1) + ss1;
    //}

    private void SpawnChessman(int index, int x, int y, bool isWhite = false)
    {
        GameObject go = Instantiate(chessmanPrefabs[index], GetTileCenter(x,y), orientation) as GameObject;
        go.transform.SetParent(transform);
        Chessmans[x, y] = go.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);
        Chessmans[x, y].isWhite = isWhite;
        activeChessman.Add(go);
        Chessmans[x, y].Init();
        go.transform.localScale = new Vector3(1, 1, 1);
    }

    public Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += GridStartPos.transform.position.x + (TILE_SIZE * x) + TILE_OFFSET;
        origin.y = GridStartPos.transform.position.y;
        origin.z += GridStartPos.transform.position.z + (TILE_SIZE * y) + TILE_OFFSET;
        return origin;
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
        SpawnChessman(6, 4, 0, true);
        //Queen
        SpawnChessman(7, 3, 0, true);
        //Rooks
        SpawnChessman(8, 0, 0, true);
        SpawnChessman(8, 7, 0, true);
        //Bishop
        SpawnChessman(9, 2, 0, true);
        SpawnChessman(9, 5, 0, true);
        //Knights
        SpawnChessman(10, 1, 0, true);
        SpawnChessman(10, 6, 0, true);
        //Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(11, i, 1, true);
        }
    }

    private void DrawChessboard()
    {

        //float width = Vector3.Distance(GridStartPos.transform.position, GridEndPos.transform.position) / 8;
        //float height = Vector3.Distance(GridStartPos.transform.position, GridEndPos.transform.position) / 8;
        //
        //Vector3 widthLine = GridStartPos.transform.position + new Vector3(height * 8, 0, 0);
        //Vector3 heightLine = GridStartPos.transform.position + new Vector3(0, 0, height * 8);
        //
        //for (int i = 0; i <= 8; i++)
        //{
        //    Vector3 start = GridStartPos.transform.position + new Vector3(0, 0, height * i);
        //    Debug.DrawLine(start, new Vector3(start.x + widthLine.x, start.y, start.z + widthLine.z));
        //
        //    for(int j = 0; j <= 8; j++)
        //    {
        //        start = GridStartPos.transform.position + new Vector3(width * j, 0, 0);
        //        Debug.DrawLine(start, new Vector3(start.x + heightLine.x, start.y, start.z + heightLine.z));
        //    }
        //}

        //Vector3 widthLine = Vector3.right * 8;
        //Vector3 heightLine = Vector3.forward * 8;
        //
        //for(int i = 0; i <= 8; i++)
        //{
        //    Vector3 start = Vector3.forward * i;
        //    Debug.DrawLine(start, start + widthLine);
        //
        //    for(int j = 0; j <= 8; j++)
        //    {
        //        start = Vector3.right * j;
        //        Debug.DrawLine(start, start + heightLine);
        //    }
        //}

        //if (selectionX >= 0 && selectionY >= 0)
        //{
        //    Debug.DrawLine(Vector3.forward * selectionY + Vector3.right * selectionX,
        //                   Vector3.forward * (selectionY + 1) + Vector3.right * (selectionX + 1));
        //
        //    Debug.DrawLine(Vector3.forward * (selectionY + 1) + Vector3.right * selectionX,
        //                   Vector3.forward * selectionY + Vector3.right * (selectionX + 1));
        //}
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

    public float ChessScale(float a_value)
    {
        return 0f;
    }

    public void OnFocusEnter()
    {
        isFocused = true;
    }

    public void OnFocusExit()
    {
        isFocused = false;
        selectionX = -1;
        selectionY = -1;
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("Tap tap");
        if (selectionX >= 0 && selectionY >= 0)
        {
            if (selectedChessman == null)
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
