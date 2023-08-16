using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Random = System.Random;


public static class SceneUtils
{
    public static string[] RandomShapes(int totalObjNum, int maxShapeNum)
    {
        var objShapes = new string[totalObjNum];
        for (var i = 0; i < totalObjNum; i++)
        {
            var randomShapeIndex = UnityEngine.Random.Range(0, maxShapeNum);

            objShapes[i] = randomShapeIndex switch
            {
                0 => "cube",
                1 => "sphere",
                _ => "cylinder"
            };
        }

        return objShapes;
    }

    public static string[] GivenShapes(string[] shapes, int maxShapeNum)
    {
        var objShapes = new string[shapes.Length];
        for (var i = 0; i < shapes.Length; i++)
        {
            if (shapes[i] == "random")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxShapeNum);
                objShapes[i] = randomShapeIndex switch
                {
                    0 => "cube",
                    1 => "sphere",
                    _ => "cylinder"
                };
            }
            else if (shapes[i] == "no_sphere")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxShapeNum - 1);
                objShapes[i] = randomShapeIndex switch
                {
                    0 => "cube",
                    _ => "cylinder"
                };
            }
            else if (shapes[i] == "no_cube")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxShapeNum - 1);
                objShapes[i] = randomShapeIndex switch
                {
                    0 => "sphere",
                    _ => "cylinder"
                };
            }
            else if (shapes[i] == "no_cylinder")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxShapeNum - 1);
                objShapes[i] = randomShapeIndex switch
                {
                    0 => "cube",
                    _ => "sphere"
                };
            }
            else
            {
                objShapes[i] = shapes[i];
            }
        }

        return objShapes;
    }


    public static string[] RandomColors(int totalObjNum, int maxColorNum)
    {
        var objColors = new string[totalObjNum];
        for (var i = 0; i < totalObjNum; i++)
        {
            var randomColorIndex = UnityEngine.Random.Range(0, maxColorNum);
            objColors[i] = randomColorIndex switch
            {
                0 => "red",
                1 => "green",
                _ => "blue"
            };
        }

        return objColors;
    }

    public static string[] GivenColors(string[] colors, int maxColorNum)
    {
        var objColors = new string[colors.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            if (colors[i] == "random")
            {
                var randomColorIndex = UnityEngine.Random.Range(0, maxColorNum);
                objColors[i] = randomColorIndex switch
                {
                    0 => "red",
                    1 => "green",
                    _ => "blue"
                };
            }
            else if (colors[i] == "no_red")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxColorNum - 1);
                objColors[i] = randomShapeIndex switch
                {
                    0 => "green",
                    _ => "blue"
                };
            }
            else if (colors[i] == "no_green")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxColorNum - 1);
                objColors[i] = randomShapeIndex switch
                {
                    0 => "red",
                    _ => "blue"
                };
            }
            else if (colors[i] == "no_blue")
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, maxColorNum - 1);
                objColors[i] = randomShapeIndex switch
                {
                    0 => "red",
                    _ => "green"
                };
            }
            else
            {
                objColors[i] = colors[i];
            }
        }

        return objColors;
    }

    public static Vector3[] RandomPositions(Vector3 center, float objRadius, float tableWidth, int totalObjNum)
    {
        var objPositions = new Vector3[totalObjNum];
        var numTries = 0;
        const float minObjDist = 0.2f;
        const int maxNumTry = 50;
        float shiftMax = tableWidth / 3f;

        // find a 3D position for the new object

        for (var i = 0; i < totalObjNum; i++)
        {
            var validDist = true;
            do
            {
                numTries += 1;
                // exceed the maximum trying time
                if (numTries > maxNumTry) throw new IndexOutOfRangeException();
                // choose new position
                objPositions[i][0] = center[0] + UnityEngine.Random.Range(-shiftMax + objRadius, shiftMax - objRadius);
                objPositions[i][1] = center[1] + objRadius;
                objPositions[i][2] = center[2] + UnityEngine.Random.Range(-shiftMax + objRadius, shiftMax - objRadius);

                // check for overlapping
                for (var j = 0; j < i; j++)
                {
                    float dx = objPositions[j][0] - objPositions[i][0];
                    float dz = objPositions[j][2] - objPositions[i][2];
                    float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (dist - 2 * objRadius < minObjDist)
                    {
                        validDist = false;
                    }
                }
            } while (!validDist);
        }

        return objPositions;
    }

    public static List<ObjectStruct> IntegrateData(List<ObjectStruct> sceneData, string[] objShapes, string[] objColors,
        Vector3[] objPosition, int totalObjNum, float objScale)
    {
        for (var i = 0; i < totalObjNum; i++)
        {
            var objShape = objShapes[i];
            var objColor = objColors[i];
            sceneData[i] = new ObjectStruct(i, objShape, objColor, objScale)
            {
                Position = objPosition[i]
            };
        }

        return sceneData;
    }

    public static Vector3[] RotatePositions(float rotateScale, Vector3 rotateCenter, Vector3[] points)
    {
        // rotate a random degree
        var rotateRadians = UnityEngine.Random.Range(0, rotateScale); // 0.17 radians == 10 degrees
        var alpha = rotateCenter.x;
        var beta = rotateCenter.z;
        for (var i = 0; i < points.Length; i++)
        {
            points[i].x = alpha + Mathf.Cos(rotateRadians) * (points[i].x - alpha) -
                          Mathf.Sin(rotateRadians) * (points[i].z - beta);
            points[i].z = beta + Mathf.Sin(rotateRadians) * (points[i].x - alpha) +
                          Mathf.Cos(rotateRadians) * (points[i].z - beta);
        }

        return points;
    }
}

public class CustomScenes
{
    private List<ObjectStruct> _sceneData;
    private readonly float _objScale;

    private Vector3 _centerPoint;
    private readonly float _tableWidth;
    private const int MaxColorNum = 3;
    private const int MaxShapeNum = 3;
    private readonly float _unifyScale;
    private readonly float _objRadius;

    public CustomScenes(List<ObjectStruct> sceneData, float objScale, Vector3 centerPoint, float tableWidth,
        float unifyScale)
    {
        _sceneData = sceneData;
        _objScale = objScale;
        _centerPoint = centerPoint;
        _tableWidth = tableWidth;
        _unifyScale = unifyScale;
        _objRadius = _objScale * _unifyScale;
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
        var randomPositionIndex = UnityEngine.Random.Range(0, 2);
        for (var i = 0; i < totalObjNum; i++)
        {
            var randomShapeIndex = UnityEngine.Random.Range(0, MaxShapeNum);

            objShapes[i] = randomShapeIndex switch
            {
                0 => "cube",
                1 => "sphere",
                _ => "cylinder"
            };

            var randomColorIndex = UnityEngine.Random.Range(0, MaxColorNum);
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
            0 => new Vector3(_centerPoint[0] - shiftLength,
                _centerPoint[1] + _objScale * _unifyScale,
                _centerPoint[2] - shiftLength), // upper left
            _ => new Vector3(_centerPoint[0] - shiftLength,
                _centerPoint[1] + _objScale * _unifyScale,
                _centerPoint[2] + shiftLength), // lower left
        };

        if (positionType == "diag")
        {
            objPosition[1] = randomPositionIndex switch
            {
                0 => new Vector3(_centerPoint[0] + shiftLength,
                    _centerPoint[1] + _objScale * _unifyScale,
                    _centerPoint[2] + shiftLength), // lower right
                _ => new Vector3(_centerPoint[0] + shiftLength,
                    _centerPoint[1] + _objScale * _unifyScale,
                    _centerPoint[2] - shiftLength), // upper right
            };
        }
        else
        {
            objPosition[1] = randomPositionIndex switch
            {
                0 => new Vector3(_centerPoint[0] + shiftLength,
                    _centerPoint[1] + _objScale * _unifyScale,
                    _centerPoint[2] - shiftLength), // upper right
                _ => new Vector3(_centerPoint[0] + shiftLength,
                    _centerPoint[1] + _objScale * _unifyScale,
                    _centerPoint[2] + shiftLength), // lower right
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

    public List<ObjectStruct> CloseScene(string positionType)
    {
        /* objects are placed close with each other. It has nothing to do with shape and color. */
        const int totalObjNum = 2;
        var shiftCenter = _tableWidth / 6;
        var shiftMax = _tableWidth / 2.1f;
        var shiftFarMin = _tableWidth / 2.2f;

        var shiftCloseMax = _tableWidth / 6.9f;
        var shiftMin = _tableWidth / 7;

        // setup object shapes and colors
        var objShapes = SceneUtils.RandomShapes(totalObjNum, MaxShapeNum);
        var objColors = SceneUtils.RandomColors(totalObjNum, MaxColorNum);
        // setup object positions
        var objPosition = new Vector3[totalObjNum];
        objPosition[0] = new Vector3(
            _centerPoint[0] + UnityEngine.Random.Range(-shiftCenter + _objScale, shiftCenter - _objScale),
            _centerPoint[1] + _objScale * _unifyScale,
            _centerPoint[2] + UnityEngine.Random.Range(-shiftCenter + _objScale, shiftCenter - _objScale));

        if (positionType == "close")
        {
            float dist;
            do
            {
                objPosition[1] = new Vector3(
                    objPosition[0][0] + UnityEngine.Random.Range(-shiftCloseMax + _objScale, shiftCloseMax + _objScale),
                    objPosition[0][1] + _objScale * _unifyScale,
                    objPosition[0][2] +
                    UnityEngine.Random.Range(-shiftCloseMax + _objScale, shiftCloseMax + _objScale));
                var dx = objPosition[0][0] - objPosition[1][0];
                var dz = objPosition[0][2] - objPosition[1][2];
                dist = (float)Math.Sqrt(dx * dx + dz * dz);
            } while (!(dist < shiftCloseMax && dist > shiftMin));
        }
        else
        {
            float dist;
            do
            {
                objPosition[1] = new Vector3(
                    objPosition[0][0] + UnityEngine.Random.Range(-shiftMax + _objScale, shiftMax + _objScale),
                    objPosition[0][1] + _objScale * _unifyScale,
                    objPosition[0][2] + UnityEngine.Random.Range(-shiftMax + _objScale, shiftMax + _objScale));
                var dx = objPosition[0][0] - objPosition[1][0];
                var dz = objPosition[0][2] - objPosition[1][2];
                dist = (float)Math.Sqrt(dx * dx + dz * dz);
            } while (!(dist < shiftMax && dist > shiftFarMin));
        }

        // add rule object
        _sceneData = SceneUtils.IntegrateData(_sceneData, objShapes, objColors, objPosition, totalObjNum, _objScale);
        return _sceneData;
    }

    public List<ObjectStruct> ExistScene(string positionType)
    {
        /* There are always a red cube and a random color sphere in the image. In addition, there are two other objects.*/
        const int totalObjNum = 4;

        string[] objShapes;
        string[] objColors;
        Vector3[] objPositions;
        // setup objects
        if (positionType == "red_cube_and_random_sphere")
        {
            var shapes = new[] { "cube", "sphere", "random", "random" };
            var colors = new[] { "red", "random", "random", "random" };
            objShapes = SceneUtils.GivenShapes(shapes, MaxShapeNum);
            objColors = SceneUtils.GivenColors(colors, MaxColorNum);
            objPositions = SceneUtils.RandomPositions(_centerPoint, _objRadius, _tableWidth, totalObjNum);
        }
        else if (positionType == "red_cube_and_no_sphere")
        {
            var shapes = new[] { "cube", "no_sphere", "no_sphere", "no_sphere" }; // TODO: change the given data.
            var colors = new[] { "red", "random", "random", "random" };
            objShapes = SceneUtils.GivenShapes(shapes, MaxShapeNum);
            objColors = SceneUtils.GivenColors(colors, MaxColorNum);
            objPositions = SceneUtils.RandomPositions(_centerPoint, _objRadius, _tableWidth, totalObjNum);
        }
        else if (positionType == "no_red_cube_and_random_sphere")
        {
            var shapes = new[] { "no_cube", "sphere", "cube", "no_cube" }; // TODO: change the given data.
            var colors = new[] { "red", "random", "no_red", "no_red" };
            objShapes = SceneUtils.GivenShapes(shapes, MaxShapeNum);
            objColors = SceneUtils.GivenColors(colors, MaxColorNum);
            objPositions = SceneUtils.RandomPositions(_centerPoint, _objRadius, _tableWidth, totalObjNum);
        }
        else if (positionType == "no_red_cube_and_no_sphere")
        {
            var shapes = new[] { "no_sphere", "no_sphere", "no_sphere", "no_sphere" }; // TODO: change the given data.
            var colors = new[] { "no_red", "no_red", "no_red", "no_red" };
            objShapes = SceneUtils.GivenShapes(shapes, MaxShapeNum);
            objColors = SceneUtils.GivenColors(colors, MaxColorNum);
            objPositions = SceneUtils.RandomPositions(_centerPoint, _objRadius, _tableWidth, totalObjNum);
        }
        else
        {
            throw new NotImplementedException();
        }

        // for cubes, adjust its radius
        // if (sceneData[objIdx].Shape == "cube") objRadius *= (float)Math.Sqrt(2);
        // add rule object
        _sceneData = SceneUtils.IntegrateData(_sceneData, objShapes, objColors, objPositions, totalObjNum, _objScale);
        return _sceneData;
    }


    public List<ObjectStruct> CrossScene(string positionType)
    {
        /* There are always a red cube and a random color sphere in the image. In addition, there are two other objects.*/
        const int totalClusters = 3;
        const int totalObjNum = 5;
        var objShapes = new string[totalObjNum * totalClusters];
        var objColors = new string[totalObjNum * totalClusters];
        var objPositions = new Vector3[totalObjNum * totalClusters];
        // setup object colors and shapes 
        for (var clusterIndex = 0; clusterIndex < totalClusters; clusterIndex++)
        {
            for (var objIndex = 0; objIndex < totalObjNum; objIndex++)
            {
                var randomShapeIndex = UnityEngine.Random.Range(0, MaxShapeNum);

                objShapes[clusterIndex * totalObjNum + objIndex] = randomShapeIndex switch
                {
                    0 => "cube",
                    1 => "sphere",
                    _ => "cylinder"
                };
                var randomColorIndex = UnityEngine.Random.Range(0, MaxColorNum);
                objColors[clusterIndex * totalObjNum + objIndex] = randomColorIndex switch
                {
                    0 => "red",
                    1 => "green",
                    _ => "blue"
                };
            }
        }

        // setup object positions
        float clusterRadius = _objRadius * 5;
        float distScale = 0.5f;
        // rotate a random degree
        Vector3[] clusterCenters = SceneUtils.RandomPositions(_centerPoint, clusterRadius, _tableWidth, totalClusters);

        for (var clusterIndex = 0; clusterIndex < totalClusters; clusterIndex++)
        {
            Vector3[] clusterPositions = new Vector3[totalObjNum];
            clusterPositions[0] = clusterCenters[clusterIndex] + new Vector3(0, 0, 0);
            clusterPositions[1] = clusterCenters[clusterIndex] + new Vector3(distScale, 0, 0);
            clusterPositions[2] = clusterCenters[clusterIndex] + new Vector3(0, 0, distScale);
            clusterPositions[3] = clusterCenters[clusterIndex] + new Vector3(-distScale, 0, 0);
            clusterPositions[4] = clusterCenters[clusterIndex] + new Vector3(0, 0, -distScale);
            // clusterPositions = SceneUtils.RotatePositions(1f, clusterCenters[clusterIndex], clusterPositions);

            int clusterBaseIndex = clusterIndex * totalObjNum;
            objPositions[clusterBaseIndex + 0] = clusterPositions[0];
            objPositions[clusterBaseIndex + 1] = clusterPositions[1];
            objPositions[clusterBaseIndex + 2] = clusterPositions[2];
            objPositions[clusterBaseIndex + 3] = clusterPositions[3];
            objPositions[clusterBaseIndex + 4] = clusterPositions[4];
        }


        // for cubes, adjust its radius
        // if (sceneData[objIdx].Shape == "cube") objRadius *= (float)Math.Sqrt(2);
        // add rule object
        _sceneData = SceneUtils.IntegrateData(_sceneData, objShapes, objColors, objPositions,
            totalClusters * totalObjNum, _objScale);
        return _sceneData;
    }
}