using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectGameController : MonoBehaviour
{
    public Transform blackBoardRoot;
    public Transform playerBoardRoot;

    public float intervalX = 0.1f;
    public float intervalY = 0.1f;

    public Transform blackDiskPrefab;
    public Transform[] playerDiskPrefabs;

    private Network.Game game;

    private Transform[] blackDisks;

    private void Start()
    {
        game = Network.instance.playerGame;

        CreateBoardSprites();
    }

    private void CreateBoardSprites()
    {
        blackDisks = new Transform[game.cols * game.rows];

        var width = intervalX * (game.cols - 1);
        var offsetX = - width * .5f;

        for (int r = 0; r < game.rows; ++r)
        {
            float y = r * intervalY;

            for (int c = 0; c < game.cols; ++c)
            {
                float x = offsetX + c * intervalY;

                var black = Instantiate(blackDiskPrefab, blackBoardRoot);
                black.localPosition = new Vector3(x, y);
                SetBlackDisk(r, c, black);
            }
        }
    }

    private void SetBlackDisk(int r, int c, Transform disk)
    {
        blackDisks[r * game.cols + c] = disk;
    }

    private Transform GetBlackDisk(int r, int c) => blackDisks[r * game.cols + c];

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryDrop();
        }
    }

    private void TryDrop()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var diskPos = GetDiskAt(ray);
        if (diskPos != null)
        {
            print($"{diskPos.Item1}, {diskPos.Item2}");
        }
    }

    private Tuple<int,int> GetDiskAt(Ray ray)
    {
        var hit = Physics2D.GetRayIntersection(ray);
        if (hit.transform == null)
            return null;
        int index = Array.IndexOf(blackDisks, hit.transform);
        return new Tuple<int, int>(index / game.cols, index % game.cols);
    }
}
