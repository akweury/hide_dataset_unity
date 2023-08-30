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

    public static Vector3 ShiftRandomPosition(Vector3 pos, float objScale, float shiftXLeft, float shiftXRight, float shiftZUp, float shiftZDown)
    {
        pos = new Vector3(pos[0] + UnityEngine.Random.Range(shiftXLeft, shiftXRight),
            pos[1] + objScale,
            pos[2] + UnityEngine.Random.Range(shiftZUp, shiftZDown)
        );
        return pos;
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
        public int objTotalNum;

        public CustomScenes(float objScale, Vector3 centerPoint, float tableWidth,
            float unifyScale)
        {
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

        public List<ObjectStruct> TwoPairsScene(string positionType)
        {
            /* There are always 4 objects in the image. In addition, they are two pairs of objects.*/
            objTotalNum = 4;
            const int totalObjNum = 4;
            _sceneData = new List<ObjectStruct>(new ObjectStruct[totalObjNum]);

            string[] objShapes = new string[totalObjNum];
            string[] objColors = new string[totalObjNum];
            Vector3[] objPositions = new Vector3[totalObjNum];
            var colors = new[] { "red", "green", "blue" };
            var shapes = new[] { "cube", "sphere", "cylinder" };
            // setup objects
            if (positionType == "two_pairs")
            {
                // chose object shapes and colors
                bool sameShape;
                bool sameColor;
                int shape1;
                int shape2;
                int color1;
                int color2;
                do
                {
                    shape1 = UnityEngine.Random.Range(0, MaxShapeNum);
                    color1 = UnityEngine.Random.Range(0, MaxColorNum);

                    shape2 = UnityEngine.Random.Range(0, MaxShapeNum);
                    color2 = UnityEngine.Random.Range(0, MaxColorNum);

                    sameShape = shape1 == shape2;
                    sameColor = color1 == color2;
                } while (sameShape || sameColor);

                objShapes[0] = shapes[shape1];
                objShapes[1] = shapes[shape1];
                objShapes[2] = shapes[shape2];
                objShapes[3] = shapes[shape2];

                objColors[0] = colors[color1];
                objColors[1] = colors[color1];
                objColors[2] = colors[color2];
                objColors[3] = colors[color2];

                objPositions = SceneUtils.RandomPositions(_centerPoint, _objRadius, _tableWidth, totalObjNum);
            }
            else
            {
                // chose object shapes and colors
                int[] shapeIDs = new int[4];
                int[] colorIDs = new int[4];
                int uniqueShapes;
                int uniqueColors;
                do
                {
                    shapeIDs[0] = UnityEngine.Random.Range(0, MaxShapeNum);
                    shapeIDs[1] = UnityEngine.Random.Range(0, MaxShapeNum);
                    shapeIDs[2] = UnityEngine.Random.Range(0, MaxShapeNum);
                    shapeIDs[3] = UnityEngine.Random.Range(0, MaxShapeNum);

                    colorIDs[0] = UnityEngine.Random.Range(0, MaxColorNum);
                    colorIDs[1] = UnityEngine.Random.Range(0, MaxColorNum);
                    colorIDs[2] = UnityEngine.Random.Range(0, MaxColorNum);
                    colorIDs[3] = UnityEngine.Random.Range(0, MaxColorNum);
                    uniqueShapes = shapeIDs.Distinct<int>().Count();
                    uniqueColors = colorIDs.Distinct<int>().Count();
                } while (uniqueShapes == 2 && uniqueColors == 2);

                objShapes[0] = shapes[shapeIDs[0]];
                objShapes[1] = shapes[shapeIDs[1]];
                objShapes[2] = shapes[shapeIDs[2]];
                objShapes[3] = shapes[shapeIDs[3]];

                objColors[0] = colors[colorIDs[0]];
                objColors[1] = colors[colorIDs[1]];
                objColors[2] = colors[colorIDs[2]];
                objColors[3] = colors[colorIDs[3]];

                objPositions = RandomPositions(_centerPoint, _objRadius, _tableWidth, totalObjNum);
            }

            // for cubes, adjust its radius
            // if (sceneData[objIdx].Shape == "cube") objRadius *= (float)Math.Sqrt(2);
            // add rule object
            _sceneData = SceneUtils.IntegrateData(_sceneData, objShapes, objColors, objPositions, totalObjNum, _objScale);
            return _sceneData;
        }


        public List<ObjectStruct> SquareScene(string positionType)
        {
            string[] objShapes;
            string[] objColors;
            Vector3[] objPositions;
            if (positionType == "square")
            {
                /* There are always a square with side length from 3 to 5. */
                int sideSize = UnityEngine.Random.Range(3, 6);
                objTotalNum = sideSize * 4 - 4;
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = new string[objTotalNum];
                objColors = new string[objTotalNum];

                // setup object colors and shapes
                var randomShapeIndex = UnityEngine.Random.Range(0, MaxShapeNum);

                for (var objIndex = 0; objIndex < objTotalNum; objIndex++)
                {
                    objShapes[objIndex] = randomShapeIndex switch
                    {
                        0 => "cube",
                        1 => "sphere",
                        _ => "cylinder"
                    };
                    var randomColorIndex = UnityEngine.Random.Range(0, MaxColorNum);
                    objColors[objIndex] = randomColorIndex switch
                    {
                        0 => "red",
                        1 => "green",
                        _ => "blue"
                    };
                }

                // setup object positions
                float distScale = 1.1f;
                // rotate a random degree
                // Vector3[] clusterCenters = SceneUtils.RandomPositions(_centerPoint, clusterRadius, _tableWidth, totalClusters);
                float shiftScale = 3f;
                objPositions = new Vector3[objTotalNum];
                objPositions[0] = new Vector3(
                    _centerPoint[0] + UnityEngine.Random.Range(-shiftScale + _objScale, -1.3f),
                    _centerPoint[1] + _objScale * _unifyScale,
                    _centerPoint[2] + UnityEngine.Random.Range(1.3f, shiftScale - _objScale));

                for (var objIndex = 0; objIndex < sideSize - 1; objIndex++)
                {
                    // upper horizontal side
                    objPositions[1 + objIndex] = objPositions[0] + new Vector3(distScale * (objIndex + 1), 0, 0);
                    // left vertical side
                    objPositions[objIndex + sideSize - 1 + 1] =
                        objPositions[0] + new Vector3(0, 0, -distScale * (objIndex + 1));
                    // right vertical side
                    objPositions[objIndex + 2 * sideSize - 2 + 1] =
                        objPositions[0] + new Vector3(distScale * (sideSize - 1), 0, -distScale * (objIndex + 1));
                    // lower horizontal side
                    if (objIndex != 0)
                    {
                        objPositions[3 * sideSize - 3 + 1 + objIndex - 1] =
                            objPositions[0] + new Vector3(distScale * (objIndex), 0, -distScale * (sideSize - 1));
                    }
                }
            }
            else
            {
                /* There are always a square with side length from 3 to 5. */
                var sideType = UnityEngine.Random.Range(0, 2);
                int sideLengthA;
                int sideLengthB;
                if (sideType == 0)
                {
                    sideLengthA = UnityEngine.Random.Range(3, 4);
                    sideLengthB = UnityEngine.Random.Range(5, 6);
                }
                else
                {
                    sideLengthB = UnityEngine.Random.Range(3, 4);
                    sideLengthA = UnityEngine.Random.Range(5, 6);
                }

                objTotalNum = sideLengthA * 2 + sideLengthB * 2 - 4;
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = new string[objTotalNum];
                objColors = new string[objTotalNum];

                var randomShapeIndex = UnityEngine.Random.Range(0, MaxShapeNum);

                // setup object colors and shapes 
                for (var objIndex = 0; objIndex < objTotalNum; objIndex++)
                {
                    objShapes[objIndex] = randomShapeIndex switch
                    {
                        0 => "cube",
                        1 => "sphere",
                        _ => "cylinder"
                    };
                    var randomColorIndex = UnityEngine.Random.Range(0, MaxColorNum);
                    objColors[objIndex] = randomColorIndex switch
                    {
                        0 => "red",
                        1 => "green",
                        _ => "blue"
                    };
                }

                // setup object positions
                float distScale = 1.1f;
                // rotate a random degree
                // Vector3[] clusterCenters = SceneUtils.RandomPositions(_centerPoint, clusterRadius, _tableWidth, totalClusters);
                float shiftScale = 3f;
                objPositions = new Vector3[objTotalNum];
                objPositions[0] = new Vector3(
                    _centerPoint[0] + UnityEngine.Random.Range(-shiftScale + _objScale, -1.3f),
                    _centerPoint[1] + _objScale * _unifyScale,
                    _centerPoint[2] + UnityEngine.Random.Range(1.3f, shiftScale - _objScale));

                for (var objIndex = 0; objIndex < sideLengthA - 1; objIndex++)
                {
                    // upper horizontal side
                    objPositions[1 + objIndex] = objPositions[0] + new Vector3(distScale * (objIndex + 1), 0, 0);
                    // lower horizontal side
                    if (objIndex != 0)
                    {
                        objPositions[2 * sideLengthB + sideLengthA - 3 + 1 + objIndex - 1] =
                            objPositions[0] + new Vector3(distScale * (objIndex), 0, -distScale * (sideLengthB - 1));
                    }
                }

                for (var objIndex = 0; objIndex < sideLengthB - 1; objIndex++)
                {
                    // left vertical side
                    objPositions[objIndex + sideLengthA - 1 + 1] =
                        objPositions[0] + new Vector3(0, 0, -distScale * (objIndex + 1));
                    // right vertical side
                    objPositions[objIndex + sideLengthA + sideLengthB - 2 + 1] =
                        objPositions[0] + new Vector3(distScale * (sideLengthA - 1), 0, -distScale * (objIndex + 1));
                }
            }


            // for cubes, adjust its radius
            // if (sceneData[objIdx].Shape == "cube") objRadius *= (float)Math.Sqrt(2);
            // add rule object
            _sceneData = SceneUtils.IntegrateData(_sceneData, objShapes, objColors, objPositions, objTotalNum, _objScale);
            return _sceneData;
        }

        public List<ObjectStruct> ParallelScene(string positionType)
        {
            /* There are always two lines in the image.
             Positive: Line A and Line B are parallel. The variations are the distance (x, y), slope, length.*/

            string[] objShapes;
            string[] objColors;
            Vector3[] objPositions;
            const float distScale = 1.2f;
            if (positionType == "perpendicular")
            {
                /* The length of Lines are between 3 to 5. */
                int lineSizeA = UnityEngine.Random.Range(3, 5);
                int lineSizeB = UnityEngine.Random.Range(3, 5);
                objTotalNum = lineSizeA + lineSizeB;

                /* The shapes and colors are randomly selected. */
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = SceneUtils.RandomShapes(objTotalNum, 3);
                objColors = SceneUtils.RandomColors(objTotalNum, 3);

                /* Store data to two lists, and concatenate them later. */
                Vector3[] objPosLineA = new Vector3[lineSizeA];
                Vector3[] objPosLineB = new Vector3[lineSizeB];

                // rotate a random degree
                var slopeAngle = UnityEngine.Random.Range(-0.7f, 0.7f);
                Vector3 shiftVector = new Vector3(Mathf.Cos(slopeAngle) * distScale, 0, Mathf.Sin(slopeAngle) * distScale);

                /* Define Line A. */
                objPosLineA[0] = ShiftRandomPosition(_centerPoint, _objScale, -2.3f, -1.5f, 1.3f, 1f);
                for (var objIndex = 1; objIndex < lineSizeA; objIndex++)
                {
                    objPosLineA[objIndex] = objPosLineA[0] + shiftVector * objIndex;
                }

                /* Define Line B. */
                objPosLineB[0] = ShiftRandomPosition(objPosLineA[0], _objScale, 0f, 0.5f, -2.5f, -3f);
                for (var objIndex = 1; objIndex < lineSizeB; objIndex++)
                {
                    objPosLineB[objIndex] = objPosLineB[0] + shiftVector * objIndex;
                }

                objPositions = objPosLineA.Concat(objPosLineB).ToArray();
            }
            else
            {
                /* The length of Lines are between 3 to 5. */
                int lineSizeA = UnityEngine.Random.Range(3, 5);
                int lineSizeB = UnityEngine.Random.Range(3, 5);
                objTotalNum = lineSizeA + lineSizeB;

                /* The shapes and colors are randomly selected. */
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = RandomShapes(objTotalNum, 3);
                objColors = RandomColors(objTotalNum, 3);

                /* Store data to two lists, and concatenate them later. */
                Vector3[] objPosLineA = new Vector3[lineSizeA];
                Vector3[] objPosLineB = new Vector3[lineSizeB];

                // rotate a random degree
                float slopeAngleA;
                float slopeAngleB;
                do
                {
                    slopeAngleA = UnityEngine.Random.Range(-0.7f, 0.7f);
                    slopeAngleB = UnityEngine.Random.Range(-0.7f, 0.7f);
                } while (Mathf.Abs(slopeAngleA - slopeAngleB) > 1.4f || Mathf.Abs(slopeAngleA - slopeAngleB) < 0.3f); // 0.78 == 45 degrees difference

                Vector3 shiftVectorA = new Vector3(Mathf.Cos(slopeAngleA) * distScale, 0, Mathf.Sin(slopeAngleA) * distScale);
                Vector3 shiftVectorB = new Vector3(Mathf.Cos(slopeAngleB) * distScale, 0, Mathf.Sin(slopeAngleB) * distScale);

                /* Define Line A. */
                objPosLineA[0] = ShiftRandomPosition(_centerPoint, _objScale, -2.3f, -1.5f, 1.3f, 1f);
                for (var objIndex = 1; objIndex < lineSizeA; objIndex++)
                {
                    objPosLineA[objIndex] = objPosLineA[0] + shiftVectorA * objIndex;
                }

                /* Define Line B. */
                objPosLineB[0] = ShiftRandomPosition(objPosLineA[0], _objScale, 0f, 0.7f, -2.5f, -3f);
                for (var objIndex = 1; objIndex < lineSizeB; objIndex++)
                {
                    objPosLineB[objIndex] = objPosLineB[0] + shiftVectorB * objIndex;
                }

                objPositions = objPosLineA.Concat(objPosLineB).ToArray();
            }

            // add rule object
            _sceneData = IntegrateData(_sceneData, objShapes, objColors, objPositions, objTotalNum, _objScale);
            return _sceneData;
        }

        public List<ObjectStruct> PerpendicularScene(string positionType)
        {
            /* There are always two lines in the image.
            Positive: Line A and Line B are perpendicular. The variations are the slope and length.*/

            string[] objShapes;
            string[] objColors;
            Vector3[] objPositions;
            const float distScale = 1.2f;
            if (positionType == "perpendicular")
            {
                /* Store data to two lists, and concatenate them later. */
                /* The length of Lines are between 3 to 5. */
                int lineSizeA = UnityEngine.Random.Range(3, 5);
                int lineSizeB = UnityEngine.Random.Range(3, 5);
                objTotalNum = lineSizeA + lineSizeB;

                /* The shapes and colors are randomly selected. */
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = RandomShapes(objTotalNum, 3);
                objColors = RandomColors(objTotalNum, 3);


                Vector3[] objPosLineA = new Vector3[lineSizeA];
                Vector3[] objPosLineB = new Vector3[lineSizeB];

                // rotate a random degree
                var slopeAngle = UnityEngine.Random.Range(-0.7f, 0.7f);
                Vector3 shiftVectorA = new Vector3(Mathf.Cos(slopeAngle) * distScale, 0, Mathf.Sin(slopeAngle) * distScale);
                Vector3 shiftVectorB;
                if (slopeAngle > 0)
                {
                    shiftVectorB =
                        new Vector3(Mathf.Cos(Mathf.PI / 2 - slopeAngle) * distScale, 0, -Mathf.Sin(Mathf.PI / 2 - slopeAngle) * distScale);
                }
                else
                {
                    shiftVectorB =
                        new Vector3(Mathf.Cos(Mathf.PI / 2 + slopeAngle) * distScale, 0, Mathf.Sin(Mathf.PI / 2 + slopeAngle) * distScale);
                }


                /* Define Line A. */
                objPosLineA[0] = ShiftRandomPosition(_centerPoint, _objScale, -2.3f, -1.5f, 1.3f, 1f);
                for (var objIndex = 1; objIndex < lineSizeA; objIndex++)
                {
                    objPosLineA[objIndex] = objPosLineA[0] + shiftVectorA * objIndex;
                }

                /* Define Line B. */
                objPosLineB[0] = objPosLineA[UnityEngine.Random.Range(0, lineSizeA)] - shiftVectorB;
                // objPosLineB[0] = ShiftRandomPosition(objPosLineA[0], _objScale, 0f, 0.5f, -2.5f, -3f);
                for (var objIndex = 1; objIndex < lineSizeB; objIndex++)
                {
                    objPosLineB[objIndex] = objPosLineB[0] + shiftVectorB * (objIndex + 1);
                }

                objPositions = objPosLineA.Concat(objPosLineB).ToArray();
            }
            else
            {
                /* The length of Lines are between 3 to 5. */
                int lineSizeA = UnityEngine.Random.Range(3, 5);
                int lineSizeB = UnityEngine.Random.Range(3, 5);
                objTotalNum = lineSizeA + lineSizeB;

                /* The shapes and colors are randomly selected. */
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = RandomShapes(objTotalNum, 3);
                objColors = RandomColors(objTotalNum, 3);

                /* Store data to two lists, and concatenate them later. */
                Vector3[] objPosLineA = new Vector3[lineSizeA];
                Vector3[] objPosLineB = new Vector3[lineSizeB];

                // rotate a random degree
                float slopeAngleA;
                float slopeAngleB;
                do
                {
                    slopeAngleA = UnityEngine.Random.Range(-0.7f, 0.7f);
                    slopeAngleB = UnityEngine.Random.Range(-0.7f, 0.7f);
                } while (Mathf.Abs(slopeAngleA - slopeAngleB) > 1.4f || Mathf.Abs(slopeAngleA - slopeAngleB) < 0.3f); // 0.78 == 45 degrees difference

                Vector3 shiftVectorA = new Vector3(Mathf.Cos(slopeAngleA) * distScale, 0, Mathf.Sin(slopeAngleA) * distScale);
                Vector3 shiftVectorB = new Vector3(Mathf.Cos(slopeAngleB) * distScale, 0, Mathf.Sin(slopeAngleB) * distScale);

                /* Define Line A. */
                objPosLineA[0] = ShiftRandomPosition(_centerPoint, _objScale, -2.3f, -1.5f, 1.3f, 1f);
                for (var objIndex = 1; objIndex < lineSizeA; objIndex++)
                {
                    objPosLineA[objIndex] = objPosLineA[0] + shiftVectorA * objIndex;
                }

                /* Define Line B. */
                objPosLineB[0] = ShiftRandomPosition(objPosLineA[0], _objScale, 0f, 0.7f, -2.5f, -3f);
                for (var objIndex = 1; objIndex < lineSizeB; objIndex++)
                {
                    objPosLineB[objIndex] = objPosLineB[0] + shiftVectorB * objIndex;
                }

                objPositions = objPosLineA.Concat(objPosLineB).ToArray();
            }

            // add rule object
            _sceneData = IntegrateData(_sceneData, objShapes, objColors, objPositions, objTotalNum, _objScale);
            return _sceneData;
        }


        public List<ObjectStruct> CheckMarkScene(string positionType)
        {
            /*
             * There are always two lines in the image.
             * Positive: Line A and Line B form the shape of a check mark.
             * Negative: Line A and Line B form the shape of a cross.
             */

            string[] objShapes;
            string[] objColors;
            Vector3[] objPositions;
            const float distScale = 1.2f;
            if (positionType == "check_mark")
            {
                /* Store data to two lists, and concatenate them later. */
                /* The length of Lines are between 3 to 5. */
                int lineSizeA = UnityEngine.Random.Range(3, 6);
                int lineSizeB = UnityEngine.Random.Range(2, 3);
                objTotalNum = lineSizeA + lineSizeB;

                /* The shapes and colors are randomly selected. */
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = RandomShapes(objTotalNum, 3);
                objColors = RandomColors(objTotalNum, 3);


                Vector3[] objPosLineA = new Vector3[lineSizeA];
                Vector3[] objPosLineB = new Vector3[lineSizeB];

                // rotate a random degree
                var slopeAngle = UnityEngine.Random.Range(0.5f, 1.3f);
                Vector3 shiftVectorA = new Vector3(Mathf.Cos(slopeAngle) * distScale, 0, Mathf.Sin(slopeAngle) * distScale);
                Vector3 shiftVectorB;
                float angleFactor = 1.7f;
                if (slopeAngle > 0)
                {
                    shiftVectorB =
                        new Vector3(Mathf.Cos(Mathf.PI / angleFactor - slopeAngle) * distScale, 0,
                            -Mathf.Sin(Mathf.PI / angleFactor - slopeAngle) * distScale);
                }
                else
                {
                    shiftVectorB =
                        new Vector3(Mathf.Cos(Mathf.PI / angleFactor + slopeAngle) * distScale, 0,
                            Mathf.Sin(Mathf.PI / angleFactor + slopeAngle) * distScale);
                }


                /* Define Line A. */
                objPosLineA[0] = ShiftRandomPosition(_centerPoint, _objScale, -2f, 0.2f, -1f, -1.5f);
                for (var objIndex = 1; objIndex < lineSizeA; objIndex++)
                {
                    objPosLineA[objIndex] = objPosLineA[0] + shiftVectorA * objIndex;
                }

                /* Define Line B. */
                // objPosLineB[0] = objPosLineA[0] + shiftVectorB;
                // objPosLineB[0] = ShiftRandomPosition(objPosLineA[0], _objScale, 0f, 0.5f, -2.5f, -3f);
                for (var objIndex = 0; objIndex < lineSizeB; objIndex++)
                {
                    objPosLineB[objIndex] = objPosLineA[0] - shiftVectorB * (objIndex + 1);
                }

                objPositions = objPosLineA.Concat(objPosLineB).ToArray();
            }
            else
            {
                /* The length of Lines are between 3 to 5. */
                int lineSizeA = UnityEngine.Random.Range(3, 5);
                int lineSizeB = UnityEngine.Random.Range(2, 5);
                objTotalNum = lineSizeA + lineSizeB;

                /* The shapes and colors are randomly selected. */
                _sceneData = new List<ObjectStruct>(new ObjectStruct[objTotalNum]);
                objShapes = RandomShapes(objTotalNum, 3);
                objColors = RandomColors(objTotalNum, 3);

                /* Store data to two lists, and concatenate them later. */
                Vector3[] objPosLineA = new Vector3[lineSizeA];
                Vector3[] objPosLineB = new Vector3[lineSizeB];

                float angleFactor = 1.7f;
                // rotate a random degree
                var slopeAngle = UnityEngine.Random.Range(0.5f, 1.3f);
                Vector3 shiftVectorA = new Vector3(Mathf.Cos(slopeAngle) * distScale, 0, Mathf.Sin(slopeAngle) * distScale);
                Vector3 shiftVectorB;
                if (slopeAngle > 0)
                {
                    shiftVectorB =
                        new Vector3(Mathf.Cos(Mathf.PI / angleFactor - slopeAngle) * distScale, 0,
                            -Mathf.Sin(Mathf.PI / angleFactor - slopeAngle) * distScale);
                }
                else
                {
                    shiftVectorB =
                        new Vector3(Mathf.Cos(Mathf.PI / angleFactor + slopeAngle) * distScale, 0,
                            Mathf.Sin(Mathf.PI / angleFactor + slopeAngle) * distScale);
                }

                /* Define Line A. */
                objPosLineA[0] = ShiftRandomPosition(_centerPoint, _objScale, -2f, 0.2f, -1f, -1.5f);
                for (var objIndex = 1; objIndex < lineSizeA; objIndex++)
                {
                    objPosLineA[objIndex] = objPosLineA[0] + shiftVectorA * objIndex;
                }

                /* Define Line B. */
                objPosLineB[0] = objPosLineA[UnityEngine.Random.Range(1, lineSizeA)] - shiftVectorB;
                for (var objIndex = 1; objIndex < lineSizeB; objIndex++)
                {
                    objPosLineB[objIndex] = objPosLineB[0] + shiftVectorB * (objIndex + 1);
                }

                objPositions = objPosLineA.Concat(objPosLineB).ToArray();
            }

            // add rule object
            _sceneData = IntegrateData(_sceneData, objShapes, objColors, objPositions, objTotalNum, _objScale);
            return _sceneData;
        }
    }
}