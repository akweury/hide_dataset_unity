using System.Collections.Generic;
using UnityEngine;


public class CustomScenes
{
    private readonly List<ObjectStruct> _sceneData;
    private readonly float _objScale;

    private Vector3 _centerPoint;
    private readonly float _tableWidth;
    private const int MaxColorNum = 3;
    private const int MaxShapeNum = 3;

    public CustomScenes(List<ObjectStruct> sceneData, float objScale, Vector3 centerPoint, float tableWidth)
    {
        _sceneData = sceneData;
        _objScale = objScale;
        _centerPoint = centerPoint;
        _tableWidth = tableWidth;
    }

    public List<ObjectStruct> DiagScene(string positionType)
    {
        /* objects are placed in diagonal position. It has nothing to do with shape and color. */
        const int totalObjNum = 2;
        var shiftLength = _tableWidth / 4;

        // setup object shapes and colors
        var objShapes = new string[totalObjNum];
        var objColors = new string[totalObjNum];
        var objPosition = new Vector3[totalObjNum];
        var randomPositionIndex = Random.Range(0, 2);
        for (var i = 0; i < totalObjNum; i++)
        {
            var randomShapeIndex = Random.Range(0, MaxShapeNum);

            objShapes[i] = randomShapeIndex switch
            {
                0 => "cube",
                1 => "sphere",
                _ => "cylinder"
            };

            var randomColorIndex = Random.Range(0, MaxColorNum);
            objColors[i] = randomColorIndex switch
            {
                0 => "red",
                1 => "green",
                _ => "blue"
            };
        }

        // setup object positions
        objPosition[0] = randomPositionIndex switch
        {
            0 => new Vector3(_centerPoint[0] - shiftLength, _centerPoint[1], _centerPoint[2] - shiftLength), // upper left
            _ => new Vector3(_centerPoint[0] - shiftLength, _centerPoint[1], _centerPoint[2] + shiftLength), // lower left
        };

        if (positionType == "diag")
        {
            objPosition[1] = randomPositionIndex switch
            {
                0 => new Vector3(_centerPoint[0] + shiftLength, _centerPoint[1], _centerPoint[2] + shiftLength), // lower right
                _ => new Vector3(_centerPoint[0] + shiftLength, _centerPoint[1], _centerPoint[2] - shiftLength), // upper right
            };
        }
        else
        {
            objPosition[1] = randomPositionIndex switch
            {
                0 => new Vector3(_centerPoint[0] + shiftLength, _centerPoint[1], _centerPoint[2] - shiftLength), // upper right
                _ => new Vector3(_centerPoint[0] + shiftLength, _centerPoint[1], _centerPoint[2] + shiftLength), // lower right
            };
        }

        // add rule object
        for (var i = 0; i < totalObjNum; i++)
        {
            var objShape = objShapes[i];
            var objColor = objColors[i];
            _sceneData[i] = new ObjectStruct(i, objShape, objColor, _objScale)
            {
                Position = objPosition[i]
            };
        }

        return _sceneData;
    }
}