using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardHighlights : MonoBehaviour
{
    public static BoardHighlights Instance { set; get; }

    public GameObject highlightPrefab;
    private List<GameObject> highlights;

    private void Start()
    {
        Instance = this;
        highlights = new List<GameObject>();
    }

    private GameObject GetHighlightObject()
    {
        GameObject go = highlights.Find(g => !g.activeSelf);

        if(go == null)
        {
            go = Instantiate(highlightPrefab);
            highlights.Add(go);

        }
        return go;
    }

    public void HighlightAllowedMove(bool[,] moves)
    {
        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if(moves[i, j])
                {
                    GameObject go = GetHighlightObject();
                    go.SetActive(true);
                    go.transform.position = Chessboard.Instance.GetTileCenter(i, j);
                    go.transform.localScale = new Vector3(Chessboard.Instance.TILE_SIZE * 0.1f, 1f, Chessboard.Instance.TILE_SIZE * 0.1f);
                }
            }
        }
    }

    public void HideHighlights()
    {
        foreach (GameObject go in highlights)
            go.SetActive(false);
    }
}
